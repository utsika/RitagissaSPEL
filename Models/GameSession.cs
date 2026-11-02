using System.Numerics;

namespace Ritagissa.Models
{
    public enum GameState
    {
        Lobby,
        ChoosingWord,
        Drawing,
        TurnEnd,
        GameOver
    }
    public class GameSession
    {
        public string RoomCode { get; }
        public List<Player> Players { get; } = new();
        public OrdDTO? CurrentWord { get; set; }
        public Player? CurrentPlayer { get; set; }
        public List<OrdDTO> WordChoices { get; set; } = new();
        public List<string> UsedWords { get; } = new();
        public CancellationTokenSource? TimeOver { get; set; }
        public CancellationTokenSource? DrawTimeOver { get; set; }
        public int TotalRounds { get; set; } = 2;
        public int CurrentRound { get; set; } = 0;
        public HashSet<string> HasDrawnThisRound { get; } = new();

        public GameState State { get; set; } = GameState.Lobby;

        public GameSession(string roomCode)
        {
            RoomCode = roomCode;
        }

        public void ResetForNewGame()
        {
            TimeOver?.Cancel();
            DrawTimeOver?.Cancel();
            State = GameState.Lobby;
            CurrentRound = 0;
            CurrentPlayer = null;
            CurrentWord = null;
            WordChoices = new();
            UsedWords.Clear();
            HasDrawnThisRound.Clear();
            foreach (var p in Players) p.Points = 0;
        }
    }

}