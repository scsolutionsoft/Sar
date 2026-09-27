using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
[Authorize]
public class ReportsController(ApplicationDbContext db, AccessService access, ReportService reports, ExportService export, IFileStorage storage) : EditorController(db, access)
{
    [HttpPost]
    public async Task<IActionResult> SavePdf(ReportFilter f)
    {
        if (!ReportService.Kinds.ContainsKey(f.Kind) || f.FiscalYear is < 2400 or > 3000 || f.From > f.To || !ModelState.IsValid) return BadRequest("ตัวกรองไม่ถูกต้อง");
        var rows = await reports.BuildAsync(f); var title = ReportService.Kinds[f.Kind];
        var bytes = await export.PdfAsync(export.ReportHtml(title, rows));
        try {
            var pages = PersonalPdfService.PageCount(bytes);
            Db.Add(new SchoolPortfolioPR.Models.PersonalDocument { OwnerId = Access.UserId, Title = title, Category = "รายงาน", DocumentDate = DateTime.UtcNow.AddHours(7).Date, Pages = pages,
                Description = $"รายงานจากระบบ {rows.Count} รายการ", File = await storage.StoreGeneratedAsync(bytes,"saved-report.pdf",Access.UserId) });
            await Db.SaveChangesAsync(); TempData["Success"] = "เก็บรายงาน PDF ในคลังส่วนตัวแล้ว";
        } catch (InvalidDataException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index","Documents");
    }
    public async Task<IActionResult> Index(ReportFilter f, string? format)
    {
        if (Access.IsStudent) { ModelState.Remove(nameof(f.StudentId)); ModelState.Remove(nameof(f.TeacherId)); }
        if (!ReportService.Kinds.ContainsKey(f.Kind) || f.FiscalYear is < 2400 or > 3000 || f.From > f.To || !ModelState.IsValid) return BadRequest("ตัวกรองไม่ถูกต้อง");
        var rows = await reports.BuildAsync(f); var title = ReportService.Kinds[f.Kind];
        if (format == "pdf") return File(await export.PdfAsync(export.ReportHtml(title, rows)), "application/pdf", "report.pdf");
        if (format == "xlsx") return File(export.Excel(title, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
        if (format == "docx") return File(export.Word(title, rows), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "report.docx");
        if (format == "print") return Content(export.ReportHtml(title, rows), "text/html; charset=utf-8");
        ViewBag.Filter = f; ViewBag.Title = title; await LookupsAsync(); return View(rows);
    }
}
