public class GameRoomManager
{
    private readonly HashSet<string> _activeRooms = new();

    public void CreateRoom(string roomCode) => _activeRooms.Add(roomCode);

    public bool RoomExists(string roomCode) => _activeRooms.Contains(roomCode);
}