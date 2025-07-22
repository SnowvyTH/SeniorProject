using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.Controllers
{
    [Authorize(Roles = "เภสัช")]
    public class PharmaController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            return View();
        }
    }
}
