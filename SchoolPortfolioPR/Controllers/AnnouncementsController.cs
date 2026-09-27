using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

namespace SchoolPortfolioPR.Controllers;

[Authorize(Policy = "Publisher")]
public class AnnouncementsController(ApplicationDbContext db, AccessService access, IFileStorage storage, AnnouncementSchedule schedule) : EditorController(db, access)
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Today = schedule.Today;
        return View(await Db.Announcements.AsNoTracking().OrderBy(x => x.SortOrder).ThenByDescending(x => x.Id).ToListAsync());
    }

    [AllowAnonymous, HttpGet("announcements/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Details(int id)
    {
        var query = Db.Announcements.AsNoTracking();
        if (!Access.IsPublisher) query = query.Where(AnnouncementSchedule.VisibleOn(schedule.Today));
        var item = await query.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        ViewBag.Today = schedule.Today;
        return View(item);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        var f = new AnnouncementForm { StartDate = schedule.Today, EndDate = schedule.Today.AddDays(30) };
        if (id.HasValue)
        {
            var item = await Db.Announcements.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();
            f = new() { Id = item.Id, RowVersion = Convert.ToBase64String(item.RowVersion), Title = item.Title, Summary = item.Summary,
                Body = item.Body, Category = item.Category, StartDate = item.StartDate, EndDate = item.EndDate, IsEnabled = item.IsEnabled,
                Size = item.Size, Tone = item.Tone, SortOrder = item.SortOrder, CurrentImageId = item.ImageId };
        }
        await ChoicesAsync(); return View(f);
    }

    private async Task ChoicesAsync()
    {
        // Publishers can already read all activity media. A selected image is copied to independent board ownership.
        ViewBag.Images = await Db.Set<ActivityImage>().AsNoTracking().OrderByDescending(x => x.Activity.ActivityDate).ThenBy(x => x.SortOrder)
            .Where(x => x.File.ContentType.StartsWith("image/"))
            .Select(x => new AnnouncementImageChoice(x.FileId, x.File.OriginalFileName, x.Activity.Title)).Take(200).ToListAsync();
    }

    [HttpPost, RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Edit(AnnouncementForm f)
    {
        var item = f.Id == 0 ? new Announcement() : await Db.Announcements.SingleOrDefaultAsync(x => x.Id == f.Id);
        if (item == null) return NotFound();
        if (!Version(item, f.RowVersion)) return Conflict("ข่าวถูกแก้ไขแล้ว กรุณาเปิดหน้าแก้ไขใหม่");
        f.CurrentImageId = item.ImageId;
        FileObject? source = null;
        if (f.SourceImageId.HasValue)
        {
            source = await Db.Set<ActivityImage>().Where(x => x.FileId == f.SourceImageId && x.File.ContentType.StartsWith("image/"))
                .Select(x => x.File).FirstOrDefaultAsync();
            if (source == null) ModelState.AddModelError(nameof(f.SourceImageId), "ไม่พบภาพที่เลือก กรุณาเลือกใหม่");
        }
        if (!ModelState.IsValid) { await ChoicesAsync(); return View(f); }
        FileObject? replacement = null;
        try
        {
            if (f.Upload != null)
            {
                if (!f.Upload.ContentType.StartsWith("image/")) throw new InvalidDataException("กรุณาเลือกภาพ JPG, PNG หรือ WebP");
                replacement = await storage.StoreAsync(f.Upload, Access.UserId);
            }
            else if (source != null)
            {
                using var stream = new MemoryStream(source.FileData);
                replacement = await storage.StoreAsync(new FormFile(stream, 0, stream.Length, "file", source.OriginalFileName) {
                    Headers = new HeaderDictionary(), ContentType = source.ContentType }, Access.UserId);
            }
        }
        catch (InvalidDataException ex) { ModelState.AddModelError(nameof(f.Upload), ex.Message); await ChoicesAsync(); return View(f); }
        if ((replacement != null || f.RemoveImage) && item.ImageId.HasValue)
        {
            var old = await Db.FileObjects.SingleOrDefaultAsync(x => x.FileId == item.ImageId);
            if (old != null) old.IsDeleted = true;
        }
        if (replacement != null) item.ImageId = replacement.FileId;
        else if (f.RemoveImage) item.ImageId = null;
        item.Title = f.Title; item.Summary = f.Summary; item.Body = f.Body ?? ""; item.Category = f.Category;
        item.StartDate = f.StartDate.Date; item.EndDate = f.EndDate.Date; item.Size = f.Size; item.Tone = f.Tone;
        item.SortOrder = f.SortOrder; item.IsEnabled = f.IsEnabled;
        if (item.Id == 0) Db.Add(item);
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict("ข่าวถูกแก้ไขแล้ว กรุณาเปิดหน้าแก้ไขใหม่"); }
        TempData["Success"] = "บันทึกข่าวแล้ว ระบบจะแสดงตามวันที่และขนาดกรอบที่เลือก";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string rowVersion)
    {
        var item = await Db.Announcements.SingleOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        if (!Version(item, rowVersion)) return Conflict();
        Db.Remove(item);
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        return RedirectToAction(nameof(Index));
    }
}
