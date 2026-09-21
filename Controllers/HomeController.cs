using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Ritagissa.Models;
using System.Diagnostics;

namespace Ritagissa.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        //public async Task<IActionResult> Index()
        //{
        //    var connString = "Host=aws-0-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.dveeoewnhymgaokbmzjf;Password=zbgg9JIHf9bwCVjl";

        //    await using var conn = new NpgsqlConnection(connString);
        //    await conn.OpenAsync();

        //    await using var cmd = new NpgsqlCommand("SELECT \"Name\" FROM \"Hats\" WHERE \"Name\" = 'keps'", conn);

        //    var result = await cmd.ExecuteScalarAsync();

        //    ViewBag.DbTest = result != null ? $"Databasen svarade: {result}" : "Anslutning OK, men ingen rad hittades";

        //    return View();
        //}

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //Generates a room code for a game
        //kopierat från memory-spelet från databasen
        private string GenerateRoomCode()
        {
            // Generate a random 6-character alphanumeric string
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        

    }



}
