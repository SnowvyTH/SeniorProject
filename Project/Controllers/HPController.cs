using Microsoft.AspNetCore.Mvc;

namespace Project.Controllers
{
    public class HPController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
