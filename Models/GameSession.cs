using System.Numerics;

namespace Ritagissa.Models
{
    public class GameSession
    {
        public string RoomCode { get; }
        public List<Player> Players { get; } = new();
        public OrdDTO? CurrentWord { get; set; }
        public Player? CurrentPlayer { get; set; }

        public GameSession(string roomCode)
        {
            RoomCode = roomCode;
        }
    }
}