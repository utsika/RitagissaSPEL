using Ritagissa.Models;

public class GameRoomManager
{
    private readonly Dictionary<string, GameSession> _rooms = new();

    public GameSession GetOrCreateRoom(string roomCode)
    {
        if (!_rooms.TryGetValue(roomCode, out var session))
        {
            session = new GameSession(roomCode);
            _rooms[roomCode] = session;
        }
        return session;
    }

    public GameSession? GetRoom(string roomCode) => _rooms.GetValueOrDefault(roomCode);

    public GameSession? FindRoomByConnectionId(string connectionId) =>
        _rooms.Values.FirstOrDefault(r => r.Players.Any(p => p.ConnectionId == connectionId));
}