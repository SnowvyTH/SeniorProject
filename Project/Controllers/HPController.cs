using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.Controllers
{
    [Authorize]
    public class HPController : Controller
    {
        public IActionResult Index()
        {
            // ตรวจสอบสิทธิ์ - ต้องเป็น RoleID = 2 เท่านั้น
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            return View();
        }
    }
}
