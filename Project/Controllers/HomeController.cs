using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Project.Models;
using Project.Models.Entity;
using Project.Services;
using Project.Data;

namespace Project.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, IAuthService authService, ApplicationDbContext context)
    {
        _logger = logger;
        _authService = authService;
        _context = context;
    }

    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            var roleIdClaim = User.FindFirst("RoleID")?.Value;
            if (int.TryParse(roleIdClaim, out int roleId))
            {
                return roleId switch
                {
                    1 => RedirectToAction("Index", "Pharma"),
                    2 => RedirectToAction("Index", "HP"),
                    3 => RedirectToAction("Index", "Admin"),
                    _ => RedirectToAction("Logout")
                };
            }
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _authService.AuthenticateAsync(model.Username, model.Password);
        
        if (user == null)
        {
            ModelState.AddModelError("", "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
            return View(model);
        }

        // สร้าง Claims สำหรับ Authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FirstName", user.FirstName ?? ""),
            new Claim("LastName", user.LastName ?? ""),
            new Claim("RoleID", user.RoleID?.ToString() ?? ""),
            new Claim("RoleName", user.Role?.RoleName ?? ""),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? ""),
            new Claim("TitleID", user.TitleID?.ToString() ?? ""),
            new Claim("TitleName", user.Title?.TitleName ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), authProperties);

        // บันทึก Log การเข้าสู่ระบบ
        var log = new Log
        {
            UserID = user.UserID,
            LogDateTime = DateTime.Now
        };
        _context.Log.Add(log);
        await _context.SaveChangesAsync();

        // Redirect ตามสิทธิ์ของผู้ใช้
        return user.RoleID switch
        {
            1 => RedirectToAction("Index", "Pharma"),      // เภสัช
            2 => RedirectToAction("Index", "HP"),          // หัวหน้าเภสัช
            3 => RedirectToAction("Index", "Admin"),       // ผู้ดูแลระบบ
            _ => RedirectToAction("AccessDenied")
        };
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
