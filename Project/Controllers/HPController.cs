using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models.Entity;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Colors;
using iText.IO.Font;
using ClosedXML.Excel;
using XLColor = ClosedXML.Excel.XLColor;

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
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
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

            DateTime startDateParsed = DateTime.Today.AddDays(-7);
            DateTime endDateParsed = DateTime.Today.AddDays(1);
            string? defaultFormType = formType;

            if (string.IsNullOrEmpty(defaultFormType))
            {
                var defaultForm = await _context.Form.FirstOrDefaultAsync(f => f.FormID == 8);
                defaultFormType = defaultForm?.FormName ?? "";
            }

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStart))
                startDateParsed = parsedStart;
            
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
                endDateParsed = parsedEnd.AddDays(1); 

            ViewBag.StartDate = startDateParsed.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDateParsed.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.FormType = defaultFormType;

            ViewBag.TotalPatients = await _context.Patient.CountAsync();
            ViewBag.PatientsAssessed = await _context.Evaluation.Select(e => e.HN).Distinct().CountAsync();
            ViewBag.AverageScore = 8.5;
            ViewBag.SevereCases = await _context.Evaluation.CountAsync(e => e.Result != null && 
                (e.Result.Contains("รุนแรง") || e.Result.Contains("Severe")));

            var forms = await _context.Form.Select(f => new { f.FormID, f.FormName }).ToListAsync();
            ViewBag.Forms = forms;

            return View();
        }

        public async Task<IActionResult> Patients()
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
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

        public async Task<IActionResult> ExportReport()
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
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

            var forms = await _context.Form
                .OrderBy(f => f.FormName)
                .ToListAsync();
            
            ViewBag.Forms = forms;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEvaluationsByForm(int formId)
        {
            var evaluations = await _context.Evaluation
                .Include(e => e.Patient)
                    .ThenInclude(p => p!.Title)
                .Include(e => e.Form)
                .Where(e => e.FormID == formId)
                .OrderByDescending(e => e.RecordDate)
                .Select(e => new
                {
                    evaId = e.EvaID,
                    recordDate = e.RecordDate,
                    patientName = (e.Patient!.Title!.TitleName ?? "") + e.Patient.FirstName + " " + e.Patient.LastName,
                    hn = e.Patient.HN,
                    risk = e.Result ?? "-",
                    guideline = e.ResultDetail ?? "-",
                    formName = e.Form!.FormName
                })
                .ToListAsync();

            return Json(new { success = true, data = evaluations });
        }

        [HttpGet]
        public async Task<IActionResult> ExportEvaluationReportPDF(int formId)
        {
            try
            {
                var evaluations = await _context.Evaluation
                    .Include(e => e.Patient)
                        .ThenInclude(p => p!.Title)
                    .Include(e => e.Form)
                    .Where(e => e.FormID == formId)
                    .OrderByDescending(e => e.RecordDate)
                    .ToListAsync();

                if (!evaluations.Any())
                {
                    return BadRequest("ไม่พบข้อมูลการประเมิน");
                }

                var formName = evaluations.First().Form?.FormName ?? "ไม่ระบุแบบฟอร์ม";

                string thaiFontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "font", "THSarabunNew.ttf");
                var thaiFontProgram = FontProgramFactory.CreateFont(thaiFontPath);
                var thaiFont = PdfFontFactory.CreateFont(thaiFontProgram, PdfEncodings.IDENTITY_H);

                string thaiBoldFontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "font", "THSarabunNewBold.ttf");
                var thaiBoldFontProgram = FontProgramFactory.CreateFont(thaiBoldFontPath);
                var thaiBoldFont = PdfFontFactory.CreateFont(thaiBoldFontProgram, PdfEncodings.IDENTITY_H);

                MemoryStream stream = new MemoryStream();
                PdfWriter writer = new PdfWriter(stream);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

                document.SetMargins(40, 40, 40, 40);

                Paragraph title = new Paragraph("รายงานการประเมินผู้ป่วย")
                    .SetFont(thaiBoldFont)
                    .SetFontSize(15)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(title);

                Paragraph subtitle = new Paragraph($"แบบประเมิน: {formName}")
                    .SetFont(thaiBoldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(subtitle);

                Paragraph datePrinted = new Paragraph($"วันที่ออกรายงาน: {ConvertToThaiDate(DateTime.Now)} {DateTime.Now:HH:mm} น.")
                    .SetFont(thaiBoldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(5);
                document.Add(datePrinted);

                float[] columnWidths = { 50, 120, 200, 100, 280 };
                Table table = new Table(UnitValue.CreatePointArray(columnWidths));
                table.SetWidth(UnitValue.CreatePercentValue(100));

                string[] headers = { "ลำดับ", "วันที่ประเมิน", "HN / ชื่อผู้ป่วย", "ความเสี่ยง", "แนวทางปฏิบัติ" };
                foreach (string header in headers)
                {
                    Cell headerCell = new Cell()
                        .Add(new Paragraph(header)
                            .SetFont(thaiBoldFont)
                            .SetFontSize(14)
                            .SetFontColor(ColorConstants.WHITE))
                        .SetBackgroundColor(new DeviceRgb(44, 90, 160))
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPadding(4)
                        .SetBorder(new SolidBorder(ColorConstants.WHITE, 1));
                    table.AddHeaderCell(headerCell);
                }

                int rowNumber = 1;
                bool isAlternate = false;

                foreach (var evaluation in evaluations)
                {
                    var backgroundColor = isAlternate 
                        ? new DeviceRgb(245, 247, 250) 
                        : ColorConstants.WHITE;

                    table.AddCell(CreateDataCell(rowNumber.ToString(), thaiFont, backgroundColor, TextAlignment.CENTER));

                    string dateStr = ConvertToThaiDate(evaluation.RecordDate);
                    table.AddCell(CreateDataCell(dateStr, thaiFont, backgroundColor, TextAlignment.CENTER));

                    string hn = evaluation.Patient?.HN ?? "-";
                    string patientName = $"{evaluation.Patient?.Title?.TitleName}{evaluation.Patient?.FirstName} {evaluation.Patient?.LastName}";
                    string hnAndName = $"{patientName}\n{hn}";
                    table.AddCell(CreateDataCell(hnAndName, thaiFont, backgroundColor, TextAlignment.LEFT));

                    string risk = evaluation.Result ?? "-";
                    DeviceRgb riskColor = GetRiskColor(risk);
                    Cell riskCell = new Cell()
                        .Add(new Paragraph(risk)
                            .SetFont(thaiBoldFont)
                            .SetFontSize(12)
                            .SetFontColor(ColorConstants.WHITE))
                        .SetBackgroundColor(riskColor)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPadding(5)
                        .SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f));
                    table.AddCell(riskCell);

                    string guideline = evaluation.ResultDetail ?? "-";
                    table.AddCell(CreateDataCell(guideline, thaiFont, backgroundColor, TextAlignment.LEFT));

                    rowNumber++;
                    isAlternate = !isAlternate;
                }

                document.Add(table);

                Paragraph footer = new Paragraph($"จำนวนรายการทั้งหมด: {evaluations.Count} รายการ")
                    .SetFont(thaiFont)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginTop(12);
                document.Add(footer);

                document.Close();

                var fileName = $"รายงานการประเมิน_{formName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        private Cell CreateDataCell(string content, PdfFont font, Color backgroundColor, TextAlignment alignment)
        {
            return new Cell()
                .Add(new Paragraph(content)
                    .SetFont(font)
                    .SetFontSize(12))
                .SetBackgroundColor(backgroundColor)
                .SetTextAlignment(alignment)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(4)
                .SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f));
        }

        private string ConvertToThaiDate(DateTime date)
        {
            var thaiMonths = new[] { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
                                     "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            int thaiYear = date.Year + 543;
            return $"{date.Day} {thaiMonths[date.Month]} {thaiYear}";
        }

        private DeviceRgb GetRiskColor(string risk)
        {
            string riskLower = risk.ToLower();
            
            if (riskLower.Contains("รุนแรง") || riskLower.Contains("severe") || 
                riskLower.Contains("สูง") || riskLower.Contains("high") ||
                riskLower.Contains("marked"))
            {
                return new DeviceRgb(220, 53, 69); 
            }
            else if (riskLower.Contains("ปานกลาง") || riskLower.Contains("moderate") || 
                     riskLower.Contains("medium") || riskLower.Contains("mild"))
            {
                return new DeviceRgb(255, 193, 7); 
            }
            else if (riskLower.Contains("ปกติ") || riskLower.Contains("น้อย") || 
                     riskLower.Contains("absent") || riskLower.Contains("questionable") ||
                     riskLower.Contains("ไม่พบ"))
            {
                return new DeviceRgb(40, 167, 69); 
            }
            else
            {
                return new DeviceRgb(108, 117, 125); 
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportEvaluationReportExcel(int formId)
        {
            try
            {
                var evaluations = await _context.Evaluation
                    .Include(e => e.Patient)
                        .ThenInclude(p => p!.Title)
                    .Include(e => e.Form)
                    .Where(e => e.FormID == formId)
                    .OrderByDescending(e => e.RecordDate)
                    .ToListAsync();

                if (!evaluations.Any())
                {
                    return BadRequest("ไม่พบข้อมูลการประเมิน");
                }

                var formName = evaluations.First().Form?.FormName ?? "ไม่ระบุแบบฟอร์ม";

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("รายงานการประเมิน");

                    worksheet.Column(1).Width = 8;  
                    worksheet.Column(2).Width = 18; 
                    worksheet.Column(3).Width = 15; 
                    worksheet.Column(4).Width = 30; 
                    worksheet.Column(5).Width = 20; 
                    worksheet.Column(6).Width = 50; 

                    var titleRange = worksheet.Range("A1:F1");
                    titleRange.Merge();
                    var titleCell = worksheet.Cell(1, 1);
                    titleCell.Value = "รายงานการประเมินผู้ป่วย";
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.Font.FontSize = 18;
                    titleCell.Style.Font.FontName = "TH Sarabun New";
                    titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    titleCell.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 90, 160);
                    titleCell.Style.Font.FontColor = XLColor.White;

                    var subtitleRange = worksheet.Range("A2:F2");
                    subtitleRange.Merge();
                    var subtitleCell = worksheet.Cell(2, 1);
                    subtitleCell.Value = $"แบบประเมิน: {formName}";
                    subtitleCell.Style.Font.Bold = true;
                    subtitleCell.Style.Font.FontSize = 17;
                    subtitleCell.Style.Font.FontName = "TH Sarabun New";
                    subtitleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    subtitleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    subtitleCell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                    subtitleCell.Style.Font.FontColor = XLColor.White;

                    var dateRange = worksheet.Range("A3:F3");
                    dateRange.Merge();
                    var dateCell = worksheet.Cell(3, 1);
                    dateCell.Value = $"วันที่ออกรายงาน: {ConvertToThaiDate(DateTime.Now)} {DateTime.Now:HH:mm} น.";
                    dateCell.Style.Font.Bold = true;
                    dateCell.Style.Font.FontSize = 17;
                    dateCell.Style.Font.FontName = "TH Sarabun New";
                    dateCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    dateCell.Style.Fill.BackgroundColor = XLColor.LightGray;

                    int headerRow = 4;
                    string[] headers = { "ลำดับ", "วันที่ประเมิน", "HN", "ชื่อผู้ป่วย", "ความเสี่ยง", "แนวทางปฏิบัติ" };
                    
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var headerCell = worksheet.Cell(headerRow, i + 1);
                        headerCell.Value = headers[i];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Font.FontSize = 16;
                        headerCell.Style.Font.FontName = "TH Sarabun New";
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 90, 160);
                        headerCell.Style.Font.FontColor = XLColor.White;
                        headerCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                    int currentRow = 5;
                    int rowNumber = 1;

                    foreach (var evaluation in evaluations)
                    {
                        var rowColor = rowNumber % 2 == 0 ? XLColor.FromArgb(245, 247, 250) : XLColor.White;

                        var cell1 = worksheet.Cell(currentRow, 1);
                        cell1.Value = rowNumber;
                        cell1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell1.Style.Fill.BackgroundColor = rowColor;
                        cell1.Style.Font.FontSize = 14;
                        cell1.Style.Font.FontName = "TH Sarabun New";
                        cell1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell1.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        var cell2 = worksheet.Cell(currentRow, 2);
                        cell2.Value = ConvertToThaiDate(evaluation.RecordDate);
                        cell2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell2.Style.Fill.BackgroundColor = rowColor;
                        cell2.Style.Font.FontSize = 14;
                        cell2.Style.Font.FontName = "TH Sarabun New";
                        cell2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell2.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        var cell3 = worksheet.Cell(currentRow, 3);
                        cell3.Value = evaluation.Patient?.HN ?? "-";
                        cell3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell3.Style.Fill.BackgroundColor = rowColor;
                        cell3.Style.Font.FontSize = 14;
                        cell3.Style.Font.FontName = "TH Sarabun New";
                        cell3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell3.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        var cell4 = worksheet.Cell(currentRow, 4);
                        string patientName = $"{evaluation.Patient?.Title?.TitleName}{evaluation.Patient?.FirstName} {evaluation.Patient?.LastName}";
                        cell4.Value = patientName;
                        cell4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        cell4.Style.Fill.BackgroundColor = rowColor;
                        cell4.Style.Font.FontSize = 14;
                        cell4.Style.Font.FontName = "TH Sarabun New";
                        cell4.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell4.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        var cell5 = worksheet.Cell(currentRow, 5);
                        string risk = evaluation.Result ?? "-";
                        cell5.Value = risk;
                        cell5.Style.Font.Bold = true;
                        cell5.Style.Font.FontSize = 14;
                        cell5.Style.Font.FontName = "TH Sarabun New";
                        cell5.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell5.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        
                        string riskLower = risk.ToLower();
                        if (riskLower.Contains("รุนแรง") || riskLower.Contains("severe") || 
                            riskLower.Contains("สูง") || riskLower.Contains("high"))
                        {
                            cell5.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 53, 69);
                            cell5.Style.Font.FontColor = XLColor.White;
                        }
                        else if (riskLower.Contains("ปานกลาง") || riskLower.Contains("moderate") || 
                                 riskLower.Contains("medium"))
                        {
                            cell5.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 193, 7);
                            cell5.Style.Font.FontColor = XLColor.Black;
                        }
                        else
                        {
                            cell5.Style.Fill.BackgroundColor = XLColor.FromArgb(40, 167, 69);
                            cell5.Style.Font.FontColor = XLColor.White;
                        }
                        cell5.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        var cell6 = worksheet.Cell(currentRow, 6);
                        string guideline = evaluation.ResultDetail ?? "-";
                        cell6.Value = guideline;
                        cell6.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        cell6.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                        cell6.Style.Alignment.WrapText = true;
                        cell6.Style.Fill.BackgroundColor = rowColor;
                        cell6.Style.Font.FontSize = 14;
                        cell6.Style.Font.FontName = "TH Sarabun New";
                        cell6.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        currentRow++;
                        rowNumber++;
                    }

                    var footerRange = worksheet.Range($"A{currentRow}:F{currentRow}");
                    footerRange.Merge();
                    var footerCell = worksheet.Cell(currentRow, 1);
                    footerCell.Value = $"จำนวนรายการทั้งหมด: {evaluations.Count} รายการ";
                    footerCell.Style.Font.Bold = true;
                    footerCell.Style.Font.FontSize = 14;
                    footerCell.Style.Font.FontName = "TH Sarabun New";
                    footerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    footerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    footerCell.Style.Fill.BackgroundColor = XLColor.LightGray;

                    worksheet.Row(1).Height = 30;
                    worksheet.Row(2).Height = 25;
                    worksheet.Row(3).Height = 20;
                    worksheet.Row(4).Height = 25;

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        var fileName = $"รายงานการประเมิน_{formName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        public async Task<IActionResult> EvaluationDetail(int id)
        {
            var roleId = User.FindFirst("RoleID")?.Value;
            if (roleId != "2")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.UserName = User.FindFirst("FirstName")?.Value + " " + User.FindFirst("LastName")?.Value;
            ViewBag.RoleName = User.FindFirst("RoleName")?.Value;
            
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
                DateTime startDateParsed = DateTime.Today.AddDays(-7);
                DateTime endDateParsed = DateTime.Today.AddDays(1);

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var parsedStart))
                    startDateParsed = parsedStart;
                
                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
                    endDateParsed = parsedEnd.AddDays(1);

                var evaluations = await _context.Evaluation
                    .Include(e => e.Form)
                    .Where(e => e.RecordDate >= startDateParsed && 
                               e.RecordDate < endDateParsed &&
                               (string.IsNullOrEmpty(formType) || e.Form!.FormName == formType))
                    .Select(e => new { e.Result, FormName = e.Form!.FormName })
                    .ToListAsync();

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

                var distribution = new List<object>
                {
                    new { label = "ความรุนแรงน้อย", count = severityGroups["ความรุนแรงน้อย"], color = "#28a745" },
                    new { label = "ความรุนแรงปานกลาง", count = severityGroups["ความรุนแรงปานกลาง"], color = "#ffc107" },
                    new { label = "ความรุนแรงมาก", count = severityGroups["ความรุนแรงมาก"], color = "#dc3545" }
                };

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
                DateTime startDateParsed = DateTime.Today.AddDays(-7);
                DateTime endDateParsed = DateTime.Today.AddDays(1);

                if (DateTime.TryParse(startDate, out DateTime parsedStart))
                {
                    startDateParsed = parsedStart;
                }

                if (DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    endDateParsed = parsedEnd.AddDays(1); 
                }

                var evaluationsQuery = _context.Evaluation
                    .Include(e => e.Patient)
                    .Include(e => e.Form)
                    .Include(e => e.EvaluationDetails)
                    .Where(e => e.RecordDate >= startDateParsed && e.RecordDate < endDateParsed);

                if (!string.IsNullOrEmpty(formType))
                {
                    evaluationsQuery = evaluationsQuery.Where(e => e.Form!.FormName == formType);
                }

                var evaluations = await evaluationsQuery.ToListAsync();

                var totalPatients = await _context.Patient.CountAsync();
                var patientsAssessed = evaluations.Select(e => e.HN).Distinct().Count();
                
                var averageScore = 0.0;
                if (evaluations.Any())
                {
                    var totalScores = evaluations.Select(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0)).ToList();
                    averageScore = totalScores.Any() ? totalScores.Average() : 0;
                }
                
                var severeCases = evaluations.Count(e => e.Result == "รุนแรง" || e.Result == "Severe" || e.Result == "Severe akathisia");

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

                var trendData = new List<object>();
                var currentDate = startDateParsed;
                
                while (currentDate < endDateParsed.AddDays(-1))
                {
                    var dayEvaluations = evaluations.Where(e => e.RecordDate.Date == currentDate.Date).ToList();
                    
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

            var lowSeverity = new[] { "ปกติ", "Absent", "Questionable", "Mild akathisia", "Mild", "น้อยมาก", "ไม่พบอาการ" };
            
            var moderateSeverity = new[] { "Moderate", "Moderate akathisia", "เล็กน้อย", "ปานกลาง" };
            
            var highSeverity = new[] { "Marked akathisia", "Severe", "Severe akathisia", "รุนแรง" };

            if (lowSeverity.Contains(result))
                return "ความรุนแรงน้อย";
            else if (moderateSeverity.Contains(result))
                return "ความรุนแรงปานกลาง";
            else if (highSeverity.Contains(result))
                return "ความรุนแรงมาก";
            else
                return "ความรุนแรงน้อย"; 
        }

        [HttpGet]
        public async Task<JsonResult> GetRealDashboardData(string? startDate = null, string? endDate = null, string? formType = null)
        {
            try
            {
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

                var evaluationsQuery = _context.Evaluation
                    .Include(e => e.Patient)
                    .Include(e => e.Form)
                    .Include(e => e.EvaluationDetails)
                    .Where(e => e.RecordDate.Date >= startDateParsed.Date && e.RecordDate.Date < endDateParsed.Date);

                if (!string.IsNullOrEmpty(formType))
                {
                    evaluationsQuery = evaluationsQuery.Where(e => e.Form!.FormName == formType);
                }

                var evaluations = await evaluationsQuery.ToListAsync();

                var totalPatients = await _context.Patient.CountAsync();
                var patientsAssessed = evaluations.Select(e => e.HN).Distinct().Count();
                
                var averageScore = 0.0;
                if (evaluations.Any())
                {
                    var totalScores = evaluations.Select(e => e.EvaluationDetails.Sum(ed => ed.Score ?? 0)).ToList();
                    averageScore = totalScores.Any() ? totalScores.Average() : 0;
                }
                
                var severeCases = evaluations.Count(e => e.Result == "รุนแรง" || e.Result == "Severe" || e.Result == "Severe akathisia");

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

                var availableForms = await _context.Form.Select(f => f.FormName).Distinct().ToListAsync();
                var trendData = new List<object>();
                var currentDate = startDateParsed.Date;
                
                while (currentDate < endDateParsed.Date)
                {
                    var dayEvaluations = evaluations.Where(e => e.RecordDate.Date == currentDate.Date).ToList();
                    var dateStr = currentDate.ToString("yyyy-MM-dd");
                    
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
                "ความรุนแรงน้อย" => "#28a745", 
                "ความรุนแรงปานกลาง" => "#ffc107", 
                "ความรุนแรงมาก" => "#dc3545", 
                _ => "#6c757d" 
            };
        }

    }

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
