using Microsoft.AspNetCore.SignalR;
using Ritagissa.Models;

public class GameHub : Hub
{
    private const int MinPlayers = 2;
    private const int MinRounds = 2;
    private const int MaxRounds = 6;
    private readonly GameRoomManager _roomManager;
    private readonly GameService _gameService;


    public GameHub(GameRoomManager roomManager, GameService gameService)
    {
        _roomManager = roomManager;
        _gameService = gameService;
    }

    public async Task CreateRoom(string roomCode, string playerName, string playerId)
    {
        _roomManager.GetOrCreateRoom(roomCode); // skapar bara om den inte redan finns
        await JoinRoom(roomCode, playerName, playerId);
        await Clients.Caller.SendAsync("RoomCreated", roomCode);
    }

    //public async Task JoinRoom(string roomCode, string playerName)
    //{
    //    if (!_roomManager.RoomExists(roomCode))
    //    {
    //        await Clients.Caller.SendAsync("JoinFailed", "Rummet finns inte");
    //        return;
    //    }

    //    await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
    //    await Clients.Group(roomCode).SendAsync("PlayerJoined", playerName);
    //}
    public async Task JoinRoom(string roomCode, string playerName, string playerId)
    {
        var room = _roomManager.GetRoom(roomCode);
        if (room == null)
        {
            await Clients.Caller.SendAsync("JoinFailed", "Rummet finns inte");
            return;
        }

        var player = room.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
        {
            player = new Player { Id = playerId, Name = playerName };
            room.Players.Add(player);
        }

        player.Name = playerName;
        player.ConnectionId = Context.ConnectionId;

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        //await Clients.Group(roomCode).SendAsync("PlayerListUpdated", room.Players.Select(p => p.Name));
        await SendPlayerList(room);
        await Clients.Caller.SendAsync("TotalRoundsUpdated", room.TotalRounds);
    }

    public async Task SetTotalRounds(string roomCode, int rounds)
    {
        var room = _roomManager.GetRoom(roomCode);
        if (room == null || room.State != GameState.Lobby) return;

        // Bara hosten (första spelaren) får ändra
        if (room.Players.Count == 0 || room.Players[0].ConnectionId != Context.ConnectionId) return;

        room.TotalRounds = Math.Clamp(rounds, MinRounds, MaxRounds);
        await Clients.Group(roomCode).SendAsync("TotalRoundsUpdated", room.TotalRounds);
    }

    public async Task LeaveRoom(string roomCode, string playerId)
    {
        var room = _roomManager.GetRoom(roomCode);
        var player = room?.Players.FirstOrDefault(p => p.Id == playerId && p.ConnectionId == Context.ConnectionId);
        if (player != null) room!.Players.Remove(player);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);

        if (room != null)
            await Clients.Group(roomCode).SendAsync("PlayerListUpdated", room.Players.Select(p => p.Name));

        await Clients.Caller.SendAsync("YouLeftRoom");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var room = _roomManager.FindRoomByConnectionId(connectionId);
        var player = room?.Players.FirstOrDefault(p => p.ConnectionId == connectionId);

        if (room != null && player != null)
        {
            player.ConnectionId = string.Empty;

            await Task.Delay(TimeSpan.FromSeconds(10));

            // Har spelaren inte återanslutit på 10 sekunder lämnade de på riktigt
            if (player.ConnectionId == string.Empty)
            {
                room.Players.Remove(player);
                await SendPlayerList(room);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartGame(string roomCode)
    {
        var room = _roomManager.GetRoom(roomCode);
        if (room == null || room.State != GameState.Lobby) return;

        // Bara hosten (första spelaren) får starta
        if (room.Players.Count == 0 || room.Players[0].ConnectionId != Context.ConnectionId) return;

        if (room.Players.Count < MinPlayers)
        {
            await Clients.Caller.SendAsync("GameError", $"Det krävs minst {MinPlayers} spelare.");
            return;
        }

        await _gameService.StartGame(room);   // istället för StartTurn(room)
    }

    public async Task SelectWord(string roomCode, string word)
    {
        var room = _roomManager.GetRoom(roomCode);
        if (room == null) return;

        await _gameService.SelectWord(room, Context.ConnectionId, word);
    }

    public async Task SendDrawPoint(string roomCode, int x, int y, bool isNewStroke)
    {
        var room = _roomManager.GetRoom(roomCode);
        if (room == null || room.State != GameState.Drawing) return;

        // Bara ritaren får rita
        if (room.CurrentPlayer?.ConnectionId != Context.ConnectionId) return;

        await Clients.OthersInGroup(roomCode).SendAsync("ReceiveDrawPoint", x, y, isNewStroke);
    }


    public async Task SendMessage(string roomCode, string playerName, string message)
	{
		await Clients.Group(roomCode).SendAsync("ReceiveMessage", playerName, message);
	}

    
    public async Task SendGuess(string roomCode, string guess)
    {
        var room = _roomManager.GetRoom(roomCode);
        var player = room?.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
        if (room == null || player == null) return;

        await _gameService.ProcessGuess(room, player, guess);
    }

    public async Task PlayAgain(string roomCode)
    {
        var room = _roomManager.GetRoom(roomCode);
        if (room == null || room.State != GameState.GameOver) return;

        // Bara hosten (första spelaren) får starta en ny omgång
        if (room.Players.Count == 0 || room.Players[0].ConnectionId != Context.ConnectionId) return;

        room.ResetForNewGame();
        await Clients.Group(roomCode).SendAsync("BackToLobby");
        await SendPlayerList(room);
    }

    private Task SendPlayerList(GameSession room) =>
        Clients.Group(room.RoomCode).SendAsync("PlayerListUpdated", room.Players.Select(p => p.Name));
    
}