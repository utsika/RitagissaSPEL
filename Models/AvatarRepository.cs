using Npgsql;

namespace Ritagissa.Models
{
    // Hämtar vilka plagg som finns i tabellerna Color, Clothes, Mouth, Eyes och Hats
    public class AvatarRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly string _imageFolder;

        public AvatarRepository(NpgsqlDataSource dataSource, IWebHostEnvironment env)
        {
            _dataSource = dataSource;
            _imageFolder = Path.Combine(env.WebRootPath, "images", "avatar");
        }

        public async Task<AvatarOptions> GetOptions()
        {
            var options = new AvatarOptions();

            await using var cmd = _dataSource.CreateCommand(
                """
                SELECT 'Color', "Name" FROM "Color"
                UNION ALL SELECT 'Clothes', "Name" FROM "Clothes"
                UNION ALL SELECT 'Mouth', "Name" FROM "Mouth"
                UNION ALL SELECT 'Eyes', "Name" FROM "Eyes"
                UNION ALL SELECT 'Hat', "Name" FROM "Hats"
                ORDER BY 1, 2
                """);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(1);

                // Plagg utan bildfil hämtas inte
                if (!File.Exists(Path.Combine(_imageFolder, name + ".png"))) continue;

                var list = reader.GetString(0) switch
                {
                    "Color" => options.Color,
                    "Clothes" => options.Clothes,
                    "Mouth" => options.Mouth,
                    "Eyes" => options.Eyes,
                    _ => options.Hat,
                };
                list.Add(name);
            }
            return options;
        }

        // Ser till att spelaren bara kan ha plagg som finns. Fel eller saknat värde blir första alternativet.
        public async Task<Avatar> Validate(Avatar? chosen)
        {
            var options = await GetOptions();
            return new Avatar
            {
                Color = Pick(options.Color, chosen?.Color),
                Clothes = Pick(options.Clothes, chosen?.Clothes),
                Mouth = Pick(options.Mouth, chosen?.Mouth),
                Eyes = Pick(options.Eyes, chosen?.Eyes),
                Hat = Pick(options.Hat, chosen?.Hat),
            };
        }

        private static string? Pick(List<string> options, string? chosen) =>
            chosen != null && options.Contains(chosen) ? chosen : options.FirstOrDefault();
    }
}
