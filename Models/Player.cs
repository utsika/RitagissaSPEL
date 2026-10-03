namespace Ritagissa.Models
{
    public class Player
    {
        public string Id { get; set; } = string.Empty;   // ConnectionId från SignalR
        public string Name { get; set; } = string.Empty;
        public int Points { get; set; } = 0;
    }
}