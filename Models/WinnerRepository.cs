using Npgsql;

namespace Ritagissa.Models
{
    // Läser och skriver vinnare i Supabase-tabellen "Winners"
    public class WinnerRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public WinnerRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task AddWinners(IEnumerable<Player> winners)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            foreach (var player in winners)
            {
                // Klädfälten sätts uttryckligen till NULL. Standardvärdena i tabellen
                // ('defaultHat' osv.) finns inte i Hats/Eyes/... så de skulle bryta foreign key.
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO "Winners" ("Name", "Points", "Hat", "Eyes", "Color", "Mouth", "Clothes")
                    VALUES (@name, @points, NULL, NULL, NULL, NULL, NULL)
                    """, conn, transaction);
                cmd.Parameters.AddWithValue("name", player.Name);
                cmd.Parameters.AddWithValue("points", (long)player.Points);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        public async Task<List<Winner>> GetTopWinners(int count)
        {
            await using var cmd = _dataSource.CreateCommand(
                """
                SELECT "Id", "Name", "Points", "Hat", "Eyes", "Color", "Mouth", "Clothes"
                FROM "Winners"
                ORDER BY "Points" DESC, "Id" ASC
                LIMIT @count
                """);
            cmd.Parameters.AddWithValue("count", count);

            var winners = new List<Winner>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                winners.Add(new Winner
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    Points = reader.GetInt64(2),
                    Hat = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Eyes = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Color = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Mouth = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Clothes = reader.IsDBNull(7) ? null : reader.GetString(7),
                });
            }
            return winners;
        }
    }
}
