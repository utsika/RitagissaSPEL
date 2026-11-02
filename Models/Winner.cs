namespace Ritagissa.Models
{
    // En rad i "Winners"-tabellen
    public class Winner
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Points { get; set; }
        public Avatar Avatar { get; set; } = new();
    }
}
