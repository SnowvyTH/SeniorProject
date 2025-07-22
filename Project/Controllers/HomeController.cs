using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Project.Models;
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
        // ถ้าผู้ใช้เข้าสู่ระบบแล้ว ให้ redirect ไปหน้า Dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard");
        }
        
        return View(new LoginViewModel());
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
        var log = new Project.Models.Entity.Log
        {
            UserID = user.UserID,
            LogDateTime = DateTime.Now
        };
        _context.Log.Add(log);
        await _context.SaveChangesAsync();

        return RedirectToAction("Dashboard");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index");
    }

    public IActionResult Dashboard()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Index");
        }
        
        ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
        ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
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
