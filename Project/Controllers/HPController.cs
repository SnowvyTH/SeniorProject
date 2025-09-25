using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models.Entity;

namespace Project.Controllers
{
    [Authorize]
    public class HPController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HPController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? startDate = null, string? endDate = null, string? formType = null)
        {
            // ตรวจสอบสิทธิ์ - ต้องเป็น RoleID = 2 เท่านั้น
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // ดึงข้อมูลผู้ใช้และรูปโปรไฟล์
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            // ดึง UserID และรูปโปรไฟล์
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var user = await _context.User.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Picture))
                {
                    ViewBag.ProfileImageUrl = $"/uploads/profile/{user.Picture}";
                }
                else
                {
                    ViewBag.ProfileImageUrl = null;
                }
            }
            else
            {
                ViewBag.ProfileImageUrl = null;
            }

            // กำหนดค่า default: 7 วันที่ผ่านมา และ formID = 8
            DateTime startDateParsed = DateTime.Today.AddDays(-7);
            DateTime endDateParsed = DateTime.Today.AddDays(1);
            string? defaultFormType = formType;

            // ถ้าไม่มีการระบุ formType ให้ใช้ form ที่มี FormID = 8
            if (string.IsNullOrEmpty(defaultFormType))
            {
                var defaultForm = await _context.Form.FirstOrDefaultAsync(f => f.FormID == 8);
                defaultFormType = defaultForm?.FormName ?? "";
            }

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStart))
                startDateParsed = parsedStart;
            
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
                endDateParsed = parsedEnd.AddDays(1); // Include the end date

            // ส่งค่า filter ไปยัง view
            ViewBag.StartDate = startDateParsed.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDateParsed.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.FormType = defaultFormType;

            // เตรียมข้อมูลสำหรับ Dashboard - ใช้ static data เพื่อความเรียบง่าย
            ViewBag.TotalPatients = await _context.Patient.CountAsync();
            ViewBag.PatientsAssessed = await _context.Evaluation.Select(e => e.HN).Distinct().CountAsync();
            ViewBag.AverageScore = 8.5;
            ViewBag.SevereCases = await _context.Evaluation.CountAsync(e => e.Result != null && 
                (e.Result.Contains("รุนแรง") || e.Result.Contains("Severe")));

            // ดึงรายการ Forms
            var forms = await _context.Form.Select(f => new { f.FormID, f.FormName }).ToListAsync();
            ViewBag.Forms = forms;

            return View();
        }

        public async Task<IActionResult> Patients()
        {
            // ตรวจสอบสิทธิ์ - ต้องเป็น RoleID = 2 เท่านั้น
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // ดึงข้อมูลผู้ใช้และรูปโปรไฟล์
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            // ดึง UserID และรูปโปรไฟล์
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var user = await _context.User.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Picture))
                {
                    ViewBag.ProfileImageUrl = $"/uploads/profile/{user.Picture}";
                }
                else
                {
                    ViewBag.ProfileImageUrl = null;
                }
            }

            // ดึงข้อมูลผู้ป่วยพร้อมประวัติการประเมิน
            var patients = await _context.Patient
                .Include(p => p.Title)
                .Include(p => p.Evaluations)
                    .ThenInclude(r => r.Form)
                .Include(p => p.Evaluations)
                    .ThenInclude(r => r.User)
                    .ThenInclude(u => u!.Title)
                .OrderBy(p => p.HN)
                .ToListAsync();

            return View(patients);
        }

        public async Task<IActionResult> EvaluationDetail(int id)
        {
            // ตรวจสอบสิทธิ์ - ต้องเป็น RoleID = 2 เท่านั้น
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // ดึงข้อมูลผู้ใช้และรูปโปรไฟล์
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
            // ดึง UserID และรูปโปรไฟล์
            var userIdClaim = User.FindFirst("UserID")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var user = await _context.User.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Picture))
                {
                    ViewBag.ProfileImageUrl = $"/uploads/profile/{user.Picture}";
                }
                else
                {
                    ViewBag.ProfileImageUrl = null;
                }
            }

            var evaluation = await _context.Evaluation
                .Include(e => e.Patient)
                    .ThenInclude(p => p!.Title)
                .Include(e => e.Form)
                    .ThenInclude(f => f!.Questions)
                    .ThenInclude(q => q.Options)
                .Include(e => e.User)
                    .ThenInclude(u => u!.Title)
                .Include(e => e.EvaluationDetails)
                    .ThenInclude(ed => ed.Question)
                    .ThenInclude(q => q!.Options)
                .FirstOrDefaultAsync(e => e.EvaID == id);

            if (evaluation == null)
            {
                return NotFound();
            }

            // คำนวณคะแนนรวม
            if (evaluation.EvaluationDetails?.Any() == true && evaluation.Form != null)
            {
                var scores = evaluation.EvaluationDetails.Where(ed => ed.Score.HasValue).Select(ed => ed.Score!.Value).ToList();
                if (scores.Any())
                {
                    decimal calculatedScore = evaluation.Form.Scoring?.ToLower() switch
                    {
                        "max" => scores.Max(),
                        "min" => scores.Min(),
                        "sum" => scores.Sum(),
                        _ => scores.Sum()
                    };

                    ViewBag.InterpreterName = evaluation.Result;
                    ViewBag.InterpreterText = evaluation.ResultDetail; 
                    ViewBag.CalculatedScore = calculatedScore;
                }
            }

            return View(evaluation);
        }

        private async Task<List<HighRiskPatientData>> GetHighRiskPatients()
        {
            var severityLevels = new[] { "รุนแรง", "Severe", "Severe akathisia" };
            
            var patients = await _context.Evaluation
                .Include(e => e.Patient)
                .Include(e => e.User)
                .Include(e => e.EvaluationDetails)
                .Include(e => e.Form)
                .Where(e => e.Patient != null && severityLevels.Contains(e.Result))
                .OrderByDescending(e => e.RecordDate)
                .GroupBy(e => e.Patient!.HN)
                .Select(g => g.OrderByDescending(e => e.RecordDate).First())
                .ToListAsync();

            return patients.Select(e => new HighRiskPatientData
            {
                HN = e.Patient!.HN,
                FirstName = e.Patient.FirstName,
                LastName = e.Patient.LastName,
                BirthDate = e.Patient.BirthDate,
                Result = e.Result ?? "",
                RecordDate = e.RecordDate,
                EvaluatorName = $"{e.User?.FirstName} {e.User?.LastName}".Trim(),
                TotalScore = e.EvaluationDetails.Sum(d => d.Score ?? 0),
                EvaluationId = e.EvaID,
                FormName = e.Form?.FormName ?? ""
            }).ToList();
        }

        [HttpGet]
        public IActionResult GetChartData(string period = "month", string ward = "")
        {
            try
            {
                // ส่งข้อมูลง่ายๆ สำหรับ chart
                return Json(new { 
                    success = true, 
                    message = "Chart data loaded",
                    trendLabels = new[] { "สัปดาห์ 1", "สัปดาห์ 2", "สัปดาห์ 3", "สัปดาห์ 4" },
                    trendData = new[] { 8.5, 9.2, 10.1, 10.2 },
                    distributionData = new[] { 5, 8, 12 }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetScoreDistribution(string? startDate = null, string? endDate = null, string? formType = null)
        {
            try
            {
                // กำหนดช่วงวันที่ default
                DateTime startDateParsed = DateTime.Today.AddDays(-7);
                DateTime endDateParsed = DateTime.Today.AddDays(1);

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStart))
                    startDateParsed = parsedStart;
                
                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
                    endDateParsed = parsedEnd.AddDays(1);

                // ใช้ query ที่เรียบง่ายกว่า
                var evaluations = await _context.Evaluation
                    .Include(e => e.Form)
                    .Where(e => e.RecordDate >= startDateParsed && 
                               e.RecordDate < endDateParsed &&
                               (string.IsNullOrEmpty(formType) || e.Form!.FormName == formType))
                    .Select(e => new { e.Result, FormName = e.Form!.FormName })
                    .ToListAsync();

                // จัดกลุ่มข้อมูลตามระดับความรุนแรง 3 ระดับ
                var severityGroups = new Dictionary<string, int>
                {
                    { "ความรุนแรงน้อย", 0 },
                    { "ความรุนแรงปานกลาง", 0 },
                    { "ความรุนแรงมาก", 0 }
                };

                foreach (var evaluation in evaluations)
                {
                    var severityLevel = GetSeverityLevel(evaluation.Result ?? "");
                    if (severityGroups.ContainsKey(severityLevel))
                    {
                        severityGroups[severityLevel]++;
                    }
                }

                // สร้าง distribution data
                var distribution = new List<object>
                {
                    new { label = "ความรุนแรงน้อย", count = severityGroups["ความรุนแรงน้อย"], color = "#28a745" },
                    new { label = "ความรุนแรงปานกลาง", count = severityGroups["ความรุนแรงปานกลาง"], color = "#ffc107" },
                    new { label = "ความรุนแรงมาก", count = severityGroups["ความรุนแรงมาก"], color = "#dc3545" }
                };

                // กรองออกรายการที่มีค่า 0
                distribution = distribution.Where(d => ((dynamic)d).count > 0).ToList();

                return Json(distribution);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetFormTypeDistribution(string? startDate = null, string? endDate = null)
        {
            try
            {
                // กำหนดค่า default: 7 วันที่ผ่านมา
                DateTime startDateParsed = DateTime.Today.AddDays(-7);
                DateTime endDateParsed = DateTime.Today.AddDays(1);

                if (DateTime.TryParse(startDate, out DateTime parsedStart))
                {
                    startDateParsed = parsedStart.Date;
                }

                if (DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    endDateParsed = parsedEnd.Date.AddDays(1);
                }

                // นับจำนวนการประเมินตามประเภทแบบฟอร์ม
                var formTypeCounts = await _context.Evaluation
                    .Include(e => e.Form)
                    .Where(e => e.RecordDate >= startDateParsed && e.RecordDate < endDateParsed)
                    .GroupBy(e => e.Form!.FormName)
                    .Select(g => new { 
                        formType = g.Key, 
                        count = g.Count() 
                    })
                    .ToListAsync();

                return Json(formTypeCounts);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDashboardData(string? startDate = null, string? endDate = null, string? formType = null)
        {
            try
            {
                // กำหนดค่า default: 7 วันที่ผ่านมา
                DateTime startDateParsed = DateTime.Today.AddDays(-7);
                DateTime endDateParsed = DateTime.Today.AddDays(1);

                if (DateTime.TryParse(startDate, out DateTime parsedStart))
                {
                    startDateParsed = parsedStart;
                }

                if (DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    endDateParsed = parsedEnd.AddDays(1); // เพิ่ม 1 วันเพื่อรวมวันสุดท้าย
                }

                // ดึงข้อมูลการประเมินตามช่วงวันที่
                var evaluationsQuery = _context.Evaluation
                    .Include(e => e.Patient)
                    .Include(e => e.Form)
                    .Include(e => e.EvaluationDetails)
                    .Where(e => e.RecordDate >= startDateParsed && e.RecordDate < endDateParsed);

                // กรองตามแบบฟอร์มถ้ามี
                if (!string.IsNullOrEmpty(formType))
                {
                    evaluationsQuery = evaluationsQuery.Where(e => e.Form!.FormName == formType);
                }

                var evaluations = await evaluationsQuery.ToListAsync();

                // คำนวณ KPI
                var totalPatients = await _context.Patient.CountAsync();
                var patientsAssessed = evaluations.Select(e => e.HN).Distinct().Count();
                
                // คำนวณคะแนนรวมจาก EvaluationDetails
                var averageScore = 0.0;
                if (evaluations.Any())
                {
                    var totalScores = evaluations.Select(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0)).ToList();
                    averageScore = totalScores.Any() ? totalScores.Average() : 0;
                }
                
                var severeCases = evaluations.Count(e => e.Result == "รุนแรง" || e.Result == "Severe" || e.Result == "Severe akathisia");

                // คำนวณ trend (เปรียบเทียบกับช่วงก่อนหน้า)
                var daysDiff = (endDateParsed - startDateParsed).Days;
                var previousStartDate = startDateParsed.AddDays(-daysDiff);
                var previousEvaluations = await _context.Evaluation
                    .Include(e => e.EvaluationDetails)
                    .Where(e => e.RecordDate >= previousStartDate && e.RecordDate < startDateParsed)
                    .ToListAsync();

                var previousAvgScore = 0.0;
                if (previousEvaluations.Any())
                {
                    var previousTotalScores = previousEvaluations.Select(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0)).ToList();
                    previousAvgScore = previousTotalScores.Any() ? previousTotalScores.Average() : 0;
                }
                
                var trendPercentage = previousAvgScore > 0 ? ((averageScore - previousAvgScore) / previousAvgScore * 100) : 0;
                var trendIndicator = trendPercentage > 0 ? $"+{trendPercentage:F1}%" : $"{trendPercentage:F1}%";

                // ข้อมูลสำหรับกราฟ trend
                var trendData = new List<object>();
                var currentDate = startDateParsed;
                
                while (currentDate < endDateParsed.AddDays(-1))
                {
                    var dayEvaluations = evaluations.Where(e => e.RecordDate.Date == currentDate.Date).ToList();
                    
                    // คำนวณค่าเฉลี่ยสำหรับแต่ละแบบฟอร์ม
                    var modSasEvals = dayEvaluations.Where(e => e.Form?.FormName == "Mod-SAS").ToList();
                    var modAimsEvals = dayEvaluations.Where(e => e.Form?.FormName == "Mod-AIMS").ToList();
                    var modBarsEvals = dayEvaluations.Where(e => e.Form?.FormName == "Mod-BARS").ToList();
                    
                    var dateStr = currentDate.ToString("yyyy-MM-dd");
                    
                    if (modSasEvals.Any())
                    {
                        var avgScore = modSasEvals.Average(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0));
                        trendData.Add(new { date = dateStr, formType = "Mod-SAS", averageScore = Math.Round(avgScore, 2) });
                    }
                    if (modAimsEvals.Any())
                    {
                        var avgScore = modAimsEvals.Average(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0));
                        trendData.Add(new { date = dateStr, formType = "Mod-AIMS", averageScore = Math.Round(avgScore, 2) });
                    }
                    if (modBarsEvals.Any())
                    {
                        var avgScore = modBarsEvals.Average(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0));
                        trendData.Add(new { date = dateStr, formType = "Mod-BARS", averageScore = Math.Round(avgScore, 2) });
                    }
                    
                    currentDate = currentDate.AddDays(1);
                }

                var result = new
                {
                    patientsAssessed = patientsAssessed,
                    totalPatients = totalPatients,
                    averageScore = Math.Round(averageScore, 1),
                    trendIndicator = trendIndicator,
                    severeCases = severeCases,
                    trendData = trendData
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private string GetSeverityLevel(string result)
        {
            if (string.IsNullOrEmpty(result))
                return "ความรุนแรงน้อย";

            // ความรุนแรงน้อย = สีเขียว
            var lowSeverity = new[] { "ปกติ", "Absent", "Questionable", "Mild akathisia", "Mild", "น้อยมาก", "ไม่พบอาการ" };
            
            // ความรุนแรงปานกลาง = สีเหลือง
            var moderateSeverity = new[] { "Moderate", "Moderate akathisia", "เล็กน้อย", "ปานกลาง" };
            
            // ความรุนแรงมาก = สีแดง
            var highSeverity = new[] { "Marked akathisia", "Severe", "Severe akathisia", "รุนแรง" };

            if (lowSeverity.Contains(result))
                return "ความรุนแรงน้อย";
            else if (moderateSeverity.Contains(result))
                return "ความรุนแรงปานกลาง";
            else if (highSeverity.Contains(result))
                return "ความรุนแรงมาก";
            else
                return "ความรุนแรงน้อย"; // default
        }

        private string GetSeverityColor(string result, string formName)
        {
            return formName switch
            {
                "Mod-SAS" => result switch
                {
                    "ปกติ" => "#28a745", // เขียว
                    "Mild" => "#28a745", // เขียว
                    "Moderate" => "#ffc107", // เหลือง
                    "Severe" => "#dc3545", // แดง
                    _ => "#6c757d" // เทา
                },
                "Mod-BARS" => result switch
                {
                    "Absent" => "#28a745", // เขียว
                    "Questionable" => "#28a745", // เขียว
                    "Mild akathisia" => "#ffc107", // เหลือง
                    "Moderate akathisia" => "#ffc107", // เหลือง
                    "Marked akathisia" => "#dc3545", // แดง
                    "Severe akathisia" => "#dc3545", // แดง
                    _ => "#6c757d" // เทา
                },
                "Mod-AIMS" => result switch
                {
                    "ไม่พบอาการ" => "#28a745", // เขียว
                    "Mild" => "#28a745", // เขียว
                    "เล็กน้อย" => "#28a745", // เขียว
                    "ปานกลาง" => "#ffc107", // เหลือง
                    "รุนแรง" => "#dc3545", // แดง
                    _ => "#6c757d" // เทา
                },
                _ => "#6c757d" // เทาสำหรับที่ไม่ระบุ
            };
        }

        [HttpGet]
        public async Task<JsonResult> GetRealDashboardData(string? startDate = null, string? endDate = null, string? formType = null)
        {
            try
            {
                // กำหนดค่า default: 7 วันที่ผ่านมา
                DateTime startDateParsed = DateTime.Today.AddDays(-7);
                DateTime endDateParsed = DateTime.Today.AddDays(1);

                if (DateTime.TryParse(startDate, out DateTime parsedStart))
                {
                    startDateParsed = parsedStart.Date; // ใช้เฉพาะวันที่
                }

                if (DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    endDateParsed = parsedEnd.Date.AddDays(1); // เพิ่ม 1 วันและใช้เฉพาะวันที่
                }

                // ดึงข้อมูลการประเมินตามช่วงวันที่ (ใช้ .Date เพื่อเปรียบเทียบเฉพาะวันที่)
                var evaluationsQuery = _context.Evaluation
                    .Include(e => e.Patient)
                    .Include(e => e.Form)
                    .Include(e => e.EvaluationDetails)
                    .Where(e => e.RecordDate.Date >= startDateParsed.Date && e.RecordDate.Date < endDateParsed.Date);

                // กรองตามแบบฟอร์มถ้ามี
                if (!string.IsNullOrEmpty(formType))
                {
                    evaluationsQuery = evaluationsQuery.Where(e => e.Form!.FormName == formType);
                }

                var evaluations = await evaluationsQuery.ToListAsync();

                // คำนวณ KPI
                var totalPatients = await _context.Patient.CountAsync();
                var patientsAssessed = evaluations.Select(e => e.HN).Distinct().Count();
                
                // คำนวณคะแนนรวมจาก EvaluationDetails
                var averageScore = 0.0;
                if (evaluations.Any())
                {
                    var totalScores = evaluations.Select(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0)).ToList();
                    averageScore = totalScores.Any() ? totalScores.Average() : 0;
                }
                
                var severeCases = evaluations.Count(e => e.Result == "รุนแรง" || e.Result == "Severe" || e.Result == "Severe akathisia");

                // คำนวณ trend (เปรียบเทียบกับช่วงก่อนหน้า)
                var daysDiff = (endDateParsed - startDateParsed).Days;
                var previousStartDate = startDateParsed.AddDays(-daysDiff);
                var previousEvaluations = await _context.Evaluation
                    .Include(e => e.EvaluationDetails)
                    .Where(e => e.RecordDate >= previousStartDate && e.RecordDate < startDateParsed)
                    .ToListAsync();

                var previousAvgScore = 0.0;
                if (previousEvaluations.Any())
                {
                    var previousTotalScores = previousEvaluations.Select(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0)).ToList();
                    previousAvgScore = previousTotalScores.Any() ? previousTotalScores.Average() : 0;
                }
                
                var trendPercentage = previousAvgScore > 0 ? ((averageScore - previousAvgScore) / previousAvgScore * 100) : 0;
                var trendIndicator = trendPercentage > 0 ? $"+{trendPercentage:F1}%" : $"{trendPercentage:F1}%";

                // ข้อมูลสำหรับกราฟ trend - ดึงแบบฟอร์มทั้งหมดจากฐานข้อมูล
                var availableForms = await _context.Form.Select(f => f.FormName).Distinct().ToListAsync();
                var trendData = new List<object>();
                var currentDate = startDateParsed.Date;
                
                while (currentDate < endDateParsed.Date)
                {
                    var dayEvaluations = evaluations.Where(e => e.RecordDate.Date == currentDate.Date).ToList();
                    var dateStr = currentDate.ToString("yyyy-MM-dd");
                    
                    // คำนวณค่าเฉลี่ยสำหรับแต่ละแบบฟอร์มที่มีในระบบ
                    foreach (var formName in availableForms)
                    {
                        var formEvals = dayEvaluations.Where(e => e.Form?.FormName == formName).ToList();
                        if (formEvals.Any())
                        {
                            var avgScore = formEvals.Average(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0));
                            trendData.Add(new { date = dateStr, formType = formName, averageScore = Math.Round(avgScore, 2) });
                        }
                    }
                    
                    currentDate = currentDate.AddDays(1);
                }

                var result = new
                {
                    patientsAssessed = patientsAssessed,
                    totalPatients = totalPatients,
                    totalEvaluations = evaluations.Count,
                    averageScore = Math.Round(averageScore, 1),
                    trendIndicator = trendIndicator,
                    severeCases = severeCases,
                    trendData = trendData
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableForms()
        {
            try
            {
                var forms = await _context.Form
                    .OrderBy(f => f.FormName)
                    .Select(f => new {
                        formId = f.FormID,
                        formName = f.FormName,
                        formType = f.FormType,
                        scoring = f.Scoring
                    })
                    .ToListAsync();

                return Json(forms);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private string GetSeverityLevelColor(string severityLevel)
        {
            return severityLevel switch
            {
                "ความรุนแรงน้อย" => "#28a745", // เขียว
                "ความรุนแรงปานกลาง" => "#ffc107", // เหลือง
                "ความรุนแรงมาก" => "#dc3545", // แดง
                _ => "#6c757d" // เทา
            };
        }

        private int GetSeverityOrder(string severityLevel)
        {
            return severityLevel switch
            {
                "ความรุนแรงน้อย" => 1,
                "ความรุนแรงปานกลาง" => 2,
                "ความรุนแรงมาก" => 3,
                _ => 4
            };
        }

    }

    // Data Models for Dashboard
    public class DashboardData
    {
        public int TotalPatients { get; set; }
        public int PatientsAssessed { get; set; }
        public double AverageScore { get; set; }
        public double TrendPercentage { get; set; }
        public int SevereCases { get; set; }
        public List<TopPatient> TopPatients { get; set; } = new();
        public List<TopPharmacist> TopPharmacists { get; set; } = new();
        public List<RecentAlert> RecentAlerts { get; set; } = new();
    }

    public class TopPatient
    {
        public Project.Models.Entity.Patient? Patient { get; set; }
        public int LatestScore { get; set; }
    }

    public class TopPharmacist
    {
        public Project.Models.Entity.User? User { get; set; }
        public int AssessmentCount { get; set; }
    }

    public class RecentAlert
    {
        public Project.Models.Entity.Patient? Patient { get; set; }
        public int Score { get; set; }
        public DateTime RecordDate { get; set; }
        public string Severity { get; set; } = "";
    }

    public class HighRiskPatientData
    {
        public string HN { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Result { get; set; } = "";
        public DateTime RecordDate { get; set; }
        public string EvaluatorName { get; set; } = "";
        public int TotalScore { get; set; }
        public int EvaluationId { get; set; }
        public string FormName { get; set; } = "";
        
        public int Age => BirthDate.HasValue ? DateTime.Now.Year - BirthDate.Value.Year : 0;
        public string FullName => $"{FirstName} {LastName}".Trim();
        
        public string SeverityLevel
        {
            get
            {
                return Result switch
                {
                    "รุนแรง" => "danger",
                    "Severe" => "danger", 
                    "Severe akathisia" => "danger",
                    _ => "warning"
                };
            }
        }
    }
}
