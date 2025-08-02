using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models;
using Project.Models.Entity;
using Project.Data;
using Project.Services;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;

namespace Project.Controllers
{
    // Model for patient creation/update
    public class PatientCreateModel
    {
        public string HN { get; set; } = string.Empty;
        public string? CitizenID { get; set; }
        public int? TitleID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public int? RightID { get; set; }
        public string? Phone { get; set; }
        public string? PatientType { get; set; }
        public int? TambonID { get; set; }
    }
        [Authorize(Roles = "เภสัช")]
        public class PharmaController : Controller
        {
            private readonly ApplicationDbContext _db;

            public PharmaController(ApplicationDbContext db)
            {
                _db = db;
            }

            private async Task SetUserProfileData() {
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
            public async Task<IActionResult> Forms()
            {
                await SetUserProfileData();
                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                return View();
            }

            public async Task<IActionResult> EditForm()
            {
                await SetUserProfileData();
                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                return View();
            }

            public async Task<IActionResult> Treatments()
            {
                await SetUserProfileData();
                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                return View();
            }

            public async Task<IActionResult> Profile()
            {
                await SetUserProfileData();
                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                return View();
            }

            public async Task<IActionResult> Evaluation()
            {
                await SetUserProfileData();
                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                var forms = await _db.Form.OrderBy(f => f.FormName).ToListAsync();
                ViewBag.Forms = forms;

                return View();
            }

            [HttpGet]
            public async Task<IActionResult> SearchPatients(string term)
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
                {
                    return Json(new List<object>());
                }

                var patients = await _db.Patient
                    .Include(p => p.Title)
                    .Where(p => p.HN.Contains(term) || 
                               (p.FirstName != null && p.FirstName.Contains(term)) ||
                               (p.LastName != null && p.LastName.Contains(term)) ||
                               (p.Title != null && p.Title.TitleName != null && p.Title.TitleName.Contains(term)))
                    .Select(p => new
                    {
                        id = p.HN,
                        text = $"{p.HN} - {p.Title!.TitleName}{p.FirstName} {p.LastName}",
                        patientType = p.PatientType ?? "ไม่ระบุ",
                        birthDate = p.BirthDate != null ? p.BirthDate.Value.ToString("dd/MM/yyyy") : "ไม่ระบุ"
                    })
                    .Take(10)
                    .ToListAsync();

                return Json(patients);
            }

