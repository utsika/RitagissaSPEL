using Microsoft.AspNetCore.SignalR;
using System.Text;

namespace Ritagissa.Models
{
    public class GameService
    {
        private const int ChooseTimer = 30;
        // ändra till längre sen, men har så här nu för att kunna testa
        private const int DrawTimer = 15;
        private const string WordApiUrl = "http://localhost:5231/api/ordlista/random";

        private readonly IHubContext<GameHub> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WinnerRepository _winnerRepository;
        private readonly ILogger<GameService> _logger;

        public GameService(IHubContext<GameHub> hubContext, IHttpClientFactory httpClientFactory,
            WinnerRepository winnerRepository, ILogger<GameService> logger)
        {
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _winnerRepository = winnerRepository;
            _logger = logger;
        }

        public async Task StartGame(GameSession room)
        {
            room.CurrentRound = 1;
            room.HasDrawnThisRound.Clear();
            room.CurrentPlayer = room.Players[0];
            await StartTurn(room);
        }

        // Startar en tur: hämtar tre ord, visar rätt vy för rätt person och startar timern
        public async Task StartTurn(GameSession room)
        {
            room.State = GameState.ChoosingWord;
            room.CurrentWord = null;
            room.CurrentPlayer ??= room.Players[0];
            var drawer = room.CurrentPlayer;
            await _hubContext.Clients.Group(room.RoomCode)
                .SendAsync("RoundUpdated", room.CurrentRound, room.TotalRounds);

            // Hämta tre unika ord
            var wordOptions = new List<OrdDTO>();
            for (int i = 0; i < 3; i++)
            {
                var excludeWord = room.UsedWords.Concat(wordOptions.Select(c => c.Word)).ToList();
                var word = await GetRandomWord(excludeWord);
                if (word != null)
                {
                    wordOptions.Add(word);
                }
            }

            // Gick det inte att nå ordlistan, gå tillbaka till lobbyn istället för att fastna
            if (wordOptions.Count == 0)
            {
                room.State = GameState.Lobby;
                await _hubContext.Clients.Group(room.RoomCode)
                    .SendAsync("GameError", "Kunde inte hämta ord, försök igen.");
                return;
            }

            room.WordChoices = wordOptions;

            // Ritaren ser orden, de andra ser "väntar..."
            await _hubContext.Clients.Client(drawer.ConnectionId)
                .SendAsync("ChooseWord", wordOptions.Select(w => w.Word), ChooseTimer);
            await _hubContext.Clients.GroupExcept(room.RoomCode, new[] { drawer.ConnectionId })
                .SendAsync("WaitingForWord", drawer.Name, ChooseTimer);

            // Starta timern (avbryt en eventuell gammal först)
            room.TimeOver?.Cancel();
            room.TimeOver = new CancellationTokenSource();
            _ = RunChooseTimer(room, room.TimeOver.Token);
        }

        // Ritaren (eller timern) väljer ett ord
        public async Task SelectWord(GameSession room, string connectionId, string word)
        {
            if (room.State != GameState.ChoosingWord) return;
            if (room.CurrentPlayer?.ConnectionId != connectionId) return;   // bara ritaren

            var chosen = room.WordChoices.FirstOrDefault(w => w.Word == word);
            if (chosen == null) return;                                      // måste vara ett av de tre

            room.TimeOver?.Cancel();
            room.CurrentWord = chosen;
            room.UsedWords.Add(chosen.Word);
            room.State = GameState.Drawing;

            await _hubContext.Clients.Client(connectionId)
                .SendAsync("StartDrawing", chosen.Word);
            await _hubContext.Clients.GroupExcept(room.RoomCode, new[] { connectionId })
                .SendAsync("StartGuessing", room.CurrentPlayer.Name, chosen.Word.Length);

            // Starta ritartimern (avbryt en eventuell gammal först)
            room.DrawTimeOver?.Cancel();
            room.DrawTimeOver = new CancellationTokenSource();
            _ = RunDrawTimer(room, room.DrawTimeOver.Token);

            await _hubContext.Clients.Group(room.RoomCode).SendAsync("DrawTimerStarted", DrawTimer);
        }

        // Om ritaren inte hinner välja slumpas ett ord åt hen
        private async Task RunChooseTimer(GameSession room, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ChooseTimer), token);

