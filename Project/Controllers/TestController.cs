using Microsoft.AspNetCore.Mvc;
using Project.Services;

namespace Project.Controllers
{
    public class TestController : Controller
    {
        private readonly IPasswordService _passwordService;

        public TestController(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        public IActionResult HashPassword(string password = "hello")
        {
            var hash = _passwordService.HashPassword(password);
            return Json(new { 
                password = password, 
                hash = hash,
                expectedHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92"
            });
        }
    }
}
