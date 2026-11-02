namespace Ritagissa.Models
{
    // Spelarens gubbe. Varje värde är ett namn från databasen och samma som bildfilen
    // i wwwroot/images/avatar, t.ex. "hat_beret" -> /images/avatar/hat_beret.png
    public class Avatar
    {
        public string? Color { get; set; }
        public string? Clothes { get; set; }
        public string? Mouth { get; set; }
        public string? Eyes { get; set; }
        public string? Hat { get; set; }

        // Ordningen bilderna läggs på varandra: kroppen längst ner, hatten överst
        public IEnumerable<string?> Layers => new[] { Color, Clothes, Mouth, Eyes, Hat };
    }

    // Alla plagg som går att välja, hämtade från databasen
    public class AvatarOptions
    {
        public List<string> Color { get; set; } = new();
        public List<string> Clothes { get; set; } = new();
        public List<string> Mouth { get; set; } = new();
        public List<string> Eyes { get; set; } = new();
        public List<string> Hat { get; set; } = new();
    }
}
