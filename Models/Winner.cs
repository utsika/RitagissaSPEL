namespace Ritagissa.Models
{
    // En rad i "Winners"-tabellen. Klädfälten är null tills gubben-skaparen finns.
    public class Winner
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Points { get; set; }
        public string? Hat { get; set; }
        public string? Eyes { get; set; }
        public string? Color { get; set; }
        public string? Mouth { get; set; }
        public string? Clothes { get; set; }
    }
}
