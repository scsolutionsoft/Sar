using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

namespace SchoolPortfolioPR.Controllers;

[Authorize(Policy = "Admin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BrandingController(ApplicationDbContext db, IFileStorage storage, AccessService access) : Controller
{
    public async Task<IActionResult> Index()
    {
        var settings = await db.Branding.AsNoTracking().SingleAsync(x => x.Id == 1);
        return View(new BrandingForm { ProgramName = settings.ProgramName, CurrentLogoId = settings.LogoFileId, RowVersion = Convert.ToBase64String(settings.RowVersion) });
    }

    [HttpPost, RequestSizeLimit(3145728)]
    public async Task<IActionResult> Index(BrandingForm form)
    {
        var settings = await db.Branding.Include(x => x.LogoFile).SingleAsync(x => x.Id == 1);
        form.CurrentLogoId = settings.LogoFileId;
        form.ProgramName = form.ProgramName?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(form.ProgramName)) ModelState.AddModelError(nameof(form.ProgramName), "กรุณาระบุชื่อโปรแกรม");
        if (form.RemoveLogo && form.Logo != null) ModelState.AddModelError(nameof(form.Logo), "เลือกอัปโหลดโลโก้ใหม่หรือนำโลโก้ออกอย่างใดอย่างหนึ่ง");
        if (form.Logo != null && (form.Logo.Length > 2097152 || !new[] { ".png", ".jpg", ".jpeg", ".webp" }.Contains(Path.GetExtension(form.Logo.FileName).ToLowerInvariant())))
            ModelState.AddModelError(nameof(form.Logo), "ใช้รูป PNG, JPG หรือ WebP ขนาดไม่เกิน 2 MB");
        if (!ModelState.IsValid) return View(form);
        byte[] version;
        try { version = Convert.FromBase64String(form.RowVersion); } catch (FormatException) { return BadRequest(); }
        if (!version.SequenceEqual(settings.RowVersion)) return Conflict("มีผู้แก้ไขการตั้งค่าแล้ว กรุณาเปิดหน้าใหม่ก่อนบันทึก");
        if (form.Logo != null)
        {
            try {
                var logo = await storage.StoreAsync(form.Logo, access.UserId);
                if (settings.LogoFile != null) settings.LogoFile.IsDeleted = true;
                settings.LogoFile = logo;
            } catch (InvalidDataException ex) { ModelState.AddModelError(nameof(form.Logo), ex.Message); return View(form); }
        }
        else if (form.RemoveLogo)
        {
            if (settings.LogoFile != null) settings.LogoFile.IsDeleted = true;
            settings.LogoFile = null; settings.LogoFileId = null;
        }
        settings.ProgramName = form.ProgramName;
        // Also advance the version for requests that keep the same values.
        db.Entry(settings).Property(x => x.ProgramName).IsModified = true;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("มีผู้แก้ไขการตั้งค่าแล้ว กรุณาเปิดหน้าใหม่ก่อนบันทึก"); }
        TempData["Success"] = "บันทึกชื่อและโลโก้โปรแกรมแล้ว";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous, HttpGet("Branding/Logo/{id:guid}")]
    public async Task<IActionResult> Logo(Guid id)
    {
        var file = await db.Branding.AsNoTracking().Where(x => x.Id == 1 && x.LogoFileId == id)
            .Select(x => x.LogoFile).SingleOrDefaultAsync();
        return file == null ? NotFound() : File(file.FileData, file.ContentType);
    }
}
