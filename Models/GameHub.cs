using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    private readonly GameRoomManager _roomManager;

    public GameHub(GameRoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public async Task CreateRoom(string roomCode)
    {
        _roomManager.CreateRoom(roomCode);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.Caller.SendAsync("RoomCreated", roomCode);
    }

    public async Task JoinRoom(string roomCode, string playerName)
    {
        if (!_roomManager.RoomExists(roomCode))
        {
            await Clients.Caller.SendAsync("JoinFailed", "Rummet finns inte");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.Group(roomCode).SendAsync("PlayerJoined", playerName);
    }
	

	public async Task SendMessage(string roomCode, string playerName, string message)
	{
		await Clients.Group(roomCode).SendAsync("ReceiveMessage", playerName, message);
	}

	public async Task LeaveRoom(string roomCode, string playerName)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
    await Clients.Group(roomCode).SendAsync("PlayerLeft", playerName);
    await Clients.Caller.SendAsync("YouLeftRoom"); // ny rad - bekräftelse till den som lämnade
}

}