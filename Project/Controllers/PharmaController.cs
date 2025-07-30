using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models;
using Project.Models.Entity;
using Project.Data;
using System.Text.Json.Serialization;


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

        public IActionResult EditForm()
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
            try
            {
                var form = await _db.Form
                    .Include(f => f.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(f => f.Interpreters)
                    .FirstOrDefaultAsync(f => f.FormID == id);

                if (form == null)
                {
                    return Json(new { success = false, message = "ไม่พบแบบประเมิน" });
                }

                var formData = new
                {
                    formID = form.FormID,
                    formName = form.FormName,
                    formType = form.FormType,
                    scoring = form.Scoring,
                    questions = form.Questions.Select(q => new
                    {
                        questionID = q.QuestionID,
                        questionText = q.QuestionText,
                        section = q.Section,
                        options = q.Options.Select(o => new
                        {
                            optionID = o.OptionID,
                            optionText = o.OptionText,
                            value = o.Value
                        }).ToList()
                    }).ToList(),
                    interpreters = form.Interpreters.Select(i => new
                    {
                        interID = i.InterID,
                        minScore = i.MinScore,
                        maxScore = i.MaxScore,
                        interName = i.InterName,
                        interText = i.InterText
                    }).ToList()
                };

                return Json(new { success = true, data = formData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
            }
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

        public async Task<IActionResult> CreateForm(int? editId)
        {
            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.Forms = await _db.Form.ToListAsync();
            
            if (editId.HasValue)
            {
                var form = await _db.Form
                    .Include(f => f.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(f => f.Interpreters)
                    .FirstOrDefaultAsync(f => f.FormID == editId.Value);
                    
                if (form != null)
                {
                    ViewBag.EditForm = form;
                    ViewBag.IsEdit = true;
                }
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateForm([FromForm] string formName, [FromForm] int formType, [FromForm] string? scoring, [FromForm] string questionsJson, [FromForm] string interpretersJson)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"Form Name: {formName}");
                Console.WriteLine($"Form Type: {formType}");
                Console.WriteLine($"Scoring Method: {scoring}");
                Console.WriteLine($"Questions JSON: {questionsJson}");
                Console.WriteLine($"Interpreters JSON: {interpretersJson}");

                var existingForm = await _db.Form
                    .FirstOrDefaultAsync(f => f.FormName!.ToLower() == formName.ToLower());
                    
                if (existingForm != null)
                {
                    return Json(new { success = false, message = "ชื่อแบบประเมินนี้มีอยู่แล้วในระบบ กรุณาใช้ชื่ออื่น" });
                }

                if (!string.IsNullOrEmpty(formName))
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();
                    
                    try
                    {
                        var form = new Form
                        {
                            FormName = formName,
                            FormType = formType,
                            Scoring = scoring,
                            Questions = new List<Question>()
                        };

                        // Parse questions JSON if provided
                        if (!string.IsNullOrEmpty(questionsJson))
                        {
                            var questionsData = System.Text.Json.JsonSerializer.Deserialize<List<QuestionData>>(questionsJson);
                            Console.WriteLine($"Parsed {questionsData?.Count} questions");
                            
                            if (questionsData != null)
                            {
                                foreach (var questionData in questionsData)
                                {
                                    Console.WriteLine($"Question: {questionData.QuestionText}");
                                    var question = new Question
                                    {
                                        QuestionText = questionData.QuestionText,
                                        Section = questionData.Section,
                                        Options = new List<Option>()
                                    };

                                    form.Questions.Add(question);

                                    if (questionData.Options != null)
                                    {
                                        Console.WriteLine($"Adding {questionData.Options.Count} options");
                                        foreach (var optionData in questionData.Options)
                                        {
                                            Console.WriteLine($"Option: {optionData.OptionText}, Value: {optionData.Value}");
                                            var option = new Option
                                            {
                                                OptionText = optionData.OptionText,
                                                Value = optionData.Value,
                                            };
                                            question.Options.Add(option);
                                        }
                                    }
                                }
                            }
                        }

                        _db.Form.Add(form);
                        await _db.SaveChangesAsync();

                        // Parse interpreters JSON if provided
                        if (!string.IsNullOrEmpty(interpretersJson))
                        {
                            var interpretersData = System.Text.Json.JsonSerializer.Deserialize<List<InterpreterData>>(interpretersJson);
                            Console.WriteLine($"Parsed {interpretersData?.Count} interpreters");
                            
                            if (interpretersData != null)
                            {
                                foreach (var interpreterData in interpretersData)
                                {
                                    Console.WriteLine($"Interpreter: Name={interpreterData.InterName}, Text={interpreterData.InterText}, Min={interpreterData.MinScore}, Max={interpreterData.MaxScore}");
                                    
                                    var interName = string.IsNullOrWhiteSpace(interpreterData.InterName) 
                                        ? $"ระดับ {interpreterData.MinScore}-{interpreterData.MaxScore}"
                                        : interpreterData.InterName;

                                    var interpreter = new Interpreter
                                    {
                                        FormID = form.FormID,
                                        MinScore = interpreterData.MinScore,
                                        MaxScore = interpreterData.MaxScore,
                                        InterText = interpreterData.InterText,
                                        InterName = interName
                                    };
                                    _db.Interpreter.Add(interpreter);
                                }
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
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                        return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกข้อมูล" });
                    }
                }

                return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateForm([FromForm] int formId, [FromForm] string formName, [FromForm] int formType, [FromForm] string? scoring, [FromForm] string? questionsJson, [FromForm] string? interpretersJson)
        {
            try
            {
                var form = await _db.Form
                    .Include(f => f.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(f => f.Interpreters)
                    .FirstOrDefaultAsync(f => f.FormID == formId);
                    
                if (form == null)
                {
                    return Json(new { success = false, message = "ไม่พบแบบประเมิน" });
                }

                // Check if name already exists (excluding current form)
                var existingForm = await _db.Form
                    .FirstOrDefaultAsync(f => f.FormName != null && 
                                            f.FormName.ToLower() == formName.ToLower() && 
                                            f.FormID != formId);
                    
                if (existingForm != null)
                {
                    return Json(new { success = false, message = "ชื่อแบบประเมินนี้มีอยู่แล้วในระบบ กรุณาใช้ชื่ออื่น" });
                }

                using var transaction = await _db.Database.BeginTransactionAsync();
                
                try
                {
                    // Update form basic info
                    form.FormName = formName;
                    form.FormType = formType;
                    form.Scoring = scoring;

                    // Delete existing questions and options
                    foreach (var question in form.Questions.ToList())
                    {
                        _db.Option.RemoveRange(question.Options);
                        _db.Question.Remove(question);
                    }

                    // Delete existing interpreters
                    _db.Interpreter.RemoveRange(form.Interpreters);

                    // Parse and add new questions
                    if (!string.IsNullOrEmpty(questionsJson))
                    {
                        var questions = System.Text.Json.JsonSerializer.Deserialize<List<QuestionData>>(questionsJson);
                        if (questions != null)
                        {
                            foreach (var questionData in questions)
                            {
                                var question = new Question
                                {
                                    FormID = form.FormID,
                                    QuestionText = questionData.QuestionText ?? "",
                                    Section = questionData.Section
                                };

                                _db.Question.Add(question);
                                await _db.SaveChangesAsync(); // Save to get QuestionID

                                // Add options
                                if (questionData.Options != null)
                                {
                                    foreach (var optionData in questionData.Options)
                                    {
                                        var option = new Option
                                        {
                                            QuestionID = question.QuestionID,
                                            OptionText = optionData.OptionText ?? "",
                                            Value = optionData.Value ?? 0
                                        };
                                        _db.Option.Add(option);
                                    }
                                }
                            }
                        }
                    }

                    // Parse and add new interpreters
                    if (!string.IsNullOrEmpty(interpretersJson))
                    {
                        var interpreters = System.Text.Json.JsonSerializer.Deserialize<List<InterpreterData>>(interpretersJson);
                        if (interpreters != null)
                        {
                            foreach (var interpreterData in interpreters)
                            {
                                var interpreter = new Interpreter
                                {
                                    FormID = form.FormID,
                                    MinScore = interpreterData.MinScore ?? 0,
                                    MaxScore = interpreterData.MaxScore ?? 0,
                                    InterName = interpreterData.InterName ?? "",
                                    InterText = interpreterData.InterText ?? ""
                                };
                                _db.Interpreter.Add(interpreter);
                            }
                        }
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "อัพเดทแบบประเมินเรียบร้อย" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "เกิดข้อผิดพลาดในการอัพเดท: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการอัพเดท: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteForm(int id)
        {
            try
            {
                var form = await _db.Form
                    .Include(f => f.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(f => f.Interpreters)
                    .Include(f => f.Evaluations)
                    .FirstOrDefaultAsync(f => f.FormID == id);
                    
                if (form == null)
                {
                    return Json(new { success = false, message = "ไม่พบแบบประเมิน" });
                }

                // Check if form is being used in evaluations
                if (form.Evaluations.Any())
                {
                    return Json(new { success = false, message = "ไม่สามารถลบแบบประเมินนี้ได้ เนื่องจากมีการใช้งานอยู่" });
                }

                using var transaction = await _db.Database.BeginTransactionAsync();
                
                try
                {
                    // Delete options first
                    foreach (var question in form.Questions)
                    {
                        _db.Option.RemoveRange(question.Options);
                    }

                    // Delete questions
                    _db.Question.RemoveRange(form.Questions);

                    // Delete interpreters
                    _db.Interpreter.RemoveRange(form.Interpreters);

                    // Delete form
                    _db.Form.Remove(form);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "ลบแบบประเมินเรียบร้อย" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "เกิดข้อผิดพลาดในการลบ: " + ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
            }
        }

        public class QuestionData
        {
            [JsonPropertyName("questionText")]
            public string? QuestionText { get; set; }
            
            [JsonPropertyName("section")]
            public string? Section { get; set; }
            
            [JsonPropertyName("options")]
            public List<OptionData>? Options { get; set; }
        }

        public class OptionData
        {
            [JsonPropertyName("optionText")]
            public string? OptionText { get; set; }
            
            [JsonPropertyName("value")]
            public int? Value { get; set; }
        }

        public class InterpreterData
        {
            [JsonPropertyName("interName")]
            public string? InterName { get; set; }
            
            [JsonPropertyName("interText")]
            public string? InterText { get; set; }
            
            [JsonPropertyName("minScore")]
            public int? MinScore { get; set; }
            
            [JsonPropertyName("maxScore")]
            public int? MaxScore { get; set; }
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
                .AnyAsync(f => f.FormName != null && f.FormName.ToLower() == request.FormName.ToLower() && 
                              (request.ExcludeFormId == null || f.FormID != request.ExcludeFormId));

            return Json(new { exists = exists });
        }
    }
}
