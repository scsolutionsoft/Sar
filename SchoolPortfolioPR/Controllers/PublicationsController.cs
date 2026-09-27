using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
public class PublicationsController(ApplicationDbContext db, AccessService access, PublicationService publications) : EditorController(db, access)
{
    public async Task<IActionResult> Index()
    {
        var publications = await Access.Publications().OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync();
        var ids = publications.Select(x => x.Id).ToArray();
        var files = await Db.Set<PublicationFile>().AsNoTracking().Where(x => ids.Contains(x.MonthlyPublicationId)).OrderByDescending(x => x.GeneratedAt).Select(x => new { x.MonthlyPublicationId, x.FileId }).ToListAsync();
        ViewBag.PdfFiles = files.GroupBy(x => x.MonthlyPublicationId).ToDictionary(x => x.Key, x => x.First().FileId);
        return View(publications);
    }
    [Authorize(Policy = "Publisher")]
    public async Task<IActionResult> Edit(int? id, int? year, int? month)
    {
        var f = new PublicationForm { Year = year ?? DateTime.Today.Year, Month = month ?? DateTime.Today.Month };
        if (f.Year is < 2000 or > 2400 || f.Month is < 1 or > 12) return BadRequest();
        if (id.HasValue) {
            var p = await Db.Publications.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id); if (p == null) return NotFound();
            f = new PublicationForm { Id = p.Id, RowVersion = Convert.ToBase64String(p.RowVersion), Title = p.Title, Year = p.Year, Month = p.Month, DirectorMessage = p.DirectorMessage, IsPublic = p.IsPublic,
                SelectedItems = p.Items.OrderBy(x => x.SortOrder).Select(x => x.ActivityId.HasValue ? "a:" + x.ActivityId : "w:" + x.AwardId).ToArray() };
        }
        await ChoicesAsync(f); return View(f);
    }
    private async Task ChoicesAsync(PublicationForm f)
    {
        var choices = await publications.ChoicesAsync(f.Year, f.Month);
        ViewBag.Choices = choices.OrderBy(x => { var i = Array.IndexOf(f.SelectedItems, x.Key); return i < 0 ? int.MaxValue : i; }).Select(x => x with { Selected = f.SelectedItems.Contains(x.Key) }).ToList();
    }
    [HttpPost, Authorize(Policy = "Publisher")]
    public async Task<IActionResult> Edit(PublicationForm f)
    {
        if (!ModelState.IsValid) { if (f.Year is >= 2000 and <= 2400 && f.Month is >= 1 and <= 12) await ChoicesAsync(f); else ViewBag.Choices = new List<PublicationChoice>(); return View(f); }
        var p = f.Id == 0 ? new MonthlyPublication() : await Db.Publications.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == f.Id);
        if (p == null) return NotFound(); Version(p, f.RowVersion);
        if (await Db.Publications.AnyAsync(x => x.Id != f.Id && x.Year == f.Year && x.Month == f.Month)) ModelState.AddModelError("", "มีวารสารเดือนนี้แล้ว");
        var choices = await publications.ChoicesAsync(f.Year, f.Month);
        if (f.SelectedItems.Any(i => !choices.Any(c => c.Key == i)) || f.SelectedItems.Distinct().Count() != f.SelectedItems.Length) ModelState.AddModelError("", "รายการบางรายการไม่พร้อมเผยแพร่ กรุณาเลือกใหม่");
        if (!ModelState.IsValid) { await ChoicesAsync(f); return View(f); }
        p.Title = f.Title; p.Year = f.Year; p.Month = f.Month; p.DirectorMessage = f.DirectorMessage;
        // Editing content invalidates the generated PDF and unpublishes until generation succeeds.
        p.IsPublic = false;
        var oldFiles = await Db.Set<PublicationFile>().Where(x => x.MonthlyPublicationId == p.Id).Select(x => x.File).ToListAsync(); foreach (var file in oldFiles) file.IsDeleted = true;
        Db.RemoveRange(p.Items); p.Items.Clear(); int order = 0;
        foreach (var key in f.SelectedItems) { var c = choices.Single(x => x.Key == key); var id = int.Parse(key[2..]); p.Items.Add(new PublicationItem { ActivityId = key.StartsWith("a:") ? id : null, AwardId = key.StartsWith("w:") ? id : null, Section = c.Section, SortOrder = order++ }); }
        if (f.Id == 0) Db.Add(p);
        try { await Db.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        TempData["Success"] = "บันทึกแล้ว กรุณาสร้าง PDF และเผยแพร่"; return RedirectToAction(nameof(Manage), new { id = p.Id });
    }
    [Authorize(Policy = "Publisher")]
    public async Task<IActionResult> Manage(int id)
    {
        var p = await Db.Publications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); return p == null ? NotFound() : View(p);
    }
    [HttpPost, Authorize(Policy = "Publisher")]
    public async Task<IActionResult> Generate(int id, bool publish, string rowVersion)
    {
        var p = await Db.Publications.SingleOrDefaultAsync(x => x.Id == id); if (p == null) return NotFound(); if (!Version(p, rowVersion)) return Conflict();
        await publications.GenerateAsync(p, Access.UserId); p.IsPublic = publish; await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Viewer), new { year = p.Year + 543, month = p.Month });
    }
    [HttpGet("publications/{year:int}/{month:int}")]
    public async Task<IActionResult> Viewer(int year, int month)
    {
        var ad = year > 2400 ? year - 543 : year;
        var p = await Access.Publications().SingleOrDefaultAsync(x => x.Year == ad && x.Month == month); if (p == null) return NotFound();
        ViewBag.PdfId = await Db.Set<PublicationFile>().Where(x => x.MonthlyPublicationId == p.Id && !x.File.IsDeleted).OrderByDescending(x => x.GeneratedAt).Select(x => (Guid?)x.FileId).FirstOrDefaultAsync();
        return View(p);
    }
    public async Task<IActionResult> Qr(int id)
    {
        var p = await Access.Publications().SingleOrDefaultAsync(x => x.Id == id && x.IsPublic); if (p == null) return NotFound();
        var url = Url.Action(nameof(Viewer), "Publications", new { year = p.Year + 543, month = p.Month }, Request.Scheme)!;
        using var data = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.Q); using var png = new PngByteQRCode(data); return File(png.GetGraphic(8), "image/png");
    }
    [HttpPost, Authorize(Policy = "Publisher")]
    public async Task<IActionResult> Delete(int id, string rowVersion)
    {
        var p = await Db.Publications.SingleOrDefaultAsync(x => x.Id == id); if (p == null) return NotFound(); if (!Version(p, rowVersion)) return Conflict(); Db.Remove(p); await Db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
}