            [HttpGet]
            public async Task<IActionResult> StartEvaluation(string patientId, int formId)
            {
                var patient = await _db.Patient
                    .Include(p => p.Title)
                    .FirstOrDefaultAsync(p => p.HN == patientId);

                var form = await _db.Form
                    .Include(f => f.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(f => f.Interpreters)
                    .FirstOrDefaultAsync(f => f.FormID == formId);

                if (patient == null || form == null)
                {
                    return RedirectToAction("Evaluation");
                }

                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
                ViewBag.Patient = patient;
                ViewBag.Form = form;

                return View("EvaluationForm");
            }

            [HttpPost]
            public async Task<IActionResult> SubmitEvaluation([FromBody] EvaluationSubmissionModel model)
            {
                try
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                    }

                    // Create evaluation record
                    var evaluation = new Evaluation
                    {
                        HN = model.PatientId,
                        FormID = model.FormId,
                        UserID = int.Parse(userId),
                        RecordDate = DateTime.Now,
                        Result = model.Result
                    };

                    _db.Evaluation.Add(evaluation);
                    await _db.SaveChangesAsync();

                    // Create evaluation detail records
                    foreach (var answer in model.Answers)
                    {
                        var evaluationDetail = new EvaluationDetail
                        {
                            EvaID = evaluation.EvaID,
                            QuestionID = answer.QuestionId,
                            Score = answer.Score
                        };

                        _db.EvaluationDetail.Add(evaluationDetail);
                    }

                    await _db.SaveChangesAsync();

                    return Json(new { success = true, message = "บันทึกผลการประเมินเรียบร้อยแล้ว" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            [HttpGet]
            public async Task<IActionResult> EvaluationDetail(int evaId)
            {
                var evaluation = await _db.Evaluation
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
                    .FirstOrDefaultAsync(e => e.EvaID == evaId);

                if (evaluation == null)
                {
                    return RedirectToAction("Index");
                }

                // คำนวณคะแนนตามเงื่อนไข max, min, sum
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

                        // ค้นหา Interpreter ที่ตรงกับคะแนนที่คำนวณได้
                        var interpreter = await _db.Interpreter
                            .Where(i => i.FormID == evaluation.FormID 
                                    && calculatedScore >= i.MinScore 
                                    && calculatedScore <= i.MaxScore)
                            .FirstOrDefaultAsync();

                        // เก็บข้อมูล Interpreter ใน ViewBag เพื่อใช้ใน View
                        ViewBag.InterpreterName = interpreter?.InterName;
                        ViewBag.InterpreterText = interpreter?.InterText;
                        ViewBag.CalculatedScore = calculatedScore;
                    }
                }

                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                return View(evaluation);
            }

            public async Task<IActionResult> Patients()
            {
                await SetUserProfileData();
                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                var patients = await _db.Patient
                    .Include(p => p.Title)
                    .Include(p => p.Treatment)
                    .Include(p => p.District)
                        .ThenInclude(d => d!.Amphur)
                        .ThenInclude(a => a!.Province)
                    .OrderBy(p => p.HN)
                    .ToListAsync();

                return View(patients);
            }

            // API Endpoints for Patient Management
            [HttpGet]
            public async Task<IActionResult> GetNextHN()
            {
                var lastPatient = await _db.Patient
                    .OrderByDescending(p => p.HN)
                    .FirstOrDefaultAsync();

                string nextHN;
                if (lastPatient == null)
                {
                    nextHN = "HN00001";
                }
                else
                {
                    // Extract number from HN (remove "HN" prefix)
                    var lastNumber = lastPatient.HN.Substring(2);
                    if (int.TryParse(lastNumber, out int number))
                    {
                        number++;
                        nextHN = $"HN{number:D5}";
                    }
                    else
                    {
                        nextHN = "HN00001";
                    }
                }

                return Json(new { nextHN });
            }

            [HttpGet]
            public async Task<IActionResult> GetTitles()
            {
                var titles = await _db.Title.ToListAsync();
                return Json(titles);
            }

            [HttpGet]
            public async Task<IActionResult> GetRights()
            {
                var treatments = await _db.Treatment.ToListAsync();
                return Json(treatments.Select(t => new
                {
                    rightID = t.TreatmentID,
                    rightName = t.TreatmentName
                }));
            }

            [HttpGet]
            public async Task<IActionResult> GetProvinces()
            {
                var provinces = await _db.Province
                    .Where(p => p.PROVINCE_ID > 0)  // ไม่เอา ID = 0
                    .OrderBy(p => p.PROVINCE_NAME)
                    .ToListAsync();
                return Json(provinces.Select(p => new
                {
                    provinceID = p.PROVINCE_ID,
                    provinceName = p.PROVINCE_NAME
                }));
            }

            [HttpGet]
            public async Task<IActionResult> GetAmphoes(int id)
            {
                var amphoes = await _db.Amphur
                    .Where(a => a.PROVINCE_ID == id && a.AMPHUR_ID > 0)  // ไม่เอา ID = 0
                    .OrderBy(a => a.AMPHUR_NAME)
                    .ToListAsync();

                return Json(amphoes.Select(a => new
                {
                    amphID = a.AMPHUR_ID,
                    amphName = a.AMPHUR_NAME
                }));
            }

            [HttpGet]
            public async Task<IActionResult> GetTambons(int id)
            {
                var districts = await _db.District
                    .Where(d => d.AMPHUR_ID == id && d.DISTRICT_ID > 0)  // ไม่เอา ID = 0
                    .OrderBy(d => d.DISTRICT_NAME)
                    .ToListAsync();

                return Json(districts.Select(d => new
                {
                    tambonID = d.DISTRICT_ID,
                    tambonName = d.DISTRICT_NAME
                }));
            }

            [HttpGet]
            public async Task<IActionResult> GetPatient(string id)
            {
                var patient = await _db.Patient
                    .Include(p => p.Title)
                    .Include(p => p.Treatment)
                    .Include(p => p.District)
                        .ThenInclude(d => d!.Amphur)
                        .ThenInclude(a => a!.Province)
                    .FirstOrDefaultAsync(p => p.HN == id);

                if (patient == null)
                {
                    return NotFound();
                }

                var result = new
                {
                    hn = patient.HN,
                    citizenID = patient.CitizenID,
                    titleID = patient.TitleID,
                    firstName = patient.FirstName,
                    lastName = patient.LastName,
                    birthDate = patient.BirthDate?.ToString("yyyy-MM-dd"),
                    weight = patient.Weight,
                    height = patient.Height,
                    rightID = patient.TreatmentID,
                    phone = patient.Tel,
                    patientType = patient.PatientType,
                    provinceID = patient.District?.Amphur?.PROVINCE_ID,
                    amphoeID = patient.District?.AMPHUR_ID,
                    tambonID = patient.DISTRICT_ID
                };

                return Json(result);
            }

            [HttpPost]
            public async Task<IActionResult> CreatePatient([FromBody] PatientCreateModel model)
            {
                try
                {
                    // Check if HN already exists
                    var existingPatient = await _db.Patient.FindAsync(model.HN);
                    if (existingPatient != null)
                    {
                        return Json(new { success = false, message = "หมายเลข HN นี้มีอยู่แล้วในระบบ" });
                    }

                    var patient = new Patient
                    {
                        HN = model.HN,
                        CitizenID = model.CitizenID,
                        TitleID = model.TitleID,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        BirthDate = model.BirthDate,
                        Weight = model.Weight,
                        Height = model.Height,
                        TreatmentID = model.RightID,
                        Tel = model.Phone,
                        PatientType = model.PatientType,
                        DISTRICT_ID = model.TambonID
                    };

                    _db.Patient.Add(patient);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true, message = "เพิ่มผู้ป่วยใหม่เรียบร้อยแล้ว" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            [HttpPost]
            public async Task<IActionResult> UpdatePatient([FromBody] PatientCreateModel model)
            {
                try
                {
                    var patient = await _db.Patient.FindAsync(model.HN);
                    if (patient == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลผู้ป่วย" });
                    }

                    patient.CitizenID = model.CitizenID;
                    patient.TitleID = model.TitleID;
                    patient.FirstName = model.FirstName;
                    patient.LastName = model.LastName;
                    patient.BirthDate = model.BirthDate;
                    patient.Weight = model.Weight;
                    patient.Height = model.Height;
                    patient.TreatmentID = model.RightID;
                    patient.Tel = model.Phone;
                    patient.PatientType = model.PatientType;
                    patient.DISTRICT_ID = model.TambonID;

                    _db.Patient.Update(patient);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true, message = "แก้ไขข้อมูลผู้ป่วยเรียบร้อยแล้ว" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            // Form Management API Actions
            [HttpGet]
            public async Task<IActionResult> GetForms(string search = "", int? formType = null, string status = "")
            {
                var query = _db.Form.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f => f.FormName != null && f.FormName.Contains(search));
                }

                // Apply form type filter
                if (formType.HasValue)
                {
                    query = query.Where(f => f.FormType == formType.Value);
                }

                var forms = await query.Select(f => new
                {
                    formID = f.FormID,
                    formName = f.FormName,
                    formType = f.FormType,
                    questionCount = f.Questions.Count(),
                    evaluationCount = f.Evaluations.Count()
                }).OrderBy(f => f.formID).ToListAsync();

                // Apply status filter (after data retrieval)
                if (!string.IsNullOrEmpty(status))
                {
                    switch (status)
                    {
                        case "withQuestions":
                            forms = forms.Where(f => f.questionCount > 0).ToList();
                            break;
                        case "noQuestions":
                            forms = forms.Where(f => f.questionCount == 0).ToList();
                            break;
                        case "hasEvaluations":
                            forms = forms.Where(f => f.evaluationCount > 0).ToList();
                            break;
                        case "noEvaluations":
                            forms = forms.Where(f => f.evaluationCount == 0).ToList();
                            break;
                    }
                }

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

            // Treatment CRUD methods
            [HttpGet]
            public async Task<IActionResult> GetTreatments(string search = "")
            {
                var query = _db.Treatment.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(t => t.TreatmentName != null && t.TreatmentName.Contains(search));
                }

                var treatments = await query.Select(t => new
                {
                    rightID = t.TreatmentID,
                    rightName = t.TreatmentName
                }).OrderBy(t => t.rightID).ToListAsync();

                return Json(treatments);
            }

            [HttpPost]
            public async Task<IActionResult> CreateTreatment([FromBody] TreatmentCreateModel model)
            {
                try
                {
                    if (string.IsNullOrEmpty(model.RightName))
                    {
                        return Json(new { success = false, message = "กรุณากรอกชื่อสิทธิ์การรักษา" });
                    }

                    // Check if treatment name already exists
                    var existingTreatment = await _db.Treatment
                        .FirstOrDefaultAsync(t => t.TreatmentName == model.RightName);

                    if (existingTreatment != null)
                    {
                        return Json(new { success = false, message = "ชื่อสิทธิ์การรักษานี้มีอยู่แล้ว" });
                    }

                    var treatment = new Treatment
                    {
                        TreatmentName = model.RightName
                    };

                    _db.Treatment.Add(treatment);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true, message = "เพิ่มสิทธิ์การรักษาสำเร็จ" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            [HttpPost]
            public async Task<IActionResult> UpdateTreatment([FromBody] TreatmentCreateModel model)
            {
                try
                {
                    if (string.IsNullOrEmpty(model.RightName))
                    {
                        return Json(new { success = false, message = "กรุณากรอกชื่อสิทธิ์การรักษา" });
                    }

                    var treatment = await _db.Treatment.FindAsync(model.RightID);
                    if (treatment == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลสิทธิ์การรักษา" });
                    }

                    // Check if treatment name already exists (excluding current treatment)
                    var existingTreatment = await _db.Treatment
                        .FirstOrDefaultAsync(t => t.TreatmentName == model.RightName && t.TreatmentID != model.RightID);

                    if (existingTreatment != null)
                    {
                        return Json(new { success = false, message = "ชื่อสิทธิ์การรักษานี้มีอยู่แล้ว" });
                    }

                    treatment.TreatmentName = model.RightName;
                    await _db.SaveChangesAsync();

                    return Json(new { success = true, message = "แก้ไขสิทธิ์การรักษาสำเร็จ" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            [HttpDelete]
            public async Task<IActionResult> DeleteTreatment(int id)
            {
                try
                {
                    var treatment = await _db.Treatment.FindAsync(id);
                    if (treatment == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลสิทธิ์การรักษา" });
                    }

                    // Check if treatment is being used by any patient
                    var patientsUsingTreatment = await _db.Patient
                        .Where(p => p.TreatmentID == id)
                        .CountAsync();

                    if (patientsUsingTreatment > 0)
                    {
                        return Json(new { success = false, message = $"ไม่สามารถลบได้ เนื่องจากมีผู้ป่วย {patientsUsingTreatment} คน ที่ใช้สิทธิ์การรักษานี้" });
                    }

                    _db.Treatment.Remove(treatment);
                    await _db.SaveChangesAsync();

                    return Json(new { success = true, message = "ลบสิทธิ์การรักษาสำเร็จ" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            public class TreatmentCreateModel
            {
                public int RightID { get; set; }
                public string RightName { get; set; } = string.Empty;
            }

            // User Profile methods
            [HttpGet]
            public async Task<IActionResult> GetUserProfile()
            {
                try
                {
                    // Use ClaimTypes.NameIdentifier instead of "UserID"
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้ในระบบ" });
                    }

                    var user = await _db.User
                        .Include(u => u.Title)
                        .FirstOrDefaultAsync(u => u.UserID == int.Parse(userId));

                    if (user == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้ในฐานข้อมูล" });
                    }

                    return Json(new
                    {
                        success = true,
                        username = user.Username,
                        titleID = user.TitleID,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        profilePictureUrl = string.IsNullOrEmpty(user.Picture) 
                            ? null 
                            : $"/uploads/profile/{user.Picture}"
            });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            [HttpPost]
            public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
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
                    return Json(new { 
                        success = true, 
                        message = "บันทึกรูปภาพเรียบร้อยแล้ว",
                        pictureUrl = pictureUrl
                    });
                }
                catch (Exception)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกรูปภาพ" });
                }
            }

            [HttpPost]
            public async Task<IActionResult> RemoveProfilePicture()
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

            [HttpPost]
            public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdateModel model)
            {
                try
                {
                    // Use ClaimTypes.NameIdentifier instead of "UserID"
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

                    // Validate required fields
                    if (string.IsNullOrEmpty(model.FirstName) || string.IsNullOrEmpty(model.LastName))
                    {
                        return Json(new { success = false, message = "กรุณากรอกชื่อและนามสกุล" });
                    }

                    // Handle password change if provided
                    if (!string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        if (string.IsNullOrEmpty(model.NewPassword))
                        {
                            return Json(new { success = false, message = "กรุณากรอกรหัสผ่านใหม่" });
                        }

                        // Create password service instance for verification
                        var passwordService = new PasswordService();
                        
                        // Verify current password
                        if (!passwordService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                        {
                            return Json(new { success = false, message = "รหัสผ่านปัจจุบันไม่ถูกต้อง" });
                        }

                        if (model.NewPassword.Length < 6)
                        {
                            return Json(new { success = false, message = "รหัสผ่านใหม่ต้องมีอย่างน้อย 6 ตัวอักษร" });
                        }

                        // Hash new password
                        user.PasswordHash = passwordService.HashPassword(model.NewPassword);
                    }

                    // Update profile information
                    user.TitleID = model.TitleID;
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;

                    await _db.SaveChangesAsync();

                    // Update claims if profile info changed (not password)
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        // Reload user with title for updated claims
                        var updatedUser = await _db.User
                            .Include(u => u.Title)
                            .Include(u => u.Role)
                            .FirstOrDefaultAsync(u => u.UserID == user.UserID);

                        if (updatedUser != null)
                        {
                            // Create new claims with updated information
                            var claims = new List<System.Security.Claims.Claim>
                            {
                                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, updatedUser.UserID.ToString()),
                                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, updatedUser.Username),
                                new System.Security.Claims.Claim("FirstName", updatedUser.FirstName ?? ""),
                                new System.Security.Claims.Claim("LastName", updatedUser.LastName ?? ""),
                                new System.Security.Claims.Claim("RoleID", updatedUser.RoleID?.ToString() ?? ""),
                                new System.Security.Claims.Claim("RoleName", updatedUser.Role?.RoleName ?? ""),
                                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, updatedUser.Role?.RoleName ?? ""),
                                new System.Security.Claims.Claim("TitleID", updatedUser.TitleID?.ToString() ?? ""),
                                new System.Security.Claims.Claim("TitleName", updatedUser.Title?.TitleName ?? "")
                            };

                            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                            
                            var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                            };

                            await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                                new System.Security.Claims.ClaimsPrincipal(claimsIdentity), authProperties);
                        }
                    }

                    return Json(new { success = true, message = "บันทึกข้อมูลสำเร็จ" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
                }
            }

            public class UserProfileUpdateModel
            {
                public int TitleID { get; set; }
                public string FirstName { get; set; } = string.Empty;
                public string LastName { get; set; } = string.Empty;
                public string CurrentPassword { get; set; } = string.Empty;
                public string NewPassword { get; set; } = string.Empty;
            }

            public class EvaluationSubmissionModel
            {
                public string PatientId { get; set; } = string.Empty;
                public int FormId { get; set; }
                public string Result { get; set; } = string.Empty;
                public List<EvaluationAnswerModel> Answers { get; set; } = new List<EvaluationAnswerModel>();
            }

            public class EvaluationAnswerModel
            {
                public int QuestionId { get; set; }
                public int Score { get; set; }
            }
        }
    }
