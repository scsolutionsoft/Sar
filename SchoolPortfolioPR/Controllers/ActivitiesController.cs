using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
[Authorize]
public class ActivitiesController(ApplicationDbContext db, AccessService access, IFileStorage storage, IConfiguration config) : EditorController(db, access)
{
    public async Task<IActionResult> Index(string? q, int page = 1, WorkflowStatus? status = null, bool mine = false)
    {
        var query = (await Access.ActivitiesAsync()).Include(x => x.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Title.Contains(q) || x.ActivityCode.Contains(q));
        if (!ModelState.IsValid || status.HasValue && !Enum.IsDefined(status.Value)) return BadRequest();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (mine) query = query.Where(x => x.CreatedBy == Access.UserId);
        ViewBag.Status = status; ViewBag.Mine = mine; ViewBag.CanCreate = await Access.CanAuthorAsync();
        page = Math.Max(1, page); ViewBag.Search = q;
        return View(new PageView<Activity>(await query.OrderByDescending(x => x.ActivityDate).Skip((page - 1) * 20).Take(20).ToListAsync(), page, await query.CountAsync()));
    }
    [AllowAnonymous, HttpGet("activity/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var a = await Db.Activities.AsNoTracking().Include(x => x.Department).Include(x => x.Category).Include(x => x.AcademicYear)
            .Include(x => x.Teachers).ThenInclude(x => x.Teacher).Include(x => x.Students).ThenInclude(x => x.Student)
            .Include(x => x.Tags).ThenInclude(x => x.Tag).Include(x => x.Awards).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id);
        if (a == null || !await Access.CanReadAsync(a, a.DepartmentId, a.Status, a.IsPublic)) return NotFound();
        ViewBag.Images = await Db.Set<ActivityImage>().Where(x => x.ActivityId == id && !x.File.IsDeleted).OrderBy(x => x.SortOrder).Select(x => new FileInfoView(x.FileId, x.File.OriginalFileName, x.File.ContentType, x.SortOrder)).ToListAsync();
        ViewBag.Files = await Db.Set<ActivityFile>().Where(x => x.ActivityId == id && !x.File.IsDeleted).Select(x => new FileInfoView(x.FileId, x.File.OriginalFileName, x.File.ContentType, 0)).ToListAsync();
        ViewBag.CanEdit = User.Identity?.IsAuthenticated == true && await Access.CanEditAsync(a, a.DepartmentId, a.Status);
        var gallery = await Db.Set<ActivityImage>().AsNoTracking().Where(x => x.ActivityId == id).OrderBy(x => x.SortOrder)
            .Select(x => new GalleryItemView(x.FileId, 0, x.File.OriginalFileName, x.Caption, null)).ToListAsync();
        gallery.AddRange(await Db.ActivityImageLinks.AsNoTracking().Where(x => x.ActivityId == id).OrderBy(x => x.Id)
            .Select(x => new GalleryItemView(null, x.Id, "ภาพจากคลาวด์", x.Caption, x.Url)).ToListAsync());
        ViewBag.Gallery = new ActivityGalleryView(id, Convert.ToBase64String(a.RowVersion), (bool)ViewBag.CanEdit, gallery);
        var transitions = new List<WorkflowStatus>();
        foreach (var s in Enum.GetValues<WorkflowStatus>()) if (await Access.CanTransitionAsync(a, a.DepartmentId, a.Status, s)) transitions.Add(s);
        ViewBag.Transitions = transitions; return View(a);
    }
    [Authorize(Policy = "Editor")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null && !await Access.CanAuthorAsync()) return Forbid();
        await LookupsAsync(forAuthoring: true);
        if (id == null) return View(new ActivityForm { DepartmentId = await Access.DepartmentAsync() ?? 0 });
        var a = await Db.Activities.Include(x => x.Teachers).Include(x => x.Students).Include(x => x.Tags).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        if (!await Access.CanEditAsync(a, a.DepartmentId, a.Status)) return Forbid();
        return View(new ActivityForm { Id = a.Id, RowVersion = Convert.ToBase64String(a.RowVersion), ActivityCode = a.ActivityCode, Title = a.Title,
            ShortDescription = a.ShortDescription, Description = a.Description, ActivityDate = a.ActivityDate, EndDate = a.EndDate, Location = a.Location, Organizer = a.Organizer,
            CategoryId = a.CategoryId, DepartmentId = a.DepartmentId, SubjectGroupId = a.SubjectGroupId, AcademicYearId = a.AcademicYearId, FiscalYear = a.FiscalYear,
            Slug = a.Slug, IsProject = a.IsProject, IsFeatured = a.IsFeatured, IsPublic = a.IsPublic,
            TeacherIds = a.Teachers.Select(x => x.TeacherId).ToArray(), StudentIds = a.Students.Select(x => x.StudentId).ToArray(), TagIds = a.Tags.Select(x => x.TagId).ToArray(), TeacherRole = a.Teachers.FirstOrDefault()?.Role ?? TeacherRole.Coach });
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Edit(ActivityForm f)
    {
        var a = f.Id == 0 ? new Activity() : await Db.Activities.Include(x => x.Teachers).Include(x => x.Students).Include(x => x.Tags).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == f.Id);
        if (a == null) return NotFound();
        if (f.Id != 0 && !await Access.CanEditAsync(a, a.DepartmentId, a.Status) || !await Access.CanCreateAsync(f.DepartmentId)) return Forbid();
        Version(a, f.RowVersion);
        await ValidateLinksAsync(f.DepartmentId, f.AcademicYearId, f.SubjectGroupId, f.TeacherIds, f.StudentIds);
        if (!Enum.IsDefined(f.TeacherRole)) ModelState.AddModelError("TeacherRole", "บทบาทไม่ถูกต้อง");
        if (!await Db.Set<ActivityCategory>().AnyAsync(x => x.Id == f.CategoryId)) ModelState.AddModelError("CategoryId", "ไม่พบประเภทกิจกรรม");
        if (await Db.Set<Tag>().CountAsync(x => f.TagIds.Contains(x.Id)) != f.TagIds.Distinct().Count()) ModelState.AddModelError("TagIds", "ไม่พบแท็ก");
        f.Slug = a.Slug;
        if (await Db.Activities.AnyAsync(x => x.Id != f.Id && x.ActivityCode == f.ActivityCode)) ModelState.AddModelError(nameof(f.ActivityCode), "รหัสกิจกรรมซ้ำ");
        if (!ModelState.IsValid) { await LookupsAsync(forAuthoring: true); return View(f); }
        a.ActivityCode = f.ActivityCode; a.Title = f.Title; a.ShortDescription = f.ShortDescription; a.Description = f.Description;
        a.ActivityDate = f.ActivityDate; a.EndDate = f.EndDate; a.Location = f.Location; a.Organizer = f.Organizer; a.CategoryId = f.CategoryId;
        a.DepartmentId = f.DepartmentId; a.SubjectGroupId = f.SubjectGroupId; a.AcademicYearId = f.AcademicYearId; a.FiscalYear = f.FiscalYear;
        if (f.Id == 0) {
            do { a.Slug = ActivitySlug.Create(f.ActivityCode); } while (await Db.Activities.AnyAsync(x => x.Slug == a.Slug));
        }
        a.IsProject = f.IsProject; a.IsFeatured = f.IsFeatured; a.IsPublic = f.IsPublic;
        foreach (var x in a.Teachers.Where(x => !f.TeacherIds.Contains(x.TeacherId)).ToList()) Db.Remove(x);
        foreach (var x in a.Students.Where(x => !f.StudentIds.Contains(x.StudentId)).ToList()) Db.Remove(x);
        foreach (var x in a.Tags.Where(x => !f.TagIds.Contains(x.TagId)).ToList()) Db.Remove(x);
        foreach (var id in f.TeacherIds.Distinct()) { var link = a.Teachers.FirstOrDefault(x => x.TeacherId == id); if (link == null) a.Teachers.Add(new ActivityTeacher { TeacherId = id, Role = f.TeacherRole }); else link.Role = f.TeacherRole; }
        foreach (var id in f.StudentIds.Distinct()) if (!a.Students.Any(x => x.StudentId == id)) a.Students.Add(new ActivityStudent { StudentId = id });
        foreach (var id in f.TagIds.Distinct()) if (!a.Tags.Any(x => x.TagId == id)) a.Tags.Add(new ActivityTag { TagId = id });
        if (f.Id == 0) Db.Add(a);
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "ข้อมูลเปลี่ยนแปลง กรุณาเปิดใหม่"); await LookupsAsync(forAuthoring: true); return View(f); }
        TempData["Success"] = "บันทึกกิจกรรมแล้ว"; return RedirectToAction(nameof(Details), new { id = a.Id });
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Transition(int id, WorkflowStatus status, string rowVersion)
    {
        var a = await Db.Activities.SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!Version(a, rowVersion)) return Conflict("ข้อมูลเปลี่ยนแปลง กรุณาโหลดใหม่");
        if (!await Access.CanTransitionAsync(a, a.DepartmentId, a.Status, status)) return Forbid();
        a.Status = status; if (status == WorkflowStatus.Published) a.PublishedDate = DateTime.UtcNow;
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, Authorize(Policy = "Editor"), RequestSizeLimit(57671680)]
    public async Task<IActionResult> Upload(int id, List<IFormFile> files)
    {
        var a = await Db.Activities.Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!await Access.CanEditAsync(a, a.DepartmentId, a.Status)) return Forbid();
        if (files.Count == 0 || files.Count > 20 || files.Sum(x => x.Length) > config.GetValue<long>("Uploads:MaxBatchBytes")) return BadRequest("สูงสุด 20 ไฟล์ / 50 MB ต่อครั้ง");
        try {
            int order = a.Images.Count == 0 ? 0 : a.Images.Max(x => x.SortOrder) + 1;
            foreach (var file in files) {
                var stored = await storage.StoreAsync(file, Access.UserId);
                if (stored.ContentType.StartsWith("image/")) { a.Images.Add(new ActivityImage { File = stored, SortOrder = order++ }); a.CoverImageId ??= stored.FileId; }
                else a.Files.Add(new ActivityFile { File = stored });
            }
            await Db.SaveChangesAsync(); TempData["Success"] = "อัปโหลดเรียบร้อย";
        } catch (InvalidDataException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Images(int id, Guid cover, Guid[] order)
    {
        var a = await Db.Activities.Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!await Access.CanEditAsync(a, a.DepartmentId, a.Status)) return Forbid();
        if (!a.Images.Any(x => x.FileId == cover) || order.Length != a.Images.Count || order.Distinct().Count() != order.Length || order.Any(i => !a.Images.Any(x => x.FileId == i))) return BadRequest();
        a.CoverImageId = cover; for (int i = 0; i < order.Length; i++) a.Images.Single(x => x.FileId == order[i]).SortOrder = i;
        await Db.SaveChangesAsync(); return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Delete(int id, string rowVersion)
    {
        var a = await Db.Activities.SingleOrDefaultAsync(x => x.Id == id); if (a == null) return NotFound();
        if (!await Access.CanEditAsync(a, a.DepartmentId, a.Status)) return Forbid();
        if (!Version(a, rowVersion)) return Conflict(); Db.Remove(a); await Db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
}
