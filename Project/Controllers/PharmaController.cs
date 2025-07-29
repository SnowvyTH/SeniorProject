using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models;
using Project.Models.Entity;
using Project.Data;


namespace Project.Controllers
{
    [Authorize(Roles = "เภสัช")]
    public class PharmaController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PharmaController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UserName = User.FindFirst("FirstName").Value + " " + User.FindFirst("LastName").Value;
            ViewBag.Forms = await _db.Form.ToListAsync();

            var patients = await _db.Patient
                .Include(p => p.Title)
                .Include(p => p.Evaluations)
                    .ThenInclude(r => r.Form)
                .Include(p => p.Evaluations)
                    .ThenInclude(r => r.User)
                    .ThenInclude(u => u.Title)
                .ToListAsync();

            return View(patients);
        }
    }
}
