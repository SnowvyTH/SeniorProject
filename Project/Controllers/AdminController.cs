using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models.Entity;
using Project.Data;
using Project.Services;

namespace Project.Controllers
{
    [Authorize(Roles = "ผู้ดูแลระบบ")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _passwordService;

        public AdminController(ApplicationDbContext db, IPasswordService passwordService)
        {
            _db = db;
            _passwordService = passwordService;
        }

        private async Task SetUserProfileData()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _db.User.FindAsync(int.Parse(userId));
                if (user != null && !string.IsNullOrEmpty(user.Picture))
                {
                    ViewBag.ProfileImageUrl = "/uploads/profile/" + user.Picture;
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            await SetUserProfileData();
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

            // ดึงข้อมูลสถิติ
            var patientCount = await _db.Patient.CountAsync();
            var userCount = await _db.User.CountAsync();
            var formCount = await _db.Form.CountAsync();
            var todayLogins = await _db.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date == DateTime.Today)
                .CountAsync();

            ViewBag.PatientCount = patientCount;
            ViewBag.UserCount = userCount;
            ViewBag.FormCount = formCount;
            ViewBag.TodayLogins = todayLogins;

            return View();
        }

        public async Task<IActionResult> Users()
        {
            await SetUserProfileData();
            var roleId = User.FindFirst("RoleID")?.Value;

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(string search = "", string roleFilter = "", string statusFilter = "")
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            var query = _db.User.Include(u => u.Title).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FirstName.Contains(search) || 
                                       u.LastName.Contains(search) || 
                                       u.Username.Contains(search) ||
                                       u.Title.TitleName.Contains(search));
            }

            if (!string.IsNullOrEmpty(roleFilter) && int.TryParse(roleFilter, out int roleInt))
            {
                query = query.Where(u => u.RoleID == roleInt);
            }

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
                isActive = u.IsActive,
                profilePictureUrl = string.IsNullOrEmpty(u.Picture) ? null : $"/uploads/profile/{u.Picture}"
            }).OrderBy(u => u.userID).ToListAsync();

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var roleId = User.FindFirst("RoleID")?.Value;

            var user = await _db.User.Include(u => u.Title)
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
                if (string.IsNullOrEmpty(form["Username"]) || string.IsNullOrEmpty(form["Password"]) ||
                    string.IsNullOrEmpty(form["FirstName"]) || string.IsNullOrEmpty(form["LastName"]) ||
                    string.IsNullOrEmpty(form["TitleID"]) || string.IsNullOrEmpty(form["RoleID"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                var existingUser = await _db.User.FirstOrDefaultAsync(u => u.Username == form["Username"].ToString());
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "ชื่อผู้ใช้นี้มีอยู่แล้ว" });
                }

                var hashedPassword = _passwordService.HashPassword(form["Password"].ToString()!);

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

                _db.User.Add(user);
                await _db.SaveChangesAsync();

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
                var user = await _db.User.FindAsync(userId);
                
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบผู้ใช้" });
                }

                if (string.IsNullOrEmpty(form["Username"]) || string.IsNullOrEmpty(form["FirstName"]) ||
                    string.IsNullOrEmpty(form["LastName"]) || string.IsNullOrEmpty(form["TitleID"]) ||
                    string.IsNullOrEmpty(form["RoleID"]))
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                var existingUser = await _db.User.FirstOrDefaultAsync(u => u.Username == form["Username"].ToString() && u.UserID != userId);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "ชื่อผู้ใช้นี้มีอยู่แล้ว" });
                }

                user.TitleID = int.Parse(form["TitleID"].ToString()!);
                user.FirstName = form["FirstName"].ToString();
                user.LastName = form["LastName"].ToString();
                user.Username = form["Username"].ToString()!;
                user.RoleID = int.Parse(form["RoleID"].ToString()!);

                if (!string.IsNullOrEmpty(form["Password"]))
                {
                    user.PasswordHash = _passwordService.HashPassword(form["Password"].ToString()!);
                }

                await _db.SaveChangesAsync();

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
                var user = await _db.User.FindAsync(userID);
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบผู้ใช้" });
                }

                user.IsActive = isActive;
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Titles()
        {
            await SetUserProfileData();
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

            var query = _db.Title.AsQueryable();

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
                var existingTitle = await _db.Title.FirstOrDefaultAsync(t => t.TitleName == form["TitleName"].ToString());
                if (existingTitle != null)
                {
                    return Json(new { success = false, message = "คำนำหน้านี้มีอยู่แล้ว" });
                }

                var title = new Title
                {
                    TitleName = form["TitleName"].ToString()!
                };

                _db.Title.Add(title);
                await _db.SaveChangesAsync();

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
                var title = await _db.Title.FindAsync(titleId);
                
                if (title == null)
                {
                    return Json(new { success = false, message = "ไม่พบคำนำหน้า" });
                }

                var existingTitle = await _db.Title.FirstOrDefaultAsync(t => t.TitleName == form["TitleName"].ToString() && t.TitleID != titleId);
                if (existingTitle != null)
                {
                    return Json(new { success = false, message = "คำนำหน้านี้มีอยู่แล้ว" });
                }

                title.TitleName = form["TitleName"].ToString()!;
                await _db.SaveChangesAsync();

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
                var title = await _db.Title.FindAsync(titleID);
                if (title == null)
                {
                    return Json(new { success = false, message = "ไม่พบคำนำหน้า" });
                }

                var usersWithTitle = await _db.User.CountAsync(u => u.TitleID == titleID);
                if (usersWithTitle > 0)
                {
                    return Json(new { success = false, message = $"ไม่สามารถลบได้ เนื่องจากมีผู้ใช้ {usersWithTitle} คนที่ใช้คำนำหน้านี้อยู่" });
                }

                _db.Title.Remove(title);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Treatments()
        {
            await SetUserProfileData();
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

            var query = _db.Treatment.AsQueryable();

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
                var existingTreatment = await _db.Treatment.FirstOrDefaultAsync(t => t.TreatmentName == form["TreatmentName"].ToString());
                if (existingTreatment != null)
                {
                    return Json(new { success = false, message = "สิทธิ์การรักษานี้มีอยู่แล้ว" });
                }

                var treatment = new Treatment
                {
                    TreatmentName = form["TreatmentName"].ToString()!
                };

                _db.Treatment.Add(treatment);
                await _db.SaveChangesAsync();

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
                var treatment = await _db.Treatment.FindAsync(treatmentId);
                
                if (treatment == null)
                {
                    return Json(new { success = false, message = "ไม่พบสิทธิ์การรักษา" });
                }

                // Check if treatment name is already taken by another treatment
                var existingTreatment = await _db.Treatment.FirstOrDefaultAsync(t => t.TreatmentName == form["TreatmentName"].ToString() && t.TreatmentID != treatmentId);
                if (existingTreatment != null)
                {
                    return Json(new { success = false, message = "สิทธิ์การรักษานี้มีอยู่แล้ว" });
                }

                treatment.TreatmentName = form["TreatmentName"].ToString()!;
                await _db.SaveChangesAsync();

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
                var treatment = await _db.Treatment.FindAsync(treatmentID);
                if (treatment == null)
                {
                    return Json(new { success = false, message = "ไม่พบสิทธิ์การรักษา" });
                }

                // Check if treatment is being used by any patient
                var patientsWithTreatment = await _db.Patient.CountAsync(p => p.TreatmentID == treatmentID);
                if (patientsWithTreatment > 0)
                {
                    return Json(new { success = false, message = $"ไม่สามารถลบได้ เนื่องจากมีผู้ป่วย {patientsWithTreatment} คนที่ใช้สิทธิ์การรักษานี้อยู่" });
                }

                _db.Treatment.Remove(treatment);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Logs()
        {
            await SetUserProfileData();
            var roleId = User.FindFirst("RoleID")?.Value;

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
        
            var query = _db.Log
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
                    username = l.User.Username,
                    logDateTime = l.LogDateTime,
                    titleName = l.User != null && l.User.Title != null ? l.User.Title.TitleName : "",
                    firstName = l.User != null ? l.User.FirstName : "",
                    lastName = l.User != null ? l.User.LastName : "",
                    roleID = l.User != null ? l.User.RoleID : 0
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

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todayCount = await _db.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date == today)
                .CountAsync();

            var weekCount = await _db.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date >= weekStart)
                .CountAsync();

            var monthCount = await _db.Log
                .Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value.Date >= monthStart)
                .CountAsync();

            var totalCount = await _db.Log.CountAsync();

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
                var oldLogs = _db.Log.Where(l => l.LogDateTime.HasValue && l.LogDateTime.Value < cutoffDate);
                var deletedCount = await oldLogs.CountAsync();
                
                _db.Log.RemoveRange(oldLogs);
                await _db.SaveChangesAsync();

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
            var startDate = endDate.AddDays(-11);

            var dailyLoginCounts = await _db.Log
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

        [HttpGet]
        public async Task<IActionResult> UserRoleChart()
        {
            try
            {
                var roleStats = await _db.User
                    .Where(u => u.RoleID.HasValue)
                    .GroupBy(u => u.RoleID)
                    .Select(g => new {
                        RoleID = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var labels = new List<string>();
                var dataPoints = new List<int>();
                var backgroundColors = new List<string>();

                foreach (var stat in roleStats.OrderBy(s => s.RoleID))
                {
                    string roleName = "";
                    string color = "";
                    
                    switch (stat.RoleID)
                    {
                        case 1:
                            roleName = "เภสัช";
                            color = "rgba(81, 195, 183, 0.8)"; // Mint green
                            break;
                        case 2:
                            roleName = "หัวหน้าแผนกเภสัช";
                            color = "rgba(34, 197, 94, 0.8)"; // Green
                            break;
                        case 3:
                            roleName = "ผู้ดูแลระบบ";
                            color = "#06695f";
                            break;
                        default:
                            roleName = "อื่นๆ";
                            color = "rgba(239, 68, 68, 0.8)"; // Red
                            break;
                    }

                    labels.Add(roleName);
                    dataPoints.Add(stat.Count);
                    backgroundColors.Add(color);
                }

                return Json(new
                {
                    labels = labels,
                    data = dataPoints,
                    backgroundColor = backgroundColors
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Profile Picture Management for Admin
        [HttpPost]
        public async Task<IActionResult> UploadUserProfilePicture(int userId, IFormFile profilePicture)
        {
            try
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                }

                if (profilePicture == null || profilePicture.Length == 0)
                {
                    return Json(new { success = false, message = "ไม่พบไฟล์รูปภาพ" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(profilePicture.ContentType.ToLower()))
                {
                    return Json(new { success = false, message = "รูปแบบไฟล์ไม่ถูกต้อง (อนุญาตเฉพาะ JPG, PNG, GIF)" });
                }

                // Validate file size (5MB)
                if (profilePicture.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "ขนาดไฟล์ใหญ่เกินไป (สูงสุด 5MB)" });
                }

                // Create uploads directory if not exists
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Remove old profile picture if exists
                if (!string.IsNullOrEmpty(user.Picture))
                {
                    var oldFilePath = Path.Combine(uploadsPath, user.Picture);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Generate filename using UserID
                var fileName = $"{user.UserID}{Path.GetExtension(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                // Update user profile picture in database
                user.Picture = fileName;
                await _db.SaveChangesAsync();

                var pictureUrl = $"/uploads/profile/{fileName}";
                
                // Set cache headers to prevent caching of profile pictures
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Json(new { 
                    success = true, 
                    message = "บันทึกรูปภาพเรียบร้อยแล้ว",
                    pictureUrl = pictureUrl + "?t=" + DateTime.Now.Ticks
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกรูปภาพ" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUserProfilePicture(int userId)
        {
            try
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                }

                // Remove file if exists
                if (!string.IsNullOrEmpty(user.Picture))
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");
                    var filePath = Path.Combine(uploadsPath, user.Picture);
                    
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    // Update database
                    user.Picture = null;
                    await _db.SaveChangesAsync();
                }

                return Json(new { 
                    success = true, 
                    message = "ลบรูปภาพเรียบร้อยแล้ว"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการลบรูปภาพ" });
            }
        }

        // Admin Profile Picture for Navbar
        [HttpGet]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                }

                var user = await _db.User.FirstOrDefaultAsync(u => u.UserID == int.Parse(userId));
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                }

                var profilePictureUrl = string.IsNullOrEmpty(user.Picture) 
                    ? null 
                    : $"/uploads/profile/{user.Picture}";

                return Json(new { 
                    success = true,
                    profilePictureUrl = profilePictureUrl
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาด" });
            }
        }
    }
}
