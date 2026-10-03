using Microsoft.AspNetCore.SignalR;
using Ritagissa.Models;

public class GameHub : Hub
{
    private readonly GameRoomManager _roomManager;

    public GameHub(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    //public async Task CreateRoom(string roomCode, string playerName)
    //{
    //    _roomManager.CreateRoom(roomCode);
    //    await JoinRoom(roomCode, playerName);
    //    //await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
    //    await Clients.Caller.SendAsync("RoomCreated", roomCode);
    //}

    public async Task CreateRoom(string roomCode, string playerName)
    {
        _roomManager.GetOrCreateRoom(roomCode); // skapar bara om den inte redan finns
        await JoinRoom(roomCode, playerName);
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
    public async Task JoinRoom(string roomCode, string playerName)
    {
        var room = _roomManager.GetRoom(roomCode);
        if (room == null)
        {
            await Clients.Caller.SendAsync("JoinFailed", "Rummet finns inte");
            return;
        }

        var player = new Player { Id = Context.ConnectionId, Name = playerName };
        room.Players.Add(player);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.Group(roomCode).SendAsync("PlayerListUpdated", room.Players.Select(p => p.Name));
    }

    public async Task LeaveRoom(string roomCode, string playerName)
    {
        var room = _roomManager.GetRoom(roomCode);
        var player = room?.Players.FirstOrDefault(p => p.Id == Context.ConnectionId);
        if (player != null) room!.Players.Remove(player);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);

        if (room != null)
            await Clients.Group(roomCode).SendAsync("PlayerListUpdated", room.Players.Select(p => p.Name));

        await Clients.Caller.SendAsync("YouLeftRoom");
    }

    // NY - städar upp om någon stänger fliken eller laddar om sidan
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = _roomManager.FindRoomByConnectionId(Context.ConnectionId);
        if (room != null)
        {
            var player = room.Players.First(p => p.Id == Context.ConnectionId);
            room.Players.Remove(player);
            await Clients.Group(room.RoomCode).SendAsync("PlayerListUpdated", room.Players.Select(p => p.Name));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string roomCode, string playerName, string message)
	{
		await Clients.Group(roomCode).SendAsync("ReceiveMessage", playerName, message);
	}

//	public async Task LeaveRoom(string roomCode, string playerName)
//{
//    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
//    await Clients.Group(roomCode).SendAsync("PlayerLeft", playerName);
//    await Clients.Caller.SendAsync("YouLeftRoom"); // ny rad - bekräftelse till den som lämnade
//}

}