using Microsoft.AspNetCore.Mvc;

namespace Project.Controllers
{
    public class PharmaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
