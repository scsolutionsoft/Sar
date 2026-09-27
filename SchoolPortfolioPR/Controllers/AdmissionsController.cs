using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;

[Authorize(Policy = "StudentPortfolio")]
public class AdmissionsController(ApplicationDbContext db, AccessService access, AdmissionPortfolioService portfolio, IFileStorage storage) : EditorController(db,access)
{
    private IQueryable<AdmissionPortfolio> Own => Db.AdmissionPortfolios.Where(x=>x.OwnerId==Access.UserId);
    private IQueryable<AdmissionPortfolio> WithItems => Own.Include(x=>x.Entries).Include(x=>x.Documents).ThenInclude(x=>x.PersonalDocument).AsSplitQuery();
    public async Task<IActionResult> Index()=>View(await Own.AsNoTracking().OrderByDescending(x=>x.UpdatedAt).ToListAsync());
    public async Task<IActionResult> Details(int id)
    {
        var p=await WithItems.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();
        ViewBag.Checklist=AdmissionPortfolioService.Checklist(p);return View(p);
    }
    private async Task Choices()=>ViewBag.Documents=await Db.PersonalDocuments.AsNoTracking().Where(x=>x.OwnerId==Access.UserId).OrderByDescending(x=>x.DocumentDate).ToListAsync();
    public async Task<IActionResult> Edit(int? id)
    {
        await Choices();
        if(id==null){var user=await Db.Users.Include(x=>x.Student).SingleAsync(x=>x.Id==Access.UserId);return View(new AdmissionForm{FullName=user.Student?.Name??user.DisplayName,ContactEmail=user.Email});}
        var p=await WithItems.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();
        var f=new AdmissionForm();
        // Copy only explicitly declared form properties; owner, export IDs and publication rights are never form fields.
        foreach(var property in typeof(AdmissionForm).GetProperties()){var source=typeof(AdmissionPortfolio).GetProperty(property.Name);if(source!=null&&source.PropertyType==property.PropertyType)property.SetValue(f,source.GetValue(p));}
        f.RowVersion=Convert.ToBase64String(p.RowVersion);f.CurrentPortraitId=p.PortraitId;f.DocumentIds=p.Documents.OrderBy(x=>x.SortOrder).Select(x=>x.PersonalDocumentId).ToArray();return View(f);
    }
    [HttpPost,RequestSizeLimit(12*1024*1024)]
    public async Task<IActionResult> Edit(AdmissionForm f)
    {
        var p=f.Id==0?new AdmissionPortfolio{OwnerId=Access.UserId}:await WithItems.SingleOrDefaultAsync(x=>x.Id==f.Id);
        if(p==null)return NotFound();if(!Version(p,f.RowVersion))return Conflict();f.CurrentPortraitId=p.PortraitId;
        if(await Db.PersonalDocuments.CountAsync(x=>f.DocumentIds.Contains(x.Id)&&x.OwnerId==Access.UserId)!=f.DocumentIds.Length)ModelState.AddModelError("","เลือกได้เฉพาะเอกสารของตนเอง");
        if(!ModelState.IsValid){await Choices();return View(f);}
        FileObject? photo=null;try{if(f.PortraitUpload!=null)photo=await StoreImage(f.PortraitUpload);}catch(InvalidDataException ex){ModelState.AddModelError("",ex.Message);await Choices();return View(f);}
        p.Title=f.Title;p.FullName=f.FullName;p.School=f.School;p.StudyPlan=f.StudyPlan??"";p.GraduationYear=f.GraduationYear??"";p.Gpax=f.Gpax;p.ContactEmail=f.ContactEmail??"";p.Phone=f.Phone??"";
        p.TargetUniversity=f.TargetUniversity??"";p.TargetFaculty=f.TargetFaculty??"";p.AdmissionRound=f.AdmissionRound??"";p.RequirementsUrl=f.RequirementsUrl??"";p.Bio=f.Bio??"";p.Motivation=f.Motivation??"";p.Education=f.Education??"";p.Skills=f.Skills??"";p.Goals=f.Goals??"";
        p.Theme=f.Theme;p.IncludeContents=f.IncludeContents;p.IncludeClosing=f.IncludeClosing;p.IncludeEvidence=f.IncludeEvidence;p.MaxPages=f.MaxPages;p.MaxMegabytes=f.MaxMegabytes;
        if(photo!=null||f.RemovePortrait){await HideImage(p.PortraitId);p.PortraitId=photo?.FileId;}
        foreach(var old in p.Documents.Where(x=>!f.DocumentIds.Contains(x.PersonalDocumentId)).ToList()){Db.Remove(old);p.Documents.Remove(old);}
        for(var i=0;i<f.DocumentIds.Length;i++){var link=p.Documents.SingleOrDefault(x=>x.PersonalDocumentId==f.DocumentIds[i]);if(link==null)p.Documents.Add(new PortfolioDocument{PersonalDocumentId=f.DocumentIds[i],SortOrder=i});else link.SortOrder=i;}
        await portfolio.InvalidateAsync(p);if(f.Id==0)Db.Add(p);
        try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}return RedirectToAction(nameof(Details),new{id=p.Id});
    }
    private async Task<FileObject> StoreImage(IFormFile file){if(!file.ContentType.StartsWith("image/"))throw new InvalidDataException("เลือกภาพ JPG, PNG หรือ WebP");return await storage.StoreAsync(file,Access.UserId);}
    private async Task HideImage(Guid? id){if(id.HasValue){var image=await Db.FileObjects.SingleOrDefaultAsync(x=>x.FileId==id);if(image!=null)image.IsDeleted=true;}}
    public async Task<IActionResult> Entry(int portfolioId,int? id)
    {
        var p=await WithItems.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==portfolioId);if(p==null)return NotFound();await Choices();
        var f=new PortfolioEntryForm{PortfolioId=p.Id,PortfolioVersion=Convert.ToBase64String(p.RowVersion)};
        if(id.HasValue){var e=p.Entries.SingleOrDefault(x=>x.Id==id);if(e==null)return NotFound();foreach(var prop in typeof(PortfolioEntryForm).GetProperties()){var source=typeof(PortfolioEntry).GetProperty(prop.Name);if(source!=null&&source.PropertyType==prop.PropertyType)prop.SetValue(f,source.GetValue(e));}f.RowVersion=Convert.ToBase64String(e.RowVersion);f.CurrentImageId=e.ImageId;}
        return View(f);
    }
    [HttpPost,RequestSizeLimit(12*1024*1024)]
    public async Task<IActionResult> Entry(PortfolioEntryForm f)
    {
        var p=await WithItems.SingleOrDefaultAsync(x=>x.Id==f.PortfolioId);if(p==null)return NotFound();if(!Version(p,f.PortfolioVersion))return Conflict();
        var e=f.Id==0?new PortfolioEntry{AdmissionPortfolioId=p.Id}:p.Entries.SingleOrDefault(x=>x.Id==f.Id);if(e==null)return NotFound();if(!Version(e,f.RowVersion))return Conflict();f.CurrentImageId=e.ImageId;
        if(f.PersonalDocumentId.HasValue&&!await Db.PersonalDocuments.AnyAsync(x=>x.Id==f.PersonalDocumentId&&x.OwnerId==Access.UserId))ModelState.AddModelError("","ไม่พบหลักฐานในคลังส่วนตัว");
        if(f.Id==0&&p.Entries.Count>=30)ModelState.AddModelError("","หนึ่งแฟ้มรองรับผลงานไม่เกิน 30 รายการ");
        if(!ModelState.IsValid){await Choices();return View(f);}
        FileObject? image=null;try{if(f.ImageUpload!=null)image=await StoreImage(f.ImageUpload);}catch(InvalidDataException ex){ModelState.AddModelError("",ex.Message);await Choices();return View(f);}
        e.Title=f.Title;e.Section=f.Section;e.Date=f.Date.Date;e.Role=f.Role??"";e.Description=f.Description??"";e.Outcome=f.Outcome??"";e.Reflection=f.Reflection??"";e.Link=f.Link??"";e.SortOrder=f.SortOrder;e.PersonalDocumentId=f.PersonalDocumentId;
        if(image!=null||f.RemoveImage){await HideImage(e.ImageId);e.ImageId=image?.FileId;}
        if(f.Id==0)Db.Add(e);await portfolio.InvalidateAsync(p);try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}return RedirectToAction(nameof(Details),new{id=p.Id});
    }
    [HttpPost]
    public async Task<IActionResult> DeleteEntry(int id,int portfolioId,string rowVersion,string portfolioVersion)
    {
        var p=await WithItems.SingleOrDefaultAsync(x=>x.Id==portfolioId);if(p==null)return NotFound();var e=p.Entries.SingleOrDefault(x=>x.Id==id);if(e==null)return NotFound();if(!Version(p,portfolioVersion)||!Version(e,rowVersion))return Conflict();
        Db.Remove(e);await portfolio.InvalidateAsync(p);try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}return RedirectToAction(nameof(Details),new{id=p.Id});
    }
    [HttpPost]
    public async Task<IActionResult> Order(int id,string rowVersion,int[] order)
    {
        var p=await WithItems.SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();if(!Version(p,rowVersion))return Conflict();
        if(order.Length!=p.Entries.Count||order.Distinct().Count()!=order.Length||order.Any(i=>p.Entries.All(x=>x.Id!=i)))return BadRequest();
        for(var i=0;i<order.Length;i++)p.Entries.Single(x=>x.Id==order[i]).SortOrder=i;
        await portfolio.InvalidateAsync(p);try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}return RedirectToAction(nameof(Details),new{id});
    }
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Preview(int id)
    {
        var p=await WithItems.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();
        // Script-free, owner-only preview uses embedded local fonts and images.
        Response.Headers["Content-Security-Policy"]="default-src 'none'; style-src 'unsafe-inline'; img-src data:; font-src data:; frame-ancestors 'self'; base-uri 'none'";
        return Content(await portfolio.HtmlAsync(p),"text/html; charset=utf-8");
    }
    [HttpPost]
    public async Task<IActionResult> Generate(int id,string rowVersion)
    {
        var p=await WithItems.SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();if(!Version(p,rowVersion))return Conflict();
        try{await portfolio.GenerateAsync(p,Access.UserId);await Db.SaveChangesAsync();}
        catch(InvalidDataException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Details),new{id});}
        catch(DbUpdateConcurrencyException){return Conflict("แฟ้มถูกแก้ไขระหว่างสร้าง PDF กรุณาสร้างใหม่");}
        return RedirectToAction(nameof(Viewer),new{id});
    }
    public async Task<IActionResult> Viewer(int id){var p=await Own.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);return p==null?NotFound():View(p);}
    [HttpPost]
    public async Task<IActionResult> Delete(int id,string rowVersion){var p=await Own.SingleOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();if(!Version(p,rowVersion))return Conflict();Db.Remove(p);try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}return RedirectToAction(nameof(Index));}
}
