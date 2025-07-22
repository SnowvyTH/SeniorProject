using Microsoft.AspNetCore.Mvc;

namespace Project.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
