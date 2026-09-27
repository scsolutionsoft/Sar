using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

namespace SchoolPortfolioPR.Controllers;

[Authorize]
public class GalleryController(ApplicationDbContext db, AccessService access, IFileStorage storage) : EditorController(db, access)
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var activities = await Access.ActivitiesAsync();
        if (!string.IsNullOrWhiteSpace(q)) activities = activities.Where(x => x.Title.Contains(q) || x.ActivityCode.Contains(q) || x.Images.Any(i => i.Caption != null && i.Caption.Contains(q)) || Db.ActivityImageLinks.Any(i => i.ActivityId == x.Id && i.Caption != null && i.Caption.Contains(q)));
        page = Math.Max(1, page); ViewBag.Search = q;
        var rows = await activities.OrderByDescending(x => x.ActivityDate).ThenByDescending(x => x.Id).Skip((page - 1) * 20).Take(20)
            .Select(x => new GalleryAlbumView(x.Id, x.Title, x.ActivityDate, x.Department.Name, x.CoverImageId, x.Images.Count, Db.ActivityImageLinks.Count(i => i.ActivityId == x.Id))).ToListAsync();
        return View(new PageView<GalleryAlbumView>(rows, page, await activities.CountAsync()));
    }

    [Authorize(Policy = "Editor")]
    public async Task<IActionResult> Entry(int activityId, Guid? fileId, int linkId = 0, GallerySource source = GallerySource.Upload)
    {
        var activity = await Db.Activities.SingleOrDefaultAsync(x => x.Id == activityId); if (activity == null) return NotFound();
        if (!await Access.CanEditAsync(activity, activity.DepartmentId, activity.Status)) return Forbid();
        if (!Enum.IsDefined(source) || fileId != null && linkId != 0) return BadRequest();
        ViewBag.ActivityTitle = activity.Title;
        var form = new GalleryEntryForm { ActivityId = activityId, Source = source, FileId = fileId, LinkId = linkId, RowVersion = Convert.ToBase64String(activity.RowVersion) };
        if (fileId != null) {
            var image = await Db.Set<ActivityImage>().SingleOrDefaultAsync(x => x.ActivityId == activityId && x.FileId == fileId); if (image == null) return NotFound();
            form.Source = GallerySource.Upload; form.Caption = image.Caption;
        }
        if (linkId != 0) {
            var link = await Db.ActivityImageLinks.SingleOrDefaultAsync(x => x.Id == linkId && x.ActivityId == activityId); if (link == null) return NotFound();
            form.Source = GallerySource.External; form.Caption = link.Caption; form.ExternalUrl = link.Url;
        }
        return View(form);
    }

    [HttpPost, Authorize(Policy = "Editor"), RequestSizeLimit(57671680)]
    public async Task<IActionResult> Entry(GalleryEntryForm form)
    {
        var activity = await Db.Activities.Include(x => x.Images).SingleOrDefaultAsync(x => x.Id == form.ActivityId); if (activity == null) return NotFound();
        if (!await Access.CanEditAsync(activity, activity.DepartmentId, activity.Status)) return Forbid();
        ViewBag.ActivityTitle = activity.Title;
        if (!ModelState.IsValid) return View(form);
        if (!Version(activity, form.RowVersion)) return Conflict("ข้อมูลกิจกรรมเปลี่ยนแปลง กรุณาเปิดหน้าใหม่ก่อนบันทึก");
        var caption = string.IsNullOrWhiteSpace(form.Caption) ? null : form.Caption.Trim();
        if (form.FileId != null) {
            var image = activity.Images.SingleOrDefault(x => x.FileId == form.FileId); if (image == null) return NotFound();
            image.Caption = caption;
        }
        else if (form.Source == GallerySource.External) {
            var link = form.LinkId == 0 ? new ActivityImageLink { ActivityId = activity.Id } : await Db.ActivityImageLinks.SingleOrDefaultAsync(x => x.Id == form.LinkId && x.ActivityId == activity.Id);
            if (link == null) return NotFound();
            link.Url = form.ExternalUrl!.Trim(); link.Caption = caption;
            if (form.LinkId == 0) Db.Add(link);
        }
        else {
            try {
                var order = activity.Images.Count == 0 ? 0 : activity.Images.Max(x => x.SortOrder) + 1;
                foreach (var file in form.Files) {
                    var stored = await storage.StoreAsync(file, Access.UserId);
                    activity.Images.Add(new ActivityImage { File = stored, Caption = caption, SortOrder = order++ }); activity.CoverImageId ??= stored.FileId;
                }
            } catch (InvalidDataException ex) { ModelState.AddModelError(nameof(form.Files), ex.Message); return View(form); }
        }
        Db.Entry(activity).Property(x => x.Status).IsModified = true;
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict("ข้อมูลกิจกรรมเปลี่ยนแปลง กรุณาเปิดหน้าใหม่ก่อนบันทึก"); }
        TempData["Success"] = "บันทึกคลังภาพกิจกรรมแล้ว";
        return RedirectToAction("Details", "Activities", new { id = activity.Id }, "activityGallery");
    }

    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> DeleteLink(int activityId, int linkId, string rowVersion)
    {
        var activity = await Db.Activities.SingleOrDefaultAsync(x => x.Id == activityId); if (activity == null) return NotFound();
        if (!await Access.CanEditAsync(activity, activity.DepartmentId, activity.Status)) return Forbid();
        if (!Version(activity, rowVersion)) return Conflict();
        var link = await Db.ActivityImageLinks.SingleOrDefaultAsync(x => x.Id == linkId && x.ActivityId == activityId); if (link == null) return NotFound();
        Db.Remove(link); Db.Entry(activity).Property(x => x.Status).IsModified = true;
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        TempData["Success"] = "นำลิงก์ออกจากคลังภาพแล้ว";
        return RedirectToAction("Details", "Activities", new { id = activityId }, "activityGallery");
    }
}
