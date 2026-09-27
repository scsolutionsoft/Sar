using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
[Authorize]
public class PortfoliosController(ApplicationDbContext db, AccessService access, ReportService reports) : EditorController(db, access)
{
    public async Task<IActionResult> Index(ReportFilter f)
    {
        if (Access.IsStudent) return Forbid();
        if (!ModelState.IsValid || f.FiscalYear is < 2400 or > 3000 || f.From > f.To) return BadRequest("ตัวกรองไม่ถูกต้อง");
        if (f.TeacherId != null && f.StudentId != null) return BadRequest("เลือกครูหรือนักเรียนเพียงคนเดียว");
        f.Kind = f.StudentId.HasValue ? "StudentPortfolio" : "TeacherPortfolio";
        ViewBag.Filter = f; await LookupsAsync();
        var rows = f.TeacherId.HasValue || f.StudentId.HasValue ? await reports.BuildAsync(f) : [];
        var awards = await Access.AwardsAsync();
        if (f.TeacherId.HasValue) awards = awards.Where(x => x.Teachers.Any(t => t.TeacherId == f.TeacherId));
        else if (f.StudentId.HasValue) awards = awards.Where(x => x.Students.Any(t => t.StudentId == f.StudentId));
        else awards = awards.Where(x => false);
        if (f.AcademicYearId.HasValue) awards = awards.Where(x => x.AcademicYearId == f.AcademicYearId);
        if (f.From.HasValue) awards = awards.Where(x => x.AwardDate >= f.From);
        if (f.To.HasValue) { var end = f.To.Value.Date.AddDays(1); awards = awards.Where(x => x.AwardDate < end); }
        if (f.DepartmentId.HasValue) awards = awards.Where(x => x.DepartmentId == f.DepartmentId);
        if (f.SubjectGroupId.HasValue) awards = awards.Where(x => x.SubjectGroupId == f.SubjectGroupId);
        ViewBag.Files = await Db.Set<AwardCertificate>().Where(x => awards.Select(a => a.Id).Contains(x.AwardId)).Select(x => new FileInfoView(x.FileId, x.File.OriginalFileName, x.File.ContentType, 0)).ToListAsync();
        return View(rows);
    }
}
