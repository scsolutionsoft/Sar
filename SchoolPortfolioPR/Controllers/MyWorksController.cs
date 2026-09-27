using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

namespace SchoolPortfolioPR.Controllers;

[Authorize,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class MyWorksController(ApplicationDbContext db,AccessService access,IFileStorage storage) : EditorController(db,access)
{
    private IQueryable<PersonalWork> Own => Db.PersonalWorks.Where(x=>x.OwnerId==Access.UserId);
    public async Task<IActionResult> Index(PersonalWorkKind? kind,string? q,int page=1)
    {
        if(!ModelState.IsValid||kind.HasValue&&!Enum.IsDefined(kind.Value))return BadRequest();
        ViewBag.Counts=await Own.GroupBy(x=>x.Kind).Select(x=>new {Kind=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Kind,x=>x.Count);
        var query=Own.AsNoTracking();if(kind.HasValue)query=query.Where(x=>x.Kind==kind);
        if(!string.IsNullOrWhiteSpace(q))query=query.Where(x=>x.Title.Contains(q)||x.Description.Contains(q)||x.Category.Contains(q)||x.Media.Any(m=>m.Caption.Contains(q)));
        page=Math.Max(1,page);ViewBag.Kind=kind;ViewBag.Search=q;
        var rows=await query.OrderByDescending(x=>x.WorkDate).ThenByDescending(x=>x.Id).Skip((page-1)*12).Take(12)
            .Select(x=>new PersonalWorkCard(x.Id,x.Kind,x.Title,x.WorkDate,x.Category,x.Description,
                x.Media.Where(m=>m.FileId!=null&&m.File!.ContentType.StartsWith("image/")).OrderBy(m=>m.SortOrder).ThenBy(m=>m.Id).Select(m=>m.FileId).FirstOrDefault(),x.Media.Count)).ToListAsync();
        return View(new PageView<PersonalWorkCard>(rows,page,await query.CountAsync(),12));
    }
    public async Task<IActionResult> Details(int id)
    {
        var work=await Own.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(work==null)return NotFound();
        var media=await Db.PersonalWorkMedia.AsNoTracking().Where(x=>x.PersonalWorkId==id).OrderBy(x=>x.SortOrder).ThenBy(x=>x.Id)
            .Select(x=>new WorkMediaView(x.Id,x.FileId,x.File==null?"ลิงก์จากคลาวด์":x.File.OriginalFileName,x.File==null?"":x.File.ContentType,x.Caption,x.ExternalUrl)).ToListAsync();
        return View(new PersonalWorkDetail(work,media));
    }
    public async Task<IActionResult> Edit(int? id,PersonalWorkKind kind=PersonalWorkKind.Activity)
    {
        if(!ModelState.IsValid||!Enum.IsDefined(kind))return BadRequest();
        if(id==null)return View(new PersonalWorkForm{Kind=kind});
        var w=await Own.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(w==null)return NotFound();
        return View(new PersonalWorkForm{Id=w.Id,RowVersion=Convert.ToBase64String(w.RowVersion),Kind=w.Kind,Title=w.Title,WorkDate=w.WorkDate,Category=w.Category,Organizer=w.Organizer,Location=w.Location,Role=w.Role,Achievement=w.Achievement,Description=w.Description});
    }
    [HttpPost,RequestSizeLimit(57671680)]
    public async Task<IActionResult> Edit(PersonalWorkForm f)
    {
        var w=f.Id==0?new PersonalWork{OwnerId=Access.UserId,Kind=f.Kind}:await Own.Include(x=>x.Media).SingleOrDefaultAsync(x=>x.Id==f.Id);
        if(w==null)return NotFound();if(!Version(w,f.RowVersion))return Conflict("ข้อมูลเปลี่ยนแปลง กรุณาเปิดหน้าใหม่");
        if(f.Id!=0&&f.Kind!=w.Kind)ModelState.AddModelError(nameof(f.Kind),"ไม่สามารถเปลี่ยนประเภทของรายการเดิมได้");
        if(!ModelState.IsValid)return View(f);
        try{await AddFiles(w,f.Uploads,"");}catch(InvalidDataException e){ModelState.AddModelError(nameof(f.Uploads),e.Message);return View(f);}
        w.Title=f.Title.Trim();w.WorkDate=f.WorkDate.Date;w.Category=string.IsNullOrWhiteSpace(f.Category)?"ทั่วไป":f.Category.Trim();
        w.Organizer=f.Organizer?.Trim()??"";w.Location=f.Location?.Trim()??"";w.Role=f.Role?.Trim()??"";w.Achievement=f.Achievement?.Trim()??"";w.Description=f.Description?.Trim()??"";
        if(f.Id==0)Db.Add(w);else Db.Entry(w).Property(x=>x.Title).IsModified=true;
        try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict("ข้อมูลเปลี่ยนแปลง กรุณาเปิดหน้าใหม่");}
        TempData["Success"]="บันทึกผลงานของคุณแล้ว";return RedirectToAction(nameof(Details),new{id=w.Id});
    }
    private async Task AddFiles(PersonalWork work,List<IFormFile> files,string caption)
    {
        if(files.Count>20||files.Sum(x=>x.Length)>52428800)throw new InvalidDataException("สูงสุด 20 ไฟล์ รวมไม่เกิน 50 MB");
        var order=work.Media.Select(x=>x.SortOrder).DefaultIfEmpty(-1).Max()+1;
        foreach(var file in files){
            var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
            if(!new[]{".png",".jpg",".jpeg",".webp",".pdf"}.Contains(ext)||work.Kind==PersonalWorkKind.Album&&ext==".pdf")throw new InvalidDataException(work.Kind==PersonalWorkKind.Album?"อัลบั้มรับเฉพาะรูป PNG, JPG หรือ WebP":"รองรับรูปภาพ PNG, JPG, WebP และ PDF เท่านั้น");
            work.Media.Add(new PersonalWorkMedia{File=await storage.StoreAsync(file,Access.UserId),Caption=caption,SortOrder=order++});
        }
    }
    [HttpPost,RequestSizeLimit(57671680)]
    public async Task<IActionResult> Upload(int id,string rowVersion,List<IFormFile> files,string? caption)
    {
        var w=await Own.Include(x=>x.Media).SingleOrDefaultAsync(x=>x.Id==id);if(w==null)return NotFound();if(!Version(w,rowVersion))return Conflict();
        if(!ModelState.IsValid||files.Count==0||caption?.Length>500)return BadRequest();
        try{await AddFiles(w,files,caption?.Trim()??"");}catch(InvalidDataException e){TempData["Error"]=e.Message;return RedirectToAction(nameof(Details),new{id});}
        return await SaveMedia(w,"เพิ่มไฟล์ในผลงานแล้ว");
    }
    [HttpPost]
    public async Task<IActionResult> Link(int id,string rowVersion,string url,string? caption)
    {
        var w=await Own.Include(x=>x.Media).SingleOrDefaultAsync(x=>x.Id==id);if(w==null)return NotFound();if(!Version(w,rowVersion))return Conflict();
        if(!ModelState.IsValid||!GalleryLinks.IsValid(url)||caption?.Length>500){TempData["Error"]="กรุณาใช้ URL แบบ https:// หรือ http:// และคำบรรยายไม่เกิน 500 ตัวอักษร";return RedirectToAction(nameof(Details),new{id});}
        w.Media.Add(new PersonalWorkMedia{ExternalUrl=url.Trim(),Caption=caption?.Trim()??"",SortOrder=w.Media.Select(x=>x.SortOrder).DefaultIfEmpty(-1).Max()+1});
        return await SaveMedia(w,"เพิ่มลิงก์คลาวด์แล้ว");
    }
    [HttpPost]
    public async Task<IActionResult> Media(int id,int mediaId,string rowVersion,string? caption,string? url)
    {
        var w=await Own.SingleOrDefaultAsync(x=>x.Id==id);if(w==null)return NotFound();if(!Version(w,rowVersion))return Conflict();
        var item=await Db.PersonalWorkMedia.SingleOrDefaultAsync(x=>x.Id==mediaId&&x.PersonalWorkId==id);if(item==null)return NotFound();
        if(!ModelState.IsValid||caption?.Length>500||item.FileId==null&&!GalleryLinks.IsValid(url))return BadRequest("คำบรรยายหรือลิงก์ไม่ถูกต้อง");
        item.Caption=caption?.Trim()??"";if(item.FileId==null)item.ExternalUrl=url!.Trim();
        return await SaveMedia(w,"บันทึกคำบรรยายแล้ว");
    }
    [HttpPost]
    public async Task<IActionResult> DeleteMedia(int id,int mediaId,string rowVersion)
    {
        var w=await Own.SingleOrDefaultAsync(x=>x.Id==id);if(w==null)return NotFound();if(!Version(w,rowVersion))return Conflict();
        var item=await Db.PersonalWorkMedia.SingleOrDefaultAsync(x=>x.Id==mediaId&&x.PersonalWorkId==id);if(item==null)return NotFound();
        Db.Remove(item);return await SaveMedia(w,"นำไฟล์หรือลิงก์ออกแล้ว");
    }
    private async Task<IActionResult> SaveMedia(PersonalWork w,string message)
    {
        Db.Entry(w).Property(x=>x.Title).IsModified=true;
        try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict("ข้อมูลเปลี่ยนแปลง กรุณาเปิดหน้าใหม่");}
        TempData["Success"]=message;return RedirectToAction(nameof(Details),new{id=w.Id});
    }
    [HttpPost]
    public async Task<IActionResult> SavePdf(int id,int mediaId)
    {
        var item=await Db.PersonalWorkMedia.Include(x=>x.File).Include(x=>x.PersonalWork).SingleOrDefaultAsync(x=>x.Id==mediaId&&x.PersonalWorkId==id&&x.PersonalWork.OwnerId==Access.UserId&&x.FileId!=null);
        if(item?.File?.ContentType!="application/pdf")return NotFound();
        if(await Db.PersonalDocuments.AnyAsync(x=>x.OwnerId==Access.UserId&&x.FileId==item.FileId)){TempData["Success"]="ไฟล์นี้อยู่ในคลัง PDF ส่วนตัวแล้ว";return RedirectToAction("Index","Documents");}
        try{
            var pages=PersonalPdfService.PageCount(item.File.FileData);
            Db.Add(new PersonalDocument{OwnerId=Access.UserId,Title=item.PersonalWork.Title.Length>200?item.PersonalWork.Title[..200]:item.PersonalWork.Title,Category=WorkKinds.Labels[item.PersonalWork.Kind],Description=item.Caption,DocumentDate=item.PersonalWork.WorkDate,FileId=item.FileId!.Value,Pages=pages});
            await Db.SaveChangesAsync();
        }catch(InvalidDataException e){TempData["Error"]=e.Message;return RedirectToAction(nameof(Details),new{id});}
        TempData["Success"]=Access.IsStudent?"เก็บ PDF เข้าคลังส่วนตัวแล้ว สามารถเลือกใช้เป็นหลักฐานในแฟ้มยื่นเรียนต่อได้":"เก็บ PDF เข้าคลังส่วนตัวแล้ว";
        return RedirectToAction("Index","Documents");
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id,string rowVersion)
    {
        var w=await Own.SingleOrDefaultAsync(x=>x.Id==id);if(w==null)return NotFound();if(!Version(w,rowVersion))return Conflict();
        Db.Remove(w);try{await Db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){return Conflict();}
        TempData["Success"]="ลบรายการผลงานแล้ว";return RedirectToAction(nameof(Index),new{kind=w.Kind});
    }
}
