using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Entity;
using Project.Data;

namespace Project.Controllers
{
    [Authorize(Roles = "ผู้ดูแลระบบ")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

            // ดึงข้อมูลสถิติ
            var patientCount = await _context.Patient.CountAsync();
            var userCount = await _context.User.CountAsync();
            var formCount = await _context.Form.CountAsync();
            var todayLogins = await _context.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date == DateTime.Today)
                .CountAsync();

            ViewBag.PatientCount = patientCount;
            ViewBag.UserCount = userCount;
            ViewBag.FormCount = formCount;
            ViewBag.TodayLogins = todayLogins;

            return View();
        }

        public IActionResult Users()
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            return View();
        }

        // User Management API Actions
        [HttpGet]
        public async Task<IActionResult> GetUsers(string search = "", string roleFilter = "", string statusFilter = "")
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            var query = _context.User.Include(u => u.Title).AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FirstName.Contains(search) || 
                                       u.LastName.Contains(search) || 
                                       u.Username.Contains(search) ||
                                       u.Title.TitleName.Contains(search));
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(roleFilter) && int.TryParse(roleFilter, out int roleInt))
            {
                query = query.Where(u => u.RoleID == roleInt);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter) && bool.TryParse(statusFilter, out bool statusBool))
            {
                query = query.Where(u => u.IsActive == statusBool);
            }

            var users = await query.Select(u => new
            {
                userID = u.UserID,
                titleName = u.Title.TitleName,
                firstName = u.FirstName,
                lastName = u.LastName,
                username = u.Username,
                roleID = u.RoleID,
                isActive = u.IsActive
            }).OrderBy(u => u.userID).ToListAsync();

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            var user = await _context.User.Include(u => u.Title)
                .Where(u => u.UserID == id)
                .Select(u => new
                {
                    userID = u.UserID,
                    titleID = u.TitleID,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    username = u.Username,
                    roleID = u.RoleID,
                    isActive = u.IsActive
                }).FirstOrDefaultAsync();

            if (user == null) return Json(new { success = false, message = "User not found" });

            return Json(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(IFormCollection form)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(form["Username"]) || string.IsNullOrEmpty(form["Password"]) ||
                    string.IsNullOrEmpty(form["FirstName"]) || string.IsNullOrEmpty(form["LastName"]) ||
                    string.IsNullOrEmpty(form["TitleID"]) || string.IsNullOrEmpty(form["RoleID"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                // Check if username already exists
                var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Username == form["Username"].ToString());
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "ชื่อผู้ใช้นี้มีอยู่แล้ว" });
                }

                // Hash password
                var passwordService = HttpContext.RequestServices.GetService<Services.PasswordService>();
                if (passwordService == null)
                {
                    return Json(new { success = false, message = "ไม่พบ Password Service" });
                }
                var hashedPassword = passwordService.HashPassword(form["Password"].ToString()!);

                var user = new User
                {
                    TitleID = int.Parse(form["TitleID"].ToString()!),
                    FirstName = form["FirstName"].ToString(),
                    LastName = form["LastName"].ToString(),
                    Username = form["Username"].ToString()!,
                    PasswordHash = hashedPassword,
                    RoleID = int.Parse(form["RoleID"].ToString()!),
                    IsActive = form.ContainsKey("IsActive") && form["IsActive"].ToString().Contains("true")
                };

                _context.User.Add(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser(IFormCollection form)
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            try
            {
                if (string.IsNullOrEmpty(form["UserID"]))
                {
                    return Json(new { success = false, message = "ไม่พบรหัสผู้ใช้" });
                }

                var userId = int.Parse(form["UserID"].ToString()!);
                var user = await _context.User.FindAsync(userId);
                
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบผู้ใช้" });
                }

                // Validate required fields
                if (string.IsNullOrEmpty(form["Username"]) || string.IsNullOrEmpty(form["FirstName"]) ||
                    string.IsNullOrEmpty(form["LastName"]) || string.IsNullOrEmpty(form["TitleID"]) ||
                    string.IsNullOrEmpty(form["RoleID"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                // Check if username is already taken by another user
                var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Username == form["Username"].ToString() && u.UserID != userId);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "ชื่อผู้ใช้นี้มีอยู่แล้ว" });
                }

                user.TitleID = int.Parse(form["TitleID"].ToString()!);
                user.FirstName = form["FirstName"].ToString();
                user.LastName = form["LastName"].ToString();
                user.Username = form["Username"].ToString()!;
                user.RoleID = int.Parse(form["RoleID"].ToString()!);

                // Update password if provided
                if (!string.IsNullOrEmpty(form["Password"]))
                {
                    var passwordService = HttpContext.RequestServices.GetService<Project.Services.PasswordService>();
                    if (passwordService != null)
                    {
                        user.PasswordHash = passwordService.HashPassword(form["Password"].ToString()!);
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userID, bool isActive)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            try
            {
                var user = await _context.User.FindAsync(userID);
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบผู้ใช้" });
                }

                user.IsActive = isActive;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Titles()
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "3")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            return View();
        }

        // Title Management API Actions
        [HttpGet]
        public async Task<IActionResult> GetTitles(string search = "")
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            var query = _context.Title.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.TitleName != null && t.TitleName.Contains(search));
            }

            var titles = await query.Select(t => new
            {
                titleID = t.TitleID,
                titleName = t.TitleName
            }).OrderBy(t => t.titleID).ToListAsync();

            return Json(titles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTitle(IFormCollection form)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            try
            {
                if (string.IsNullOrEmpty(form["TitleName"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกคำนำหน้า" });
                }

                // Check if title already exists
                var existingTitle = await _context.Title.FirstOrDefaultAsync(t => t.TitleName == form["TitleName"].ToString());
                if (existingTitle != null)
                {
                    return Json(new { success = false, message = "คำนำหน้านี้มีอยู่แล้ว" });
                }

                var title = new Title
                {
                    TitleName = form["TitleName"].ToString()!
                };

                _context.Title.Add(title);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTitle(IFormCollection form)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            try
            {
                if (string.IsNullOrEmpty(form["TitleID"]) || string.IsNullOrEmpty(form["TitleName"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                var titleId = int.Parse(form["TitleID"].ToString()!);
                var title = await _context.Title.FindAsync(titleId);
                
                if (title == null)
                {
                    return Json(new { success = false, message = "ไม่พบคำนำหน้า" });
                }

                // Check if title name is already taken by another title
                var existingTitle = await _context.Title.FirstOrDefaultAsync(t => t.TitleName == form["TitleName"].ToString() && t.TitleID != titleId);
                if (existingTitle != null)
                {
                    return Json(new { success = false, message = "คำนำหน้านี้มีอยู่แล้ว" });
                }

                title.TitleName = form["TitleName"].ToString()!;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTitle(int titleID)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            try
            {
                var title = await _context.Title.FindAsync(titleID);
                if (title == null)
                {
                    return Json(new { success = false, message = "ไม่พบคำนำหน้า" });
                }

                // Check if title is being used by any user
                var usersWithTitle = await _context.User.CountAsync(u => u.TitleID == titleID);
                if (usersWithTitle > 0)
                {
                    return Json(new { success = false, message = $"ไม่สามารถลบได้ เนื่องจากมีผู้ใช้ {usersWithTitle} คนที่ใช้คำนำหน้านี้อยู่" });
                }

                _context.Title.Remove(title);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Treatments()
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            return View();
        }

        // Treatment Management API Actions
        [HttpGet]
        public async Task<IActionResult> GetTreatments(string search = "")
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            var query = _context.Treatment.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.TreatmentName != null && t.TreatmentName.Contains(search));
            }

            var treatments = await query.Select(t => new
            {
                treatmentID = t.TreatmentID,
                treatmentName = t.TreatmentName
            }).OrderBy(t => t.treatmentID).ToListAsync();

            return Json(treatments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTreatment(IFormCollection form)
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            try
            {
                if (string.IsNullOrEmpty(form["TreatmentName"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกชื่อสิทธิ์การรักษา" });
                }

                // Check if treatment already exists
                var existingTreatment = await _context.Treatment.FirstOrDefaultAsync(t => t.TreatmentName == form["TreatmentName"].ToString());
                if (existingTreatment != null)
                {
                    return Json(new { success = false, message = "สิทธิ์การรักษานี้มีอยู่แล้ว" });
                }

                var treatment = new Treatment
                {
                    TreatmentName = form["TreatmentName"].ToString()!
                };

                _context.Treatment.Add(treatment);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTreatment(IFormCollection form)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            try
            {
                if (string.IsNullOrEmpty(form["TreatmentID"]) || string.IsNullOrEmpty(form["TreatmentName"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                var treatmentId = int.Parse(form["TreatmentID"].ToString()!);
                var treatment = await _context.Treatment.FindAsync(treatmentId);
                
                if (treatment == null)
                {
                    return Json(new { success = false, message = "ไม่พบสิทธิ์การรักษา" });
                }

                // Check if treatment name is already taken by another treatment
                var existingTreatment = await _context.Treatment.FirstOrDefaultAsync(t => t.TreatmentName == form["TreatmentName"].ToString() && t.TreatmentID != treatmentId);
                if (existingTreatment != null)
                {
                    return Json(new { success = false, message = "สิทธิ์การรักษานี้มีอยู่แล้ว" });
                }

                treatment.TreatmentName = form["TreatmentName"].ToString()!;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTreatment(int treatmentID)
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "3") return Json(new { success = false, message = "Access denied" });

            try
            {
                var treatment = await _context.Treatment.FindAsync(treatmentID);
                if (treatment == null)
                {
                    return Json(new { success = false, message = "ไม่พบสิทธิ์การรักษา" });
                }

                // Check if treatment is being used by any patient
                var patientsWithTreatment = await _context.Patient.CountAsync(p => p.TreatmentID == treatmentID);
                if (patientsWithTreatment > 0)
                {
                    return Json(new { success = false, message = $"ไม่สามารถลบได้ เนื่องจากมีผู้ป่วย {patientsWithTreatment} คนที่ใช้สิทธิ์การรักษานี้อยู่" });
                }

                _context.Treatment.Remove(treatment);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Logs()
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "3")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            return View();
        }

        // Log Management API Actions
        [HttpGet]
        public async Task<IActionResult> GetLogs(string search = "", string dateFrom = "", string dateTo = "", 
            string roleFilter = "", int page = 1, int pageSize = 20)
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "3") return Json(new { success = false, message = "Access denied" });

            var query = _context.Log
                .Include(l => l.User!)
                    .ThenInclude(u => u.Title)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => l.User != null && 
                    (l.User.FirstName != null && l.User.FirstName.Contains(search) ||
                     l.User.LastName != null && l.User.LastName.Contains(search) ||
                     l.User.Username.Contains(search)));
            }

            // Apply date range filter
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime fromDate))
            {
                query = query.Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date >= fromDate.Date);
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime toDate))
            {
                query = query.Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date <= toDate.Date);
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(roleFilter) && int.TryParse(roleFilter, out int roleInt))
            {
                query = query.Where(l => l.User != null && l.User.RoleID == roleInt);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var logs = await query
                .OrderByDescending(l => l.LogDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    logID = l.LogID,
                    logDateTime = l.LogDateTime,
                    titleName = l.User != null && l.User.Title != null ? l.User.Title.TitleName : "",
                    firstName = l.User != null ? l.User.FirstName : "",
                    lastName = l.User != null ? l.User.LastName : "",
                    roleID = l.User != null ? l.User.RoleID : 0,
                    details = "เข้าสู่ระบบ" // Log model ไม่มี Details field
                })
                .ToListAsync();

            return Json(new { 
                logs = logs, 
                totalPages = totalPages, 
                currentPage = page, 
                totalCount = totalCount 
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLogStats()
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "3") return Json(new { success = false, message = "Access denied" });

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todayCount = await _context.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date == today)
                .CountAsync();

            var weekCount = await _context.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date >= weekStart)
                .CountAsync();

            var monthCount = await _context.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date >= monthStart)
                .CountAsync();

            var totalCount = await _context.Log.CountAsync();

            return Json(new {
                today = todayCount,
                week = weekCount,
                month = monthCount,
                total = totalCount
            });
        }

        [HttpPost]
        public async Task<IActionResult> ClearOldLogs()
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-30);
                var oldLogs = _context.Log.Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value < cutoffDate);
                var deletedCount = await oldLogs.CountAsync();
                
                _context.Log.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                return Json(new { success = true, deletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoginChart()
        {
            var endDate = DateTime.Today; // วันที่ปัจจุบัน
            //var startDate = endDate.AddDays(-29); // 30 วันย้อนหลัง
            var startDate = endDate.AddDays(-6);

            var dailyLoginCounts = await _context.Log
                .Where(l => l.LogDateTime.HasValue &&
                            l.LogDateTime.Value.Date >= startDate &&
                            l.LogDateTime.Value.Date <= endDate)
                .GroupBy(l => l.LogDateTime.Value.Date)
                .Select(g => new {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            var labels = new List<string>();
            var dataPoints = new List<int>();

            // สร้างข้อมูลสำหรับทุกวันใน 30 วันที่ผ่านมา
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                labels.Add(date.ToString("dd/MM")); // รูปแบบ "25/07"
                var count = dailyLoginCounts.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
                dataPoints.Add(count);
            }

            return Json(new
            {
                labels = labels,
                data = dataPoints,
                title = $"จำนวนการเข้าสู่ระบบย้อนหลัง 30 วัน"
            });
        }
    }
}
