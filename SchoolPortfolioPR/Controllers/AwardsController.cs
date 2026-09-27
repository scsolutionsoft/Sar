using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
[Authorize]
public class AwardsController(ApplicationDbContext db, AccessService access, IFileStorage storage) : EditorController(db, access)
{
    public async Task<IActionResult> Index(string? q, int page = 1, WorkflowStatus? status = null, bool mine = false)
    {
        var query = await Access.AwardsAsync(); if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.AwardName.Contains(q) || x.CompetitionName.Contains(q));
        if (!ModelState.IsValid || status.HasValue && !Enum.IsDefined(status.Value)) return BadRequest();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (mine) query = query.Where(x => x.CreatedBy == Access.UserId);
        ViewBag.Status = status; ViewBag.Mine = mine; ViewBag.CanCreate = await Access.CanAuthorAsync();
        page = Math.Max(1, page); ViewBag.Search = q;
        return View(new PageView<Award>(await query.Include(x => x.Department).OrderByDescending(x => x.AwardDate).Skip((page - 1) * 20).Take(20).ToListAsync(), page, await query.CountAsync()));
    }
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var a = await Db.Awards.AsNoTracking().Include(x => x.Department).Include(x => x.AcademicYear).Include(x => x.Teachers).ThenInclude(x => x.Teacher).Include(x => x.Students).ThenInclude(x => x.Student).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id);
        if (a == null || !await Access.CanReadAsync(a, a.DepartmentId, a.Status, a.IsPublic)) return NotFound();
        ViewBag.Files = await (await Access.CertificatesAsync()).Where(x => x.AwardId == id).Select(x => new FileInfoView(x.FileId, x.File.OriginalFileName, x.File.ContentType, 0)).ToListAsync();
        ViewBag.CanEdit = User.Identity?.IsAuthenticated == true && await Access.CanEditAsync(a, a.DepartmentId, a.Status);
        if (ViewBag.CanEdit) {
            ViewBag.CertificateStudents = a.Students.Select(x=>x.Student).OrderBy(x=>x.Name).ToList();
            ViewBag.CertificateAssignments = await Db.Set<AwardCertificate>().Where(x=>x.AwardId==id).Select(x=>new CertificateRecipientView(x.FileId,x.File.OriginalFileName,x.StudentId)).ToListAsync();
        }
        if (Access.IsStudent) { var student = await Access.StudentAsync(); a.Students = a.Students.Where(x=>x.StudentId==student).ToList(); }
        var transitions = new List<WorkflowStatus>(); foreach (var s in Enum.GetValues<WorkflowStatus>()) if (await Access.CanTransitionAsync(a, a.DepartmentId, a.Status, s)) transitions.Add(s);
        ViewBag.Transitions = transitions; return View(a);
    }
    [Authorize(Policy = "Editor")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null && !await Access.CanAuthorAsync()) return Forbid();
        await LookupsAsync(forAuthoring: true); if (id == null) return View(new AwardForm { DepartmentId = await Access.DepartmentAsync() ?? 0 });
        var a = await Db.Awards.Include(x => x.Teachers).Include(x => x.Students).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!await Access.CanEditAsync(a, a.DepartmentId, a.Status)) return Forbid();
        return View(new AwardForm { Id = a.Id, RowVersion = Convert.ToBase64String(a.RowVersion), AwardName = a.AwardName, CompetitionName = a.CompetitionName,
            AwardLevel = a.AwardLevel, AwardRank = a.AwardRank, Organizer = a.Organizer, AwardDate = a.AwardDate, AcademicYearId = a.AcademicYearId,
            DepartmentId = a.DepartmentId, SubjectGroupId = a.SubjectGroupId, ActivityId = a.ActivityId, Description = a.Description, IsPublic = a.IsPublic,
            TeacherIds = a.Teachers.Select(x => x.TeacherId).ToArray(), StudentIds = a.Students.Select(x => x.StudentId).ToArray(), TeacherRole = a.Teachers.FirstOrDefault()?.Role ?? TeacherRole.Coach });
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Edit(AwardForm f)
    {
        var a = f.Id == 0 ? new Award() : await Db.Awards.Include(x => x.Teachers).Include(x => x.Students).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == f.Id);
        if (a == null) return NotFound();
        if (f.Id != 0 && !await Access.CanEditAsync(a, a.DepartmentId, a.Status) || !await Access.CanCreateAsync(f.DepartmentId)) return Forbid();
        Version(a, f.RowVersion); await ValidateLinksAsync(f.DepartmentId, f.AcademicYearId, f.SubjectGroupId, f.TeacherIds, f.StudentIds);
        if (f.ActivityId != null && !await (await Access.ActivitiesAsync()).AnyAsync(x => x.Id == f.ActivityId)) ModelState.AddModelError("ActivityId", "ไม่พบกิจกรรม");
        if (!ModelState.IsValid) { await LookupsAsync(forAuthoring: true); return View(f); }
        a.AwardName = f.AwardName; a.CompetitionName = f.CompetitionName; a.AwardLevel = f.AwardLevel; a.AwardRank = f.AwardRank; a.Organizer = f.Organizer;
        a.AwardDate = f.AwardDate; a.AcademicYearId = f.AcademicYearId; a.DepartmentId = f.DepartmentId; a.SubjectGroupId = f.SubjectGroupId;
        a.ActivityId = f.ActivityId; a.Description = f.Description; a.IsPublic = f.IsPublic;
        foreach (var x in a.Teachers.Where(x => !f.TeacherIds.Contains(x.TeacherId)).ToList()) Db.Remove(x);
        foreach (var x in a.Students.Where(x => !f.StudentIds.Contains(x.StudentId)).ToList()) Db.Remove(x);
        foreach (var id in f.TeacherIds.Distinct()) { var link = a.Teachers.FirstOrDefault(x => x.TeacherId == id); if (link == null) a.Teachers.Add(new AwardTeacher { TeacherId = id, Role = f.TeacherRole }); else link.Role = f.TeacherRole; }
        foreach (var id in f.StudentIds.Distinct()) if (!a.Students.Any(x => x.StudentId == id)) a.Students.Add(new AwardStudent { StudentId = id });
        if (f.Id == 0) Db.Add(a);
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "ข้อมูลเปลี่ยนแปลง กรุณาเปิดใหม่"); await LookupsAsync(forAuthoring: true); return View(f); }
        return RedirectToAction(nameof(Details), new { id = a.Id });
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Transition(int id, WorkflowStatus status, string rowVersion)
    {
        var a = await Db.Awards.SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!Version(a, rowVersion)) return Conflict();
        if (!await Access.CanTransitionAsync(a, a.DepartmentId, a.Status, status)) return Forbid();
        a.Status = status; if (status == WorkflowStatus.Published) a.PublishedDate = DateTime.UtcNow;
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, Authorize(Policy = "Editor"), RequestSizeLimit(57671680)]
    public async Task<IActionResult> Upload(int id, List<IFormFile> files, int? studentId)
    {
        if (!ModelState.IsValid) return BadRequest();
        var a = await Db.Awards.SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!await Access.CanEditAsync(a, a.DepartmentId, a.Status)) return Forbid();
        if (files.Count == 0 || files.Count > 20 || files.Sum(f => f.Length) > 52428800) return BadRequest();
        if (studentId != null && !await Db.Set<AwardStudent>().AnyAsync(x=>x.AwardId==id&&x.StudentId==studentId)) return BadRequest("นักเรียนไม่ได้อยู่ในรายชื่อผู้รับรางวัล");
        try { foreach (var file in files) a.Certificates.Add(new AwardCertificate { StudentId = studentId, File = await storage.StoreAsync(file, Access.UserId) }); await Db.SaveChangesAsync(); }
        catch (InvalidDataException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> AssignCertificate(int id, Guid fileId, int? studentId, string rowVersion)
    {
        if (!ModelState.IsValid) return BadRequest();
        var a = await Db.Awards.SingleOrDefaultAsync(x=>x.Id==id); if (a==null) return NotFound();
        if (!await Access.CanEditAsync(a,a.DepartmentId,a.Status)) return Forbid();
        if (!Version(a,rowVersion)) return Conflict();
        var certificate=await Db.Set<AwardCertificate>().SingleOrDefaultAsync(x=>x.AwardId==id&&x.FileId==fileId); if(certificate==null)return NotFound();
        if(studentId!=null&&!await Db.Set<AwardStudent>().AnyAsync(x=>x.AwardId==id&&x.StudentId==studentId))return BadRequest("นักเรียนไม่ได้อยู่ในรายชื่อผู้รับรางวัล");
        certificate.StudentId=studentId;
        Db.Entry(a).Property(x=>x.Status).IsModified=true;
        try { await Db.SaveChangesAsync(); } catch(DbUpdateConcurrencyException) { return Conflict(); }
        TempData["Success"]="บันทึกผู้รับเกียรติบัตรแล้ว";return RedirectToAction(nameof(Details),new{id});
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Delete(int id, string rowVersion)
    {
        var a = await Db.Awards.SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!await Access.CanEditAsync(a, a.DepartmentId, a.Status)) return Forbid();
        if (!Version(a, rowVersion)) return Conflict(); Db.Remove(a); await Db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
}
