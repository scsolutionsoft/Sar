using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;

[Authorize]
public class DocumentsController(ApplicationDbContext db, AccessService access, IFileStorage storage, ExportService export) : EditorController(db, access)
{
    public async Task<IActionResult> Index(string? q, string? category, int page = 1)
    {
        var all = Db.PersonalDocuments.AsNoTracking().Where(x => x.OwnerId == Access.UserId);
        ViewBag.Categories = await all.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();
        if (!string.IsNullOrWhiteSpace(q)) all = all.Where(x => x.Title.Contains(q) || x.Description.Contains(q));
        if (!string.IsNullOrWhiteSpace(category)) all = all.Where(x => x.Category == category);
        page = Math.Max(1,page); ViewBag.Search = q; ViewBag.Category = category;
        ViewBag.Portfolios = Access.IsStudent ? await Db.AdmissionPortfolios.AsNoTracking().Where(x => x.OwnerId == Access.UserId && x.PdfFileId != null).OrderByDescending(x => x.GeneratedAt).ToListAsync() : new List<AdmissionPortfolio>();
        ViewBag.IsStudent = Access.IsStudent;
        return View(new PageView<PersonalDocument>(await all.OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.Id).Skip((page-1)*20).Take(20).ToListAsync(),page,await all.CountAsync()));
    }
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return View(new DocumentForm());
        var d = await Db.PersonalDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == Access.UserId);
        return d == null ? NotFound() : View(new DocumentForm { Id = d.Id, RowVersion = Convert.ToBase64String(d.RowVersion), Title = d.Title, Category = d.Category, Description = d.Description, DocumentDate = d.DocumentDate });
    }
    [HttpPost, RequestSizeLimit(12*1024*1024)]
    public async Task<IActionResult> Edit(DocumentForm f)
    {
        var d = f.Id == 0 ? new PersonalDocument { OwnerId = Access.UserId } : await Db.PersonalDocuments.SingleOrDefaultAsync(x => x.Id == f.Id && x.OwnerId == Access.UserId);
        if (d == null) return NotFound(); if (!Version(d,f.RowVersion)) return Conflict();
        if (f.DocumentDate.Year is < 1900 or > 2400) ModelState.AddModelError("DocumentDate","กรุณาระบุวันที่ ค.ศ.");
        if (f.Id == 0 && f.Upload == null && string.IsNullOrWhiteSpace(f.ReportText)) ModelState.AddModelError("","เลือก PDF หรือเขียนเนื้อหาเพื่อสร้างรายงาน");
        if (f.Upload != null && !string.IsNullOrWhiteSpace(f.ReportText)) ModelState.AddModelError("","เลือกอัปโหลดหรือสร้างรายงานอย่างใดอย่างหนึ่ง");
        if (f.Id != 0 && (f.Upload != null || !string.IsNullOrWhiteSpace(f.ReportText))) ModelState.AddModelError("","หากต้องการเปลี่ยนไฟล์ ให้เพิ่มเอกสารใหม่เพื่อรักษาหลักฐานเดิม");
        if (!ModelState.IsValid) return View(f);
        if (f.Id == 0)
        {
            try {
                FileObject file;
                if (f.Upload != null) {
                    if (f.Upload.ContentType != "application/pdf" || Path.GetExtension(f.Upload.FileName).ToLowerInvariant() != ".pdf") throw new InvalidDataException("คลังนี้รับเฉพาะ PDF");
                    file = await storage.StoreAsync(f.Upload, Access.UserId);
                } else file = await storage.StoreGeneratedAsync(await export.PdfAsync(ExportService.Document(f.Title,$"<h1>{ExportService.Escape(f.Title)}</h1><p>{ExportService.Escape(f.ReportText)}</p>")),"personal-report.pdf",Access.UserId);
                d.Pages = PersonalPdfService.PageCount(file.FileData); d.File = file;
            } catch (InvalidDataException ex) { ModelState.AddModelError("",ex.Message); return View(f); }
            Db.Add(d);
        }
        d.Title=f.Title; d.Category=f.Category; d.Description=f.Description??""; d.DocumentDate=f.DocumentDate.Date;
        try { await Db.SaveChangesAsync(); } catch(DbUpdateConcurrencyException) { return Conflict(); }
        TempData["Success"]="บันทึก PDF ในคลังส่วนตัวแล้ว"; return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id,string rowVersion)
    {
        var d=await Db.PersonalDocuments.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==Access.UserId);
        if(d==null)return NotFound();if(!Version(d,rowVersion))return Conflict();
        if(await Db.Set<PortfolioDocument>().AnyAsync(x=>x.PersonalDocumentId==id)||await Db.Set<PortfolioEntry>().AnyAsync(x=>x.PersonalDocumentId==id&&!x.AdmissionPortfolio.IsDeleted)) {
            TempData["Error"]="เอกสารนี้ใช้อยู่ในแฟ้มสะสมผลงาน กรุณานำออกจากแฟ้มก่อนลบ";return RedirectToAction(nameof(Index));
        }
        Db.Remove(d);try {await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}return RedirectToAction(nameof(Index));
    }
}
