using Microsoft.AspNetCore.Mvc;

namespace Ritagissa.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        
}
