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
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.Forms = await _db.Form.ToListAsync();

            var patients = await _db.Patient
                .Include(p => p.Title)
                .Include(p => p.Evaluations)
                    .ThenInclude(r => r.Form)
                .Include(p => p.Evaluations)
                    .ThenInclude(r => r.User)
                    .ThenInclude(u => u!.Title)
                .ToListAsync();

            return View(patients);
        }

        public IActionResult Forms()
        {
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

            return View();
        }

        // Form Management API Actions
        [HttpGet]
        public async Task<IActionResult> GetForms(string search = "")
        {
            var query = _db.Form.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f => f.FormName != null && f.FormName.Contains(search));
            }

            var forms = await query.Select(f => new
            {
                formID = f.FormID,
                formName = f.FormName,
                formType = f.FormType,
                questionCount = f.Questions.Count(),
                evaluationCount = f.Evaluations.Count()
            }).OrderBy(f => f.formID).ToListAsync();

            return Json(forms);
        }

        [HttpGet]
        public async Task<IActionResult> GetForm(int id)
        {
            var form = await _db.Form
                .Where(f => f.FormID == id)
                .Select(f => new
                {
                    formID = f.FormID,
                    formName = f.FormName,
                    formType = f.FormType
                }).FirstOrDefaultAsync();

            if (form == null) return Json(new { success = false, message = "Form not found" });

            return Json(form);
        }

        [HttpGet]
        public async Task<IActionResult> GetFormStats()
        {
            var totalForms = await _db.Form.CountAsync();
            var formsWithQuestions = await _db.Form.CountAsync(f => f.Questions.Any());
            var totalQuestions = await _db.Question.CountAsync();
            var totalEvaluations = await _db.Evaluation.CountAsync();

            return Json(new
            {
                totalForms = totalForms,
                formsWithQuestions = formsWithQuestions,
                totalQuestions = totalQuestions,
                totalEvaluations = totalEvaluations
            });
        }

        public async Task<IActionResult> CreateForm()
        {
            ViewBag.UserName = User.FindFirst("FullName")?.Value;
            ViewBag.Forms = await _db.Form.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateForm([FromBody] CreateFormModel model)
        {
            try
            {
                var existingForm = await _db.Form
                    .FirstOrDefaultAsync(f => f.FormName.ToLower() == model.FormName.ToLower());
                    
                if (existingForm != null)
                {
                    return Json(new { success = false, message = "ชื่อแบบประเมินนี้มีอยู่แล้วในระบบ กรุณาใช้ชื่ออื่น" });
                }

                if (ModelState.IsValid)
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();
                    
                    try
                    {
                        var form = new Form
                        {
                            FormName = model.FormName,
                            FormType = model.FormType.ToString(),
                            Questions = new List<Question>()
                        };

                        if (model.Questions != null)
                        {
                            foreach (var questionModel in model.Questions)
                            {
                                var question = new Question
                                {
                                    QuestionText = questionModel.QuestionText,
                                    Options = new List<Option>()
                                };

                                form.Questions.Add(question);

                                foreach (var optionModel in questionModel.Options)
                                {
                                    var option = new Option
                                    {
                                        OptionText = optionModel.OptionText,
                                        Value = optionModel.Value,
                                    };
                                    question.Options.Add(option);
                                }
                            }
                        }

                        _db.Form.Add(form);

                        await _db.SaveChangesAsync();

                        if (model.ResultSettings != null)
                        {
                            foreach (var resultSettingModel in model.ResultSettings)
                            {
                                var resultName = string.IsNullOrWhiteSpace(resultSettingModel.ResultName) 
                                    ? $"ระดับ {resultSettingModel.MinScore}-{resultSettingModel.MaxScore}"
                                    : resultSettingModel.ResultName;

                                var resultSetting = new ResultSetting
                                {
                                    FormID = form.FormID,
                                    MinScore = resultSettingModel.MinScore,
                                    MaxScore = resultSettingModel.MaxScore,
                                    ResultText = resultSettingModel.ResultText,
                                    ResultName = resultName
                                };
                                _db.ResultSettings.Add(resultSetting);
                            }
                        }

                        await _db.SaveChangesAsync();
                        
                        await transaction.CommitAsync();

                        return Json(new { success = true, message = "บันทึกแบบประเมินเรียบร้อย" });
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction เมื่อเกิดข้อผิดพลาด
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Transaction Error: {ex.Message}");
                        return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" });
                    }
                }

                return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
        

        public class CheckFormNameRequest
        {
            public string FormName { get; set; } = "";
            public int? ExcludeFormId { get; set; }
        }

        [HttpPost]
        public async Task<JsonResult> CheckFormNameExists([FromBody] CheckFormNameRequest request)
        {
            var exists = await _db.Form
                .AnyAsync(f => f.FormName.ToLower() == request.FormName.ToLower() && 
                              (request.ExcludeFormId == null || f.FormID != request.ExcludeFormId));

            return Json(new { exists = exists });
        }
    }
}
