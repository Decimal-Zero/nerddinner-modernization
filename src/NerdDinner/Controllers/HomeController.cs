using Microsoft.AspNetCore.Mvc;

namespace NerdDinner.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Message = "Organizing the world's nerds and helping them eat in packs.";

            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
