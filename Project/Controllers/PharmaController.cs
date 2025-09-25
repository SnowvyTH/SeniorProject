using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.IO.Font;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using Project.Models.Entity;
using Project.Services;
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

            private async Task SetUserProfileData() {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _db.User.FindAsync(int.Parse(userId));
                    if (user != null && !string.IsNullOrEmpty(user.Picture))
                    {
                        ViewBag.ProfileImageUrl = "/uploads/profile/" + user.Picture + "?t=" + DateTime.Now.Ticks;
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
                await SetUserProfileData();
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

                    var evaluation = new Evaluation
                    {
                        HN = model.PatientId,
                        FormID = model.FormId,
                        UserID = int.Parse(userId),
                        RecordDate = DateTime.Now,
                        Result = model.Result,
                        ResultDetail = model.ResultDetail
                    };

                    _db.Evaluation.Add(evaluation);
                    await _db.SaveChangesAsync();

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
                await SetUserProfileData();
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

                if (evaluation!.EvaluationDetails?.Any() == true && evaluation.Form != null)
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

                ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
                ViewBag.RoleName = User.FindFirst("RoleName")?.Value;

                return View(evaluation);
            }

        [HttpGet]
        public async Task<IActionResult> ExportEvaluationPDF(int evaId)
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

            string thaifontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "font", "THSarabunNew.ttf");
            var thaifontProgram = FontProgramFactory.CreateFont(thaifontPath);
            var thaiFont = PdfFontFactory.CreateFont(thaifontProgram, PdfEncodings.IDENTITY_H);

            string thaiboldfontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "font", "THSarabunNewBold.ttf");
            var thaiboldfontProgram = FontProgramFactory.CreateFont(thaiboldfontPath);
            var thaiBoldFont = PdfFontFactory.CreateFont(thaiboldfontProgram, PdfEncodings.IDENTITY_H);

            string emojifontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "font", "NotoEmoji.ttf");
            var emojifontProgram = FontProgramFactory.CreateFont(emojifontPath);
            var emojiFont = PdfFontFactory.CreateFont(emojifontProgram, PdfEncodings.IDENTITY_H);

            if (evaluation!.Form!.FormType == 1) {

                string modsasBackgroundPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdf", "modsas.pdf");

                MemoryStream stream = new MemoryStream();
                PdfWriter writer = new PdfWriter(stream);
                PdfDocument pdf = new PdfDocument(writer);

                PdfReader reader = new PdfReader(modsasBackgroundPath);
                PdfDocument backgroundPdf = new PdfDocument(reader);

                PdfFormXObject backgroundPage = backgroundPdf.GetPage(1).CopyAsFormXObject(pdf);

                PageSize pageSize = new PageSize(backgroundPdf.GetPage(1).GetPageSize());
                PdfPage page = pdf.AddNewPage(pageSize);

                PdfCanvas canvas = new PdfCanvas(page);

                canvas.AddXObject(backgroundPage);

                Document document = new Document(pdf);

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{(evaluation.Patient!.HN).Substring(2)}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 101, 759, TextAlignment.LEFT);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{evaluation.Patient.Title?.TitleName}{evaluation.Patient.FirstName} {evaluation.Patient.LastName}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 245, 759, TextAlignment.LEFT);

                if (evaluation.Patient.PatientType == "OPD")
                {
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("✖︎").SetFontSize(8).SetFont(emojiFont), 382, 763, TextAlignment.LEFT);
                }
                else
                {
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("✖︎").SetFontSize(8).SetFont(emojiFont), 416, 763, TextAlignment.LEFT);
                }

                var scores = evaluation.Form.Questions
                            .OrderBy(q => q.QuestionID)
                            .Select(q => evaluation.EvaluationDetails
                                .FirstOrDefault(ed => ed.QuestionID == q.QuestionID)?.Score ?? 0)
                            .ToList();

                var x = 219;
                var y = 714;

                var xoffset = 71;
                var yoffset = -57;
                var question = 0;
                var addOffset = 0;

                foreach (var score in scores)
                {
                    if (question > 1)
                    {
                        addOffset = -24;
                    }
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("✖︎").SetFontSize(8).SetFont(emojiFont), x + (xoffset * score), (y + (yoffset * question)) + addOffset, TextAlignment.LEFT);
                    question++;
                }

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{evaluation.User?.FirstName} {evaluation.User?.LastName}    {evaluation.RecordDate:dd   MM  yyyy}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 378, 316, TextAlignment.LEFT);


                document.Close();
                backgroundPdf.Close();
                reader.Close();

                var fileName = $"Evaluation_{evaluation.Patient.HN}_{evaluation.RecordDate:yyyyMMdd_HHmm}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            } else if (evaluation.Form.FormType == 2) {
                string modbarBackgroundPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdf", "modbar.pdf");
                MemoryStream stream = new MemoryStream();
                PdfWriter writer = new PdfWriter(stream);
                PdfDocument pdf = new PdfDocument(writer);

                PdfReader reader = new PdfReader(modbarBackgroundPath);
                PdfDocument backgroundPdf = new PdfDocument(reader);

                PdfFormXObject backgroundPage = backgroundPdf.GetPage(1).CopyAsFormXObject(pdf);

                PageSize pageSize = new PageSize(backgroundPdf.GetPage(1).GetPageSize());
                PdfPage page = pdf.AddNewPage(pageSize);

                PdfCanvas canvas = new PdfCanvas(page);

                canvas.AddXObject(backgroundPage);

                Document document = new Document(pdf);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph("HN ................").SetFontSize(14).SetFont(thaiBoldFont), 72, 754, TextAlignment.LEFT);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph("ชื่อผู้ป่วย-นามสกุล ...................................... ").SetFontSize(14).SetFont(thaiBoldFont), 128, 754, TextAlignment.LEFT);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph("🔲  🔲").SetFontSize(10).SetFont(emojiFont), 294, 759, TextAlignment.LEFT);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph("OPD      WARD ").SetFontSize(14).SetFont(thaiBoldFont), 308, 754, TextAlignment.LEFT);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph("ผู้ประเมิน/วันที่ประเมิน .....................................").SetFontSize(14).SetFont(thaiBoldFont), 376, 754, TextAlignment.LEFT);

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{(evaluation.Patient!.HN).Substring(2)}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 89, 757, TextAlignment.LEFT);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{evaluation.Patient.Title?.TitleName}{evaluation.Patient.FirstName} {evaluation.Patient.LastName}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 203, 757, TextAlignment.LEFT);

                if (evaluation.Patient.PatientType == "OPD")
                {
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("✖︎").SetFontSize(8).SetFont(emojiFont), 295, 760, TextAlignment.LEFT);
                }
                else
                {
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("✖︎").SetFontSize(8).SetFont(emojiFont), 333, 760, TextAlignment.LEFT);
                }

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{evaluation.User?.FirstName}  ({evaluation.RecordDate:dd/MM/yyyy})").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 471, 757, TextAlignment.LEFT);

                var scores = evaluation.Form.Questions
                        .OrderBy(q => q.QuestionID)
                        .Select(q => evaluation.EvaluationDetails
                            .FirstOrDefault(ed => ed.QuestionID == q.QuestionID)?.Score ?? 0)
                        .ToList();
                var positions = new Dictionary<(int question, double score), (float x, float y)>
                    {
                        {(1, 0), (49, 727)},
                        {(1, 1), (49, 712)},
                        {(1, 2), (49, 682)},
                        {(1, 3), (49, 668)},

                        {(2, 0), (49, 625)},
                        {(2, 1), (49, 610)},
                        {(2, 2), (49, 595)},
                        {(2, 3), (49, 565)},

                        {(3, 0), (49, 522)},
                        {(3, 1), (49, 507)},
                        {(3, 2), (311, 522)},
                        {(3, 3), (311, 507)},

                        {(4, 0), (49, 479)},
                        {(4, 1), (49, 449)},
                        {(4, 2), (49, 434)},
                        {(4, 3), (49, 390)},
                        {(4, 4), (49, 361)},
                        {(4, 5), (49, 332)},
                    };

                int question = 1;
                int total = 0;

                foreach (var score in scores)
                {
                    if (positions.TryGetValue((question, score), out var pos))
                    {
                        new Canvas(canvas, pageSize)
                            .ShowTextAligned(
                                new Paragraph("🌕").SetFontSize(12).SetFont(emojiFont).SetFontColor(ColorConstants.RED),
                                pos.x, pos.y, TextAlignment.LEFT);
                    }
                    total += score;
                    question++;
                }

                int q4Score = total > 5 ? 5 : total;
                if (positions.TryGetValue((4, q4Score), out var q4Pos))
                {
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("🌕").SetFontSize(12).SetFont(emojiFont).SetFontColor(ColorConstants.RED),
                            q4Pos.x, q4Pos.y, TextAlignment.LEFT);
                }

                document.Close();
                backgroundPdf.Close();
                reader.Close();

                var fileName = $"Evaluation_{evaluation.Patient.HN}_{evaluation.RecordDate:yyyyMMdd_HHmm}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            } else if (evaluation.Form.FormType == 3){
                string modaimsBackgroundPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdf", "modaims.pdf");
                MemoryStream stream = new MemoryStream();
                PdfWriter writer = new PdfWriter(stream);
                PdfDocument pdf = new PdfDocument(writer);

                PdfReader reader = new PdfReader(modaimsBackgroundPath);
                PdfDocument backgroundPdf = new PdfDocument(reader);

                PdfFormXObject backgroundPage = backgroundPdf.GetPage(1).CopyAsFormXObject(pdf);

                PageSize pageSize = new PageSize(backgroundPdf.GetPage(1).GetPageSize());
                PdfPage page = pdf.AddNewPage(pageSize);

                PdfCanvas canvas = new PdfCanvas(page);

                canvas.AddXObject(backgroundPage);

                Document document = new Document(pdf);

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{(evaluation.Patient!.HN).Substring(2)}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 100, 704, TextAlignment.LEFT);
                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{evaluation.Patient.Title?.TitleName}{evaluation.Patient.FirstName} {evaluation.Patient.LastName}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 250, 704, TextAlignment.LEFT);

                if (evaluation.Patient.PatientType != "OPD")
                {
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("✖︎").SetFontSize(8).SetFont(emojiFont), 398, 708, TextAlignment.LEFT);
                }
                else
                {
                    new Canvas(canvas, pageSize)
                        .ShowTextAligned(new Paragraph("✖︎").SetFontSize(8).SetFont(emojiFont), 445, 708, TextAlignment.LEFT);
                }

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{evaluation.User?.FirstName} {evaluation.User?.LastName}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 479, 649, TextAlignment.LEFT);

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"{evaluation.RecordDate:dd / MM / yyyy}").SetFontSize(14).SetFont(thaiFont).SetFontColor(ColorConstants.BLUE), 491, 613, TextAlignment.LEFT);

                var scores = evaluation.Form.Questions
                        .OrderBy(q => q.QuestionID)
                        .Select(q => evaluation.EvaluationDetails
                            .FirstOrDefault(ed => ed.QuestionID == q.QuestionID)?.Score ?? 0)
                        .ToList();

                int posX = 459;
                int posY = 593;

                int question = 0;
                int x_offset = 22;
                int y_offset = -20;
                int addOffset = 0;
                int totalScore = 0;

                foreach (var score in scores)
                {
                    if (question == 1 || question == 4 || question == 6) // 2
                    {
                        addOffset += y_offset;
                        new Canvas(canvas, pageSize)
                            .ShowTextAligned(
                                new Paragraph("🌕").SetFontSize(12).SetFont(emojiFont).SetFontColor(ColorConstants.RED),
                                posX + (score * x_offset), posY + ((question * y_offset) + addOffset), TextAlignment.LEFT);
                    }
                    else if (question == 5) // 3
                    {
                        addOffset += y_offset * 2;
                        new Canvas(canvas, pageSize)
                            .ShowTextAligned(
                                new Paragraph("🌕").SetFontSize(12).SetFont(emojiFont).SetFontColor(ColorConstants.RED),
                                posX + (score * x_offset), posY + ((question * y_offset) + addOffset), TextAlignment.LEFT);
                    }
                    else if (question < 10) // 1
                    {
                        new Canvas(canvas, pageSize)
                            .ShowTextAligned(
                                new Paragraph("🌕").SetFontSize(12).SetFont(emojiFont).SetFontColor(ColorConstants.RED),
                                posX + (score * x_offset), posY + ((question * y_offset) + addOffset), TextAlignment.LEFT);
                    }

                    if (question < 7)
                    {
                        totalScore += score;
                    }

                    if (question == 10 || question == 11)
                    {
                        int x = 0, y = 0;

                        if (question == 10)
                        {
                            y = 199;
                            x = (score == 1) ? 463 : 520;
                        }
                        else if (question == 11)
                        {
                            y = 178;
                            x = (score == 1) ? 463 : 520;
                        }

                        new Canvas(canvas, pageSize)
                            .ShowTextAligned(
                                new Paragraph("🌕")
                                    .SetFontSize(12)
                                    .SetFont(emojiFont)
                                    .SetFontColor(ColorConstants.RED),
                                x, y, TextAlignment.LEFT);
                    }
                    question++;
                }

                new Canvas(canvas, pageSize)
                    .ShowTextAligned(new Paragraph($"รวมได้ {totalScore} คะแนน").SetFontSize(15).SetFont(thaiBoldFont).SetFontColor(ColorConstants.BLUE), 471, 153, TextAlignment.LEFT);

                document.Close();
                backgroundPdf.Close();
                reader.Close();

                var fileName = $"Evaluation_{evaluation.Patient.HN}_{evaluation.RecordDate:yyyyMMdd_HHmm}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            } else {
                    try {
                        using var stream = new MemoryStream();
                        var writer = new PdfWriter(stream);
                        var pdf = new PdfDocument(writer);
                        var document = new Document(pdf, PageSize.A4);

                        // Header
                        var headerParagraph = new Paragraph("แบบประเมินการเคลื่อนไหวผิดปกติจากการรักษายาโรคจิต")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(12)
                            .SetMargin(0)
                            .SetFont(thaiBoldFont);
                        document.Add(headerParagraph);

                        var subHeaderParagraph = new Paragraph("Modified Simpson-Angus Rating Scale (Mod-SAS) สำหรับ Parkinsonism และ EPS อื่นๆ")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(12)
                            .SetMargin(0)
                            .SetFont(thaiBoldFont);
                        document.Add(subHeaderParagraph);

                        // Patient info section
                        var patientInfo = new Paragraph()
                            .SetTextAlignment(TextAlignment.CENTER)
                            .Add($"HN {evaluation.Patient!.HN}")
                            .Add("   ") // Spacing
                            .Add($"ชื่อผู้ป่วย-นามสกุล {evaluation.Patient.Title?.TitleName}{evaluation.Patient.FirstName} {evaluation.Patient.LastName}")
                            .Add("   ") // Spacing
                            .Add("☐ OPD ☐ WARD")
                            .SetFontSize(10)
                            .SetMargin(0)
                            .SetFont(thaiBoldFont);
                        document.Add(patientInfo);

                        // Create main table
                        float[] columnWidths = { 6f, 2f, 2f, 2f, 2f, 2f };
                        var table = new Table(columnWidths);
                        table.SetWidth(UnitValue.CreatePercentValue(100));

                        // First header row - "คะแนน" merged across score columns
                        var testHeaderCell = new Cell(2, 1).Add(new Paragraph("การทดสอบ")
                            .SetFontSize(11) // เพิ่มขนาดฟอนต์
                            .SetMargin(0)
                            .SetMultipliedLeading(0.8f))
                            .SetFont(thaiBoldFont)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(ColorConstants.WHITE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(3);
                        table.AddHeaderCell(testHeaderCell);

                        var scoreHeaderCell = new Cell(1, 5).Add(new Paragraph("คะแนน")
                            .SetFontSize(11)
                            .SetMargin(0)
                            .SetMultipliedLeading(0.8f))
                            .SetFont(thaiBoldFont)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(ColorConstants.WHITE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(3);
                        table.AddHeaderCell(scoreHeaderCell);

                        var scoreCell0 = new Cell().Add(new Paragraph("0")
                            .SetFontSize(11)
                            .SetMargin(0)
                            .SetMultipliedLeading(0.8f))
                            .SetFont(thaiBoldFont)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(ColorConstants.WHITE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(3);
                        table.AddHeaderCell(scoreCell0);
                        var scoreCell1 = new Cell().Add(new Paragraph("1")
                            .SetFontSize(11)
                            .SetMargin(0)
                            .SetMultipliedLeading(0.8f))
                            .SetFont(thaiBoldFont)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(ColorConstants.WHITE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(3);
                        table.AddHeaderCell(scoreCell1);
                        var scoreCell2 = new Cell().Add(new Paragraph("2")
                            .SetFontSize(11)
                            .SetMargin(0)
                            .SetMultipliedLeading(0.8f))
                            .SetFont(thaiBoldFont)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(ColorConstants.WHITE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(3);
                        table.AddHeaderCell(scoreCell2);
                        var scoreCell3 = new Cell().Add(new Paragraph("3")
                            .SetFontSize(11)
                            .SetMargin(0)
                            .SetMultipliedLeading(0.8f))
                            .SetFont(thaiBoldFont)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(ColorConstants.WHITE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(3);
                        table.AddHeaderCell(scoreCell3);
                        var scoreCell4 = new Cell().Add(new Paragraph("4")
                            .SetFontSize(11)
                            .SetMargin(0)
                            .SetMultipliedLeading(0.8f))
                            .SetFont(thaiBoldFont)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBackgroundColor(ColorConstants.WHITE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(3);
                        table.AddHeaderCell(scoreCell4);

                        // Add question rows
                        var questions = evaluation.Form.Questions.OrderBy(q => q.QuestionID).ToList();
                        for (int i = 0; i < questions.Count; i++)
                        {
                            var question = questions[i];
                            var detail = evaluation.EvaluationDetails?.FirstOrDefault(ed => ed.QuestionID == question.QuestionID);
                            var selectedScore = detail?.Score ?? 0;

                            // Question text cell
                            string questionText = GetQuestionDisplayText(i + 1, question.QuestionText);
                            var questionCell = CreateCell(questionText, thaiFont, false);
                            questionCell.SetFontSize(10); // ขนาด font สำหรับคำถาม
                            questionCell.SetVerticalAlignment(VerticalAlignment.TOP); // ชิดขอบบน
                            table.AddCell(questionCell);

                            // Score cells with checkboxes
                            for (int score = 0; score <= 4; score++)
                            {
                                string checkMark = selectedScore == score ? "☐" : "☐";
                                var scoreText = "";

                                if (score == 0)
                                {
                                    scoreText = $"{checkMark} ปกติ";
                                }
                                else
                                {
                                    // Get option text for this score
                                    var option = question.Options.FirstOrDefault(o => o.Value == score);
                                    if (option != null && !string.IsNullOrEmpty(option.OptionText))
                                    {
                                        scoreText = $"{checkMark} {option.OptionText}";
                                    }
                                    else
                                    {
                                        scoreText = checkMark;
                                    }
                                }

                                var cell = CreateCell(scoreText, thaiFont, false);
                                cell.SetFontSize(10);
                                cell.SetTextAlignment(TextAlignment.LEFT);
                                cell.SetVerticalAlignment(VerticalAlignment.TOP);
                                cell.SetMargin(0);
                                cell.SetPadding(3);
                                table.AddCell(cell);
                            }
                        }

                        document.Add(table);

                        // Add summary section
                        document.Add(new Paragraph(" ").SetFontSize(10));

                        var summaryTitle = new Paragraph("สรุปผลการประเมิน")
                            .SetFontSize(12)
                            .SetFont(thaiFont);
                        document.Add(summaryTitle);

                        var resultParagraph = new Paragraph()
                            .Add($"ผลการประเมิน: {evaluation.Result}")
                            .SetFontSize(10);
                        document.Add(resultParagraph);

                        if (!string.IsNullOrEmpty(evaluation.ResultDetail))
                        {
                            var detailParagraph = new Paragraph()
                                .Add($"รายละเอียด: {evaluation.ResultDetail}")
                                .SetFontSize(10);
                            document.Add(detailParagraph);
                        }

                        // Add evaluation info
                        document.Add(new Paragraph(" ").SetFontSize(10));
                        var evaluationInfo = new Paragraph()
                            .Add($"วันที่ประเมิน: {evaluation.RecordDate:dd/MM/yyyy HH:mm}")
                            .Add("\n")
                            .Add($"ผู้ประเมิน: {evaluation.User?.Title?.TitleName}{evaluation.User?.FirstName} {evaluation.User?.LastName}")
                            .SetFontSize(10);
                        document.Add(evaluationInfo);
                        document.Close();

                        var fileName = $"Evaluation_{evaluation.Patient.HN}_{evaluation.RecordDate:yyyyMMdd_HHmm}.pdf";
                        return File(stream.ToArray(), "application/pdf", fileName);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"เกิดข้อผิดพลาดในการสร้าง PDF: {ex.Message}");
                    }
                }
            }
            
            private Cell CreateHeaderCell(string content, PdfFont font)
            {
                var paragraph = new Paragraph(content)
                    .SetFont(font)
                    .SetFontSize(11) // เพิ่มขนาดฟอนต์เล็กน้อยเพื่อดูหนา
                    .SetMargin(0)
                    .SetMultipliedLeading(0.6f); // ลดระยะห่างบรรทัดให้น้อยลง

                return new Cell()
                    .Add(paragraph)
                    .SetFont(font)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                    .SetBorder(new SolidBorder(1))
                    .SetPadding(3);
            }

            private Cell CreateCell(string content, PdfFont font, bool isBold = false)
            {
                var paragraph = new Paragraph(content)
                    .SetFont(font)
                    .SetFontSize(8) // ขนาด font เริ่มต้น
                    .SetMargin(0) // ลบ margin
                    .SetMultipliedLeading(0.8f); // ลดระยะห่างบรรทัดให้น้อยลง

                return new Cell()
                    .Add(paragraph)
                    .SetBorder(new SolidBorder(1))
                    .SetVerticalAlignment(VerticalAlignment.TOP) // ชิดขอบบน
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetPadding(3); // เพิ่ม padding เล็กน้อย
            }

            private string GetQuestionDisplayText(int questionNumber, string? questionText)
            {
                    return $"{questionNumber}. {questionText ?? ""}";
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
                    .Where(p => p.PROVINCE_ID > 0) 
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
                    .Where(a => a.PROVINCE_ID == id && a.AMPHUR_ID > 0) 
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
                    .Where(d => d.AMPHUR_ID == id && d.DISTRICT_ID > 0) 
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
                    recordDate = patient.RecordDate?.ToString("yyyy-MM-dd"),
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
            public async Task<IActionResult> CreatePatient([FromBody] PatientModel model)
            {
                try
                {
                    // ตรวจสอบ model validation
                    if (!ModelState.IsValid)
                    {
                        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                        return Json(new { success = false, message = string.Join(", ", errors) });
                    }
                    
                    // ตั้งค่า default RecordDate หากไม่ได้ระบุ
                    if (!model.RecordDate.HasValue)
                    {
                        model.RecordDate = DateTime.Today;
                    }
                    
                    // ตรวจสอบว่าวันที่บันทึกไม่เกินวันปัจจุบัน
                    if (model.RecordDate.HasValue && model.RecordDate.Value.Date > DateTime.Today)
                    {
                        return Json(new { success = false, message = "วันที่บันทึกเวชระเบียนไม่สามารถเกินวันปัจจุบันได้" });
                    }
                    
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
                        RecordDate = model.RecordDate,
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
            public async Task<IActionResult> UpdatePatient([FromBody] PatientModel model)
            {
                try
                {
                    // ตรวจสอบ model validation
                    if (!ModelState.IsValid)
                    {
                        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                        return Json(new { success = false, message = string.Join(", ", errors) });
                    }
                    
                    // ตรวจสอบว่าวันที่บันทึกไม่เกินวันปัจจุบัน
                    if (model.RecordDate.HasValue && model.RecordDate.Value.Date > DateTime.Today)
                    {
                        return Json(new { success = false, message = "วันที่บันทึกเวชระเบียนไม่สามารถเกินวันปัจจุบันได้" });
                    }
                    
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
                    patient.RecordDate = model.RecordDate;
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

            [HttpGet]
            public async Task<IActionResult> GetForms(string search = "", int? formType = null, string status = "")
            {
                var query = _db.Form.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f => f.FormName != null && f.FormName.Contains(search));
                }

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
                    evaluationCount = f.Evaluations.Count(),
                    hasEvaluations = f.Evaluations.Any()
                }).OrderBy(f => f.formID).ToListAsync();

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
                await SetUserProfileData();
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

                            if (!string.IsNullOrEmpty(questionsJson))
                            {
                                var questionsData = System.Text.Json.JsonSerializer.Deserialize<List<QuestionData>>(questionsJson);

                                if (questionsData != null)
                                {
                                    foreach (var questionData in questionsData)
                                    {
                                        var question = new Question
                                        {
                                            QuestionText = questionData.QuestionText,
                                            Section = questionData.Section,
                                            Options = new List<Option>()
                                        };

                                        form.Questions.Add(question);

                                        if (questionData.Options != null)
                                        {
                                            foreach (var optionData in questionData.Options)
                                            {
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

                            if (!string.IsNullOrEmpty(interpretersJson))
                            {
                                var interpretersData = System.Text.Json.JsonSerializer.Deserialize<List<InterpreterData>>(interpretersJson);
 
                                if (interpretersData != null)
                                {
                                    foreach (var interpreterData in interpretersData)
                                    {
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
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"เกิดข้อผิดพลาดในการบันทึกข้อมูล {ex.Message}" });
                        }
                    }

                    return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            [HttpPost]
            public async Task<IActionResult> UpdateForm([FromForm] int formId, [FromForm] string formName, [FromForm] int formType, [FromForm] string? scoring, [FromForm] string? questionsJson, [FromForm] string? interpretersJson, [FromForm] bool forceUpdate = false)
            {
                try
                {
                    var form = await _db.Form
                        .Include(f => f.Questions)
                            .ThenInclude(q => q.Options)
                        .Include(f => f.Interpreters)
                        .Include(f => f.Evaluations)
                            .ThenInclude(e => e.EvaluationDetails)
                        .FirstOrDefaultAsync(f => f.FormID == formId);

                    if (form == null)
                    {
                        return Json(new { success = false, message = "ไม่พบแบบประเมิน" });
                    }

                    // ตรวจสอบว่าเป็นฟอร์มระบบหรือไม่
                    var systemForms = new[] { "Mod-SAS", "Mod-BARS", "Mod-AIMS" };
                    if (systemForms.Contains(form.FormName))
                    {
                        return Json(new { success = false, message = "ไม่สามารถแก้ไขแบบประเมินระบบได้" });
                    }

                    var existingForm = await _db.Form
                        .FirstOrDefaultAsync(f => f.FormName != null &&
                                                f.FormName.ToLower() == formName.ToLower() &&
                                                f.FormID != formId);

                    if (existingForm != null)
                    {
                        return Json(new { success = false, message = "ชื่อแบบประเมินนี้มีอยู่แล้วในระบบ กรุณาใช้ชื่ออื่น" });
                    }

                    // ตรวจสอบว่ามีการประเมินแล้วหรือไม่
                    var questionIds = form.Questions.Select(q => q.QuestionID).ToList();
                    var evaluatedQuestionIds = new HashSet<int>();
                    var evaluatedOptionIds = new HashSet<int>();

                    if (form.Evaluations.Any())
                    {
                        var evaluationDetails = form.Evaluations
                            .SelectMany(e => e.EvaluationDetails)
                            .Where(ed => ed.QuestionID.HasValue)
                            .ToList();

                        evaluatedQuestionIds = evaluationDetails
                            .Select(ed => ed.QuestionID!.Value)
                            .ToHashSet();

                        // หา option ที่เคยถูกใช้ในการประเมิน (ใช้วิธีแยกขั้นตอน)
                        var evaluationScores = await _db.EvaluationDetail
                            .Where(ed => ed.QuestionID.HasValue && evaluatedQuestionIds.Contains(ed.QuestionID.Value))
                            .Select(ed => new { QuestionId = ed.QuestionID!.Value, Score = ed.Score ?? 0 })
                            .ToListAsync();

                        var allOptions = await _db.Option
                            .Where(opt => opt.QuestionID.HasValue && evaluatedQuestionIds.Contains(opt.QuestionID.Value))
                            .ToListAsync();

                        var optionIdList = new List<int>();
                        foreach (var evalScore in evaluationScores)
                        {
                            var matchingOption = allOptions
                                .FirstOrDefault(opt => opt.QuestionID == evalScore.QuestionId && opt.Value == evalScore.Score);
                            if (matchingOption != null)
                            {
                                optionIdList.Add(matchingOption.OptionID);
                            }
                        }

                        evaluatedOptionIds = optionIdList.ToHashSet();
                    }

                    using var transaction = await _db.Database.BeginTransactionAsync();

                    try
                    {
                        // อัพเดทข้อมูลพื้นฐานของฟอร์ม
                        form.FormName = formName;
                        form.FormType = formType;
                        form.Scoring = scoring;

                        // จัดการคำถามใหม่
                        if (!string.IsNullOrEmpty(questionsJson))
                        {
                            var newQuestions = System.Text.Json.JsonSerializer.Deserialize<List<QuestionDataWithId>>(questionsJson);
                            if (newQuestions != null)
                            {
                                // ตรวจสอบการลบคำถามที่เคยถูกประเมิน
                                var questionsToDelete = form.Questions
                                    .Where(q => !newQuestions.Any(nq => nq.QuestionID == q.QuestionID))
                                    .ToList();

                                var deletingEvaluatedQuestions = questionsToDelete
                                    .Where(q => evaluatedQuestionIds.Contains(q.QuestionID))
                                    .ToList();

                                if (deletingEvaluatedQuestions.Any() && !forceUpdate)
                                {
                                    await transaction.RollbackAsync();
                                    return Json(new { 
                                        success = false, 
                                        message = $"ไม่สามารถลบคำถามที่เคยถูกประเมินได้ ({deletingEvaluatedQuestions.Count} คำถาม) หากต้องการลบ ระบบจะลบประวัติการประเมินที่เกี่ยวข้องด้วย",
                                        requiresForceUpdate = true,
                                        affectedEvaluations = form.Evaluations.Count
                                    });
                                }

                                // ลบคำถามที่ถูกขอให้ลบ
                                foreach (var questionToDelete in questionsToDelete)
                                {
                                    // ถ้าเป็นคำถามที่เคยถูกประเมิน และ forceUpdate = true ต้องลบ evaluation details ที่เกี่ยวข้อง
                                    if (evaluatedQuestionIds.Contains(questionToDelete.QuestionID) && forceUpdate)
                                    {
                                        var relatedEvaluationDetails = await _db.EvaluationDetail
                                            .Where(ed => ed.QuestionID == questionToDelete.QuestionID)
                                            .ToListAsync();
                                        _db.EvaluationDetail.RemoveRange(relatedEvaluationDetails);
                                    }

                                    _db.Option.RemoveRange(questionToDelete.Options);
                                    _db.Question.Remove(questionToDelete);
                                }

                                // จัดการคำถามที่เหลือ
                                foreach (var questionData in newQuestions)
                                {
                                    Question question;
                                    bool isNewQuestion = false;

                                    if (questionData.QuestionID > 0)
                                    {
                                        // คำถามเดิม - อัพเดท
                                        question = form.Questions.FirstOrDefault(q => q.QuestionID == questionData.QuestionID)!;
                                        if (question != null)
                                        {
                                            question.QuestionText = questionData.QuestionText ?? "";
                                            question.Section = questionData.Section;
                                        }
                                        else
                                        {
                                            continue; // ข้าม question ที่ไม่พบ
                                        }
                                    }
                                    else
                                    {
                                        // คำถามใหม่
                                        question = new Question
                                        {
                                            FormID = form.FormID,
                                            QuestionText = questionData.QuestionText ?? "",
                                            Section = questionData.Section
                                        };
                                        _db.Question.Add(question);
                                        isNewQuestion = true;
                                    }

                                    await _db.SaveChangesAsync(); // บันทึกเพื่อได้ QuestionID

                                    // จัดการตัวเลือก
                                    if (questionData.Options != null && !isNewQuestion)
                                    {
                                        // ตรวจสอบการลบตัวเลือกที่เคยถูกใช้
                                        var currentOptions = question.Options.ToList();
                                        var optionsToDelete = currentOptions
                                            .Where(o => !questionData.Options.Any(no => no.OptionID == o.OptionID))
                                            .ToList();

                                        var deletingUsedOptions = optionsToDelete
                                            .Where(o => evaluatedOptionIds.Contains(o.OptionID))
                                            .ToList();

                                        if (deletingUsedOptions.Any() && !forceUpdate)
                                        {
                                            await transaction.RollbackAsync();
                                            return Json(new { 
                                                success = false, 
                                                message = $"ไม่สามารถลบตัวเลือกที่เคยถูกใช้ในการประเมินได้ หากต้องการลบ ระบบจะลบประวัติการประเมินที่เกี่ยวข้องด้วย",
                                                requiresForceUpdate = true,
                                                affectedEvaluations = form.Evaluations.Count
                                            });
                                        }

                                        // ลบตัวเลือกที่ถูกขอให้ลบ และลบ evaluation details ที่เกี่ยวข้องถ้า forceUpdate = true
                                        if (forceUpdate)
                                        {
                                            var deletingUsedOptionIds = deletingUsedOptions.Select(o => o.OptionID).ToList();
                                            var relatedEvaluationDetails = await _db.EvaluationDetail
                                                .Where(ed => ed.QuestionID == question.QuestionID)
                                                .ToListAsync();
                                            
                                            // ลบ evaluation details ที่ใช้ option ที่กำลังจะลบ
                                            foreach (var evalDetail in relatedEvaluationDetails)
                                            {
                                                var usedOption = deletingUsedOptions.FirstOrDefault(o => 
                                                    o.QuestionID == evalDetail.QuestionID && o.Value == evalDetail.Score);
                                                if (usedOption != null)
                                                {
                                                    _db.EvaluationDetail.Remove(evalDetail);
                                                }
                                            }
                                        }
                                        
                                        _db.Option.RemoveRange(optionsToDelete);
                                    }
                                    else if (isNewQuestion && questionData.Options != null)
                                    {
                                        // คำถามใหม่ - ลบตัวเลือกเก่าทั้งหมดแล้วเพิ่มใหม่
                                        _db.Option.RemoveRange(question.Options);
                                    }

                                    // เพิ่ม/อัพเดทตัวเลือก
                                    if (questionData.Options != null)
                                    {
                                        foreach (var optionData in questionData.Options)
                                        {
                                            if (optionData.OptionID > 0 && !isNewQuestion)
                                            {
                                                // ตัวเลือกเดิม - อัพเดท
                                                var existingOption = question.Options
                                                    .FirstOrDefault(o => o.OptionID == optionData.OptionID);
                                                if (existingOption != null)
                                                {
                                                    existingOption.OptionText = optionData.OptionText ?? "";
                                                    existingOption.Value = optionData.Value ?? 0;
                                                }
                                            }
                                            else
                                            {
                                                // ตัวเลือกใหม่
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
                        }

                        // จัดการเกณฑ์การแปรผล
                        if (!string.IsNullOrEmpty(interpretersJson))
                        {
                            var newInterpreters = System.Text.Json.JsonSerializer.Deserialize<List<InterpreterDataWithId>>(interpretersJson);
                            if (newInterpreters != null)
                            {
                                // เพิ่มเกณฑ์ใหม่เท่านั้น (ห้ามลบเกณฑ์เก่า)
                                var newOnlyInterpreters = newInterpreters.Where(i => i.InterpreterID <= 0).ToList();
                                
                                foreach (var interpreterData in newOnlyInterpreters)
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

                                // อัพเดทเกณฑ์เดิม
                                var existingInterpreters = newInterpreters.Where(i => i.InterpreterID > 0).ToList();
                                foreach (var interpreterData in existingInterpreters)
                                {
                                    var existingInterpreter = form.Interpreters
                                        .FirstOrDefault(i => i.InterID == interpreterData.InterpreterID);
                                    if (existingInterpreter != null)
                                    {
                                        existingInterpreter.MinScore = interpreterData.MinScore ?? 0;
                                        existingInterpreter.MaxScore = interpreterData.MaxScore ?? 0;
                                        existingInterpreter.InterName = interpreterData.InterName ?? "";
                                        existingInterpreter.InterText = interpreterData.InterText ?? "";
                                    }
                                }
                            }
                        }

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var deletedEvaluations = 0;
                        if (forceUpdate && form.Evaluations.Any())
                        {
                            deletedEvaluations = form.Evaluations.Count;
                        }

                        return Json(new { 
                            success = true, 
                            message = "อัพเดทแบบประเมินเรียบร้อย",
                            deletedEvaluations = deletedEvaluations
                        });
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
            public async Task<IActionResult> DeleteForm(int id, [FromBody] DeleteFormRequest? request = null)
            {
                try
                {
                    var forceDelete = request?.ForceDelete ?? false;
                    
                    var form = await _db.Form
                        .Include(f => f.Questions)
                            .ThenInclude(q => q.Options)
                        .Include(f => f.Interpreters)
                        .Include(f => f.Evaluations)
                            .ThenInclude(e => e.EvaluationDetails)
                        .FirstOrDefaultAsync(f => f.FormID == id);

                    if (form == null)
                    {
                        return Json(new { success = false, message = "ไม่พบแบบประเมิน" });
                    }

                    // ตรวจสอบว่าเป็นฟอร์มระบบหรือไม่
                    var systemForms = new[] { "Mod-SAS", "Mod-BARS", "Mod-AIMS" };
                    if (systemForms.Contains(form.FormName))
                    {
                        return Json(new { success = false, message = "ไม่สามารถลบแบบประเมินระบบได้" });
                    }

                    // นับจำนวนการประเมินที่เกี่ยวข้อง
                    int evaluationsCount = form.Evaluations.Count;

                    // ถ้ามีการประเมินและไม่ได้ระบุ forceDelete ให้แจ้งเตือน
                    if (evaluationsCount > 0 && !forceDelete)
                    {
                        return Json(new { 
                            success = false, 
                            message = "ไม่สามารถลบแบบประเมินนี้ได้ เนื่องจากมีประวัติการประเมิน " + evaluationsCount + " รายการ",
                            hasEvaluations = true,
                            evaluationsCount = evaluationsCount
                        });
                    }

                    using var transaction = await _db.Database.BeginTransactionAsync();

                    try
                    {
                        int deletedEvaluations = 0;

                        // ลบประวัติการประเมินที่เกี่ยวข้อง (ถ้ามี)
                        if (form.Evaluations.Any())
                        {
                            foreach (var evaluation in form.Evaluations)
                            {
                                // ลบรายละเอียดการประเมิน
                                if (evaluation.EvaluationDetails.Any())
                                {
                                    _db.EvaluationDetail.RemoveRange(evaluation.EvaluationDetails);
                                }
                                
                                // ลบการประเมิน
                                _db.Evaluation.Remove(evaluation);
                                deletedEvaluations++;
                            }
                        }

                        // ลบคำถามและตัวเลือก
                        foreach (var question in form.Questions)
                        {
                            _db.Option.RemoveRange(question.Options);
                        }
                        _db.Question.RemoveRange(form.Questions);

                        // ลบเกณฑ์การแปรผล
                        _db.Interpreter.RemoveRange(form.Interpreters);

                        // ลบแบบประเมิน
                        _db.Form.Remove(form);

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Json(new { 
                            success = true, 
                            message = "ลบแบบประเมินเรียบร้อย",
                            deletedEvaluations = deletedEvaluations
                        });
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

            public class DeleteFormRequest
            {
                public bool ForceDelete { get; set; }
            }

            public class QuestionDataWithId : QuestionData
            {
                [JsonPropertyName("questionID")]
                public int QuestionID { get; set; }

                [JsonPropertyName("options")]
                public new List<OptionDataWithId>? Options { get; set; }
            }

            public class OptionDataWithId : OptionData
            {
                [JsonPropertyName("optionID")]
                public int OptionID { get; set; }
            }

            public class InterpreterDataWithId : InterpreterData
            {
                [JsonPropertyName("interpreterID")]
                public int InterpreterID { get; set; }
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

            [HttpGet]
            public async Task<IActionResult> GetTreatments(string search = "")
            {
                var query = _db.Treatment.AsQueryable();

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

            [HttpGet]
            public async Task<IActionResult> GetUserProfile()
            {
                try
                {
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

                    if (profilePicture == null || profilePicture.Length == 0)
                    {
                        return Json(new { success = false, message = "ไม่พบไฟล์รูปภาพ" });
                    }

                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(profilePicture.ContentType.ToLower()))
                    {
                        return Json(new { success = false, message = "รูปแบบไฟล์ไม่ถูกต้อง (อนุญาตเฉพาะ JPG, PNG, GIF)" });
                    }

                    if (profilePicture.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "ขนาดไฟล์ใหญ่เกินไป (สูงสุด 5MB)" });
                    }

                    var uploadsPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    if (!string.IsNullOrEmpty(user!.Picture))
                    {
                        var oldFilePath = System.IO.Path.Combine(uploadsPath, user.Picture);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    var fileName = $"{user.UserID}{System.IO.Path.GetExtension(profilePicture.FileName)}";
                    var filePath = System.IO.Path.Combine(uploadsPath, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                    }

                    user.Picture = fileName;
                    await _db.SaveChangesAsync();

                    var pictureUrl = $"/uploads/profile/{fileName}";

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

                    if (!string.IsNullOrEmpty(user.Picture))
                    {
                        var uploadsPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");
                        var filePath = System.IO.Path.Combine(uploadsPath, user.Picture);
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

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

                    if (string.IsNullOrEmpty(model.FirstName) || string.IsNullOrEmpty(model.LastName))
                    {
                        return Json(new { success = false, message = "กรุณากรอกชื่อและนามสกุล" });
                    }

                    if (!string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        if (string.IsNullOrEmpty(model.NewPassword))
                        {
                            return Json(new { success = false, message = "กรุณากรอกรหัสผ่านใหม่" });
                        }

                        var passwordService = new PasswordService();

                        if (!passwordService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                        {
                            return Json(new { success = false, message = "รหัสผ่านปัจจุบันไม่ถูกต้อง" });
                        }

                        if (model.NewPassword.Length < 6)
                        {
                            return Json(new { success = false, message = "รหัสผ่านใหม่ต้องมีอย่างน้อย 6 ตัวอักษร" });
                        }

                        user.PasswordHash = passwordService.HashPassword(model.NewPassword);
                    }

                    user.TitleID = model.TitleID;
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;

                    await _db.SaveChangesAsync();

                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {

                        var updatedUser = await _db.User
                            .Include(u => u.Title)
                            .Include(u => u.Role)
                            .FirstOrDefaultAsync(u => u.UserID == user.UserID);

                        if (updatedUser != null)
                        {

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
                public string? ResultDetail { get; set; }
                public decimal? FinalScore { get; set; }
                public string? ScoringMethod { get; set; }
                public List<EvaluationAnswerModel> Answers { get; set; } = new List<EvaluationAnswerModel>();
            }

            public class EvaluationAnswerModel
            {
                public int QuestionId { get; set; }
                public int Score { get; set; }
            }
        }
    }