                if (room.State == GameState.ChoosingWord && room.WordChoices.Count > 0)
                {
                    var random = room.WordChoices[Random.Shared.Next(room.WordChoices.Count)];
                    await SelectWord(room, room.CurrentPlayer!.ConnectionId, random.Word);
                }
            }
            catch (TaskCanceledException) { }
        }

        // Hämtar ett slumpat ord från OrdlistaAPI
        private async Task<OrdDTO?> GetRandomWord(List<string> exclude)
        {
            var client = _httpClientFactory.CreateClient();

            var url = WordApiUrl;
            if (exclude.Count > 0)
            {
                var query = string.Join("&", exclude.Select(w => $"exclude={Uri.EscapeDataString(w)}"));
                url += $"?{query}";
            }

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<OrdDTO>();
        }

        public async Task ProcessGuess(GameSession room, Player player, string guess)
        {
            if (room.State != GameState.Drawing || room.CurrentWord == null) return;
            if (player == room.CurrentPlayer) return;              // ritaren får inte gissa

            guess = guess.Trim();
            if (guess.Length == 0 || guess.Length > 50) return;

            if (Normalize(guess) == Normalize(room.CurrentWord.Word))
            {
                // Stäng turen DIREKT, annars hinner fler gissa rätt under pausen
                room.State = GameState.TurnEnd;

                player.Points += 10;                               // gissaren
                room.CurrentPlayer!.Points += 5;                   // ritaren

                await _hubContext.Clients.Group(room.RoomCode).SendAsync("CorrectGuess", player.Name);
                await EndTurn(room, timedOut: false);
            }
            else
            {
                await _hubContext.Clients.Group(room.RoomCode).SendAsync("ReceiveGuess", player.Name, guess);
            }
        }

        // Körs när ritartiden är slut
        private async Task RunDrawTimer(GameSession room, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(DrawTimer), token);

                if (room.State != GameState.Drawing) return;

                room.State = GameState.TurnEnd;   // stäng turen direkt så ingen hinner gissa rätt
                await EndTurn(room, timedOut: true);
            }
            catch (TaskCanceledException) { }
        }

        // Gemensam avslutning för en tur, oavsett om någon gissade rätt eller tiden gick ut
        private async Task EndTurn(GameSession room, bool timedOut)
        {
            room.DrawTimeOver?.Cancel();   // stoppa timern om turen slutade före tiden

            await _hubContext.Clients.Group(room.RoomCode)
                .SendAsync("TurnEnded", room.CurrentWord!.Word, timedOut);

            await Task.Delay(TimeSpan.FromSeconds(3));

            await AdvanceGame(room);       // nästa ritare, ny runda eller spelet slut
        }
        // Bestämmer vad som händer när en tur är slut: nästa ritare, ny runda eller spelet slut
        private async Task AdvanceGame(GameSession room)
        {
            if (room.Players.Count == 0) return;

            // Bocka av ritaren som just ritat
            if (room.CurrentPlayer != null)
                room.HasDrawnThisRound.Add(room.CurrentPlayer.Id);

            // Första spelaren (i listordning) som inte ritat i den här rundan
            var next = room.Players.FirstOrDefault(p => !room.HasDrawnThisRound.Contains(p.Id));

            if (next == null)
            {
                // Alla har ritat, rundan är slut
                if (room.CurrentRound >= room.TotalRounds)
                {
                    await EndGame(room);
                    return;
                }

                room.CurrentRound++;
                room.HasDrawnThisRound.Clear();
                next = room.Players[0];
            }

            room.CurrentPlayer = next;
            await StartTurn(room);
        }

        private async Task EndGame(GameSession room)
        {
            room.State = GameState.GameOver;

            var ordered = room.Players.OrderByDescending(p => p.Points).ToList();
            var ranking = ordered.Select(p => new
            {
                rank = ordered.FindIndex(o => o.Points == p.Points) + 1,   // lika poäng ger samma placering
                name = p.Name,
                points = p.Points
            }).ToList();

            await _hubContext.Clients.Group(room.RoomCode).SendAsync("GameOver", ranking);

            // Spara vinnaren/vinnarna (alla med högst poäng) i highscore-listan
            var topPoints = ordered[0].Points;
            if (topPoints > 0)
            {
                try
                {
                    await _winnerRepository.AddWinners(ordered.Where(p => p.Points == topPoints));
                }
                catch (Exception ex)
                {
                    // Spelet ska inte krascha om databasen strular, men vi vill se felet
                    _logger.LogError(ex, "Kunde inte spara vinnare för rum {RoomCode}", room.RoomCode);
                }
            }
        }
        private static string Normalize(string text)
        {
            // Gör t.ex. å, ä, ö jämförbara oavsett hur tangentbordet skapade tecknet
            text = text.Normalize(NormalizationForm.FormC);

            // Trimmar och slår ihop dubbla mellanslag ("ice  cream" blir "ice cream")
            text = string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return text.ToLowerInvariant();   // hanterar å, ä, ö korrekt
        }
    }
}