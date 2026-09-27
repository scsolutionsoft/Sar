using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Services;

public record PortfolioCheck(string Label, bool Complete);
public class AdmissionPortfolioService(ApplicationDbContext db, ExportService export, IFileStorage storage, IWebHostEnvironment environment)
{
    public static List<PortfolioCheck> Checklist(AdmissionPortfolio p) => [
        new("ชื่อและสถานศึกษา", !string.IsNullOrWhiteSpace(p.FullName)&&!string.IsNullOrWhiteSpace(p.School)),
        new("คณะและมหาวิทยาลัยเป้าหมาย",!string.IsNullOrWhiteSpace(p.TargetUniversity)&&!string.IsNullOrWhiteSpace(p.TargetFaculty)),
        new("ประวัติการศึกษาและผลการเรียน",!string.IsNullOrWhiteSpace(p.Education)&&p.Gpax.HasValue),
        new("เหตุผลที่เลือกเรียนและเป้าหมาย",!string.IsNullOrWhiteSpace(p.Motivation)&&!string.IsNullOrWhiteSpace(p.Goals)),
        new("ทักษะและความสามารถ",!string.IsNullOrWhiteSpace(p.Skills)),
        new("ช่องทางติดต่อ",!string.IsNullOrWhiteSpace(p.ContactEmail)||!string.IsNullOrWhiteSpace(p.Phone)),
        new("ผลงานพร้อมบทบาทและสิ่งที่เรียนรู้",p.Entries.Count>0&&p.Entries.All(x=>!string.IsNullOrWhiteSpace(x.Role)&&!string.IsNullOrWhiteSpace(x.Reflection))),
        new("หลักฐานประกอบ",p.Documents.Count>0||p.Entries.Any(x=>x.PersonalDocumentId.HasValue)),
        new("ลิงก์เกณฑ์รับสมัครสำหรับตรวจทาน",!string.IsNullOrWhiteSpace(p.RequirementsUrl))
    ];
    public async Task InvalidateAsync(AdmissionPortfolio p)
    {
        if(p.PdfFileId.HasValue){var old=await db.FileObjects.SingleOrDefaultAsync(x=>x.FileId==p.PdfFileId);if(old!=null)old.IsDeleted=true;}
        p.PdfFileId=null;p.GeneratedAt=null;p.ExportedPages=null;p.UpdatedAt=DateTime.UtcNow;
    }
    private async Task<string> ImageAsync(Guid? id,string owner)
    {
        if(id==null)return "";
        var owned=await db.AdmissionPortfolios.AnyAsync(x=>x.OwnerId==owner&&x.PortraitId==id)||await db.Set<PortfolioEntry>().AnyAsync(x=>x.ImageId==id&&x.AdmissionPortfolio.OwnerId==owner&&!x.AdmissionPortfolio.IsDeleted);
        if(!owned)return "";
        var f=await db.FileObjects.AsNoTracking().SingleOrDefaultAsync(x=>x.FileId==id&&x.ContentType.StartsWith("image/"));
        return f==null?"":$"data:{f.ContentType};base64,{Convert.ToBase64String(f.FileData)}";
    }
    public async Task<string> HtmlAsync(AdmissionPortfolio p)
    {
        string E(string? value)=>ExportService.Escape(value);
        string Lines(string value,string cls)=>string.Join("",value.Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(x=>$"<div class='{cls}'>{E(x)}</div>"));
        var color=p.Theme switch {PortfolioTheme.Botanical=>"#14665c",PortfolioTheme.Creative=>"#914526",_=>"#354b9d"};
        var b=new StringBuilder();
        void Start(string eyebrow,string heading)=>b.Append($"<section class='folio-page'><div class='folio-top'><span>{E(p.FullName)}</span><span>PORTFOLIO</span></div><p class='kicker'>{E(eyebrow)}</p><h1>{E(heading)}</h1>");
        void End()=>b.Append("</section>");
        var portrait=await ImageAsync(p.PortraitId,p.OwnerId);
        b.Append($"<section class='folio-page folio-cover'><p class='kicker'>A JOURNEY OF LEARNING</p><div class='cover-word'>PORT<br>FOLIO<span>.</span></div><h1>{E(p.Title)}</h1>");
        if(portrait!="")b.Append($"<img class='portrait' src='{portrait}' alt='ภาพปก'>");
        b.Append($"<div class='cover-name'><h2>{E(p.FullName)}</h2><p>{E(p.School)}<br>{E(p.StudyPlan)}</p></div><div class='cover-target'>{E(p.TargetFaculty)}<br>{E(p.TargetUniversity)} · {E(p.AdmissionRound)}</div></section>");
        if(p.IncludeContents){Start("CONTENTS","สารบัญ");var n=1;foreach(var name in new[]{"ประวัติส่วนตัวและการศึกษา","เส้นทางที่เลือกและเป้าหมาย","ทักษะและภาพรวมผลงาน"}.Concat(p.Entries.OrderBy(x=>x.SortOrder).ThenBy(x=>x.Id).Select(x=>x.Title)).Concat(p.IncludeEvidence?new[]{"เอกสารหลักฐานแนบท้าย"}:Array.Empty<string>()))b.Append($"<div class='toc-row'><strong>{n++:00}</strong><span>{E(name)}</span></div>");End();}
        Start("ABOUT ME","ประวัติส่วนตัวและการศึกษา");
        b.Append($"<h2>{E(p.FullName)}</h2><p>{E(p.Bio)}</p><div class='info-grid'><div><small>สถานศึกษา</small><h3>{E(p.School)}</h3></div><div><small>แผนการเรียน</small><h3>{E(p.StudyPlan)}</h3></div><div><small>GPAX</small><strong class='big-number'>{(p.Gpax?.ToString("0.00",CultureInfo.InvariantCulture)??"—")}</strong></div><div><small>ปีที่จบการศึกษา</small><h3>{E(p.GraduationYear)}</h3></div></div><h2>เส้นทางการศึกษา</h2>{Lines(p.Education,"timeline-item")}<div class='contact'><strong>ติดต่อ</strong><p>{E(p.ContactEmail)}<br>{E(p.Phone)}</p></div>");End();
        Start("PURPOSE & DIRECTION","เหตุผลที่เลือกเส้นทางนี้");b.Append($"<div class='quote'>{E(p.TargetFaculty)}<br><small>{E(p.TargetUniversity)}</small></div><h2>แรงบันดาลใจและความตั้งใจ</h2><p>{E(p.Motivation)}</p><h2>เป้าหมายในอนาคต</h2><p>{E(p.Goals)}</p>");End();
        Start("SKILLS & HIGHLIGHTS","ทักษะและภาพรวมผลงาน");
        b.Append($"<div class='stat-grid'><div><strong>{p.Entries.Count}</strong><span>ผลงานที่คัดเลือก</span></div><div><strong>{p.Entries.Count(x=>x.Section==PortfolioSection.Award)}</strong><span>รางวัล</span></div><div><strong>{p.Entries.Count(x=>x.Section==PortfolioSection.Volunteer)}</strong><span>จิตอาสา</span></div></div><h2>ทักษะของฉัน</h2>");
        foreach(var line in p.Skills.Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)){
            var parts=line.Split('|',2);b.Append($"<div class='skill'><strong>{E(parts[0])}</strong>");
            if(parts.Length==2&&int.TryParse(parts[1].Trim(),out var level)&&level>=0&&level<=100)b.Append($"<span>{level}%</span><div class='skill-track'><i style='width:{level}%'></i></div>");
            else if(parts.Length==2)b.Append($"<p>{E(parts[1])}</p>");b.Append("</div>");
        }b.Append("<small>ระดับทักษะเป็นการประเมินตนเองโดยผู้จัดทำ</small>");End();
        foreach(var item in p.Entries.OrderBy(x=>x.SortOrder).ThenBy(x=>x.Id)){
            Start(PersonalInput.Section(item.Section),item.Title);b.Append($"<div class='entry-meta'>{Thai.Date(item.Date)} · {E(item.Role)}</div>");
            var image=await ImageAsync(item.ImageId,p.OwnerId);if(image!="")b.Append($"<img class='entry-image' src='{image}' alt='{E(item.Title)}'>");
            b.Append($"<h2>แนวคิดและกระบวนการ</h2><p>{E(item.Description)}</p><div class='reflection-grid'><div><h2>ผลลัพธ์</h2><p>{E(item.Outcome)}</p></div><div><h2>สิ่งที่ได้เรียนรู้</h2><p>{E(item.Reflection)}</p></div></div>");
            if(!string.IsNullOrWhiteSpace(item.Link)&&PersonalInput.SafeLink(item.Link))b.Append($"<p class='work-link'>ผลงานเพิ่มเติม: <a href='{E(item.Link)}'>{E(item.Link)}</a></p>");
            if(item.PersonalDocumentId.HasValue)b.Append("<small>มีเอกสารหลักฐานในคลังส่วนตัว"+(p.IncludeEvidence?" · รวมในภาคผนวก":"")+"</small>");End();
        }
        if(p.IncludeClosing){Start("THANK YOU","ขอบคุณที่พิจารณาแฟ้มสะสมผลงาน");b.Append($"<div class='quote'>{E(p.FullName)}</div><p>{E(p.Goals)}</p><p>{E(p.ContactEmail)}<br>{E(p.Phone)}</p>");End();}
        var fontPath=Path.Combine(environment.WebRootPath??Path.Combine(environment.ContentRootPath,"wwwroot"),"fonts","Sarabun-Regular.ttf");
        var font=File.Exists(fontPath)?$"@font-face{{font-family:Folio;src:url(data:font/ttf;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(fontPath))})}}":"";
        return $"<!doctype html><html lang='th'><head><meta charset='utf-8'><title>{E(p.Title)}</title><style>{font}"+Style.Replace("ACCENT",color)+"</style></head><body>"+b+"</body></html>";
    }
    public async Task GenerateAsync(AdmissionPortfolio p,string owner)
    {
        if(p.OwnerId!=owner)throw new UnauthorizedAccessException();
        var ids=p.IncludeEvidence?p.Documents.OrderBy(x=>x.SortOrder).Select(x=>x.PersonalDocumentId).Concat(p.Entries.OrderBy(x=>x.SortOrder).Select(x=>x.PersonalDocumentId).Where(x=>x.HasValue).Select(x=>x!.Value)).Distinct().ToArray():[];
        var documents=await db.PersonalDocuments.AsNoTracking().Where(x=>ids.Contains(x.Id)&&x.OwnerId==owner).Include(x=>x.File).ToListAsync();
        if(documents.Count!=ids.Length)throw new InvalidDataException("หลักฐานบางรายการไม่พร้อมใช้งาน กรุณาเลือกใหม่");
        var body=await export.PdfAsync(await HtmlAsync(p), showPageNumbers: false);
        var result=PersonalPdfService.Combine(body,ids.Select(id=>documents.Single(x=>x.Id==id)).Select(x=>(x.Title,x.File.FileData)).ToList(),p.MaxPages,p.MaxMegabytes);
        await InvalidateAsync(p);
        var file=await storage.StoreGeneratedAsync(result.Bytes,$"portfolio-{p.Id}.pdf",owner);
        p.PdfFileId=file.FileId;p.ExportedPages=result.Pages;p.GeneratedAt=DateTime.UtcNow;
        // Caller saves the PDF and owner record together; RowVersion detects edits made during rendering.
    }
    private const string Style="""
    *{box-sizing:border-box}body{font-family:Folio,Tahoma,sans-serif;margin:0;color:#26344e;font-size:12px;line-height:1.65;--accent:ACCENT}h1{font-size:29px;line-height:1.45;color:var(--accent);margin:10px 0 22px}h2{font-size:17px;color:var(--accent);margin:20px 0 10px}h3{font-size:15px;margin:8px 0}p{white-space:pre-wrap;margin:8px 0 16px;overflow-wrap:anywhere}small{color:#68768b;font-size:10px}.folio-page{min-height:245mm;padding:12mm 9mm;break-before:page;position:relative}.folio-page:first-child{break-before:auto}.folio-top{display:flex;justify-content:space-between;border-bottom:1px solid #dce2ec;padding-bottom:12px;margin-bottom:25px;font-size:10px;color:#68768b}.kicker{letter-spacing:2px;font-size:10px;font-weight:bold;color:var(--accent);margin-top:15px}.folio-cover{background:#eef1f8;border-left:12px solid var(--accent);border-radius:12px}.cover-word{font-family:Arial,sans-serif;font-size:100px;line-height:.93;letter-spacing:-6px;font-weight:900;color:var(--accent);margin:25px 0}.cover-word span{color:#e7a44c}.folio-cover h1{font-size:22px;max-width:95%}.portrait{width:100%;height:65mm;object-fit:cover;border-radius:14px}.cover-name{margin-top:25px}.cover-name h2{font-size:26px}.cover-target{border-top:2px solid var(--accent);margin-top:25px;padding-top:15px}.toc-row{display:flex;gap:20px;padding:13px 0;border-bottom:1px solid #e0e5ee;break-inside:avoid}.toc-row strong{color:var(--accent);font-size:20px}.info-grid{display:grid;grid-template-columns:1fr 1fr;gap:14px;background:#f1f4f8;padding:20px;border-radius:12px;margin:20px 0}.big-number{font-size:38px;color:var(--accent)}.timeline-item{border-left:3px solid var(--accent);padding:8px 15px;margin:12px 0;break-inside:avoid}.contact{margin-top:25px;padding:18px;background:#f1f4f8;border-radius:12px}.quote{background:var(--accent);color:white;font-size:24px;padding:28px;border-radius:14px;margin:25px 0}.quote small{color:#edf2ff;font-size:15px}.stat-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin:25px 0}.stat-grid>div{border:1px solid #dbe2ed;border-radius:14px;padding:20px 10px;text-align:center}.stat-grid strong{display:block;font-size:38px;color:var(--accent)}.stat-grid span{font-size:11px}.skill{padding:12px 0;break-inside:avoid}.skill>span{float:right}.skill-track{height:9px;border-radius:8px;background:#e6eaf1;margin-top:8px}.skill-track i{display:block;height:100%;border-radius:8px;background:var(--accent)}.entry-meta{background:#f0f3f8;border-radius:8px;padding:10px 14px;margin-bottom:15px}.entry-image{width:100%;height:auto;max-height:65mm;object-fit:contain;border-radius:12px;margin-bottom:10px}.reflection-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px;background:#f0f3f8;padding:15px;border-radius:10px;break-inside:avoid}.reflection-grid h2{margin-top:0}.work-link{font-size:10px}a{color:var(--accent)}@media screen{body{background:#dde3ee;padding:20px}.folio-page{background-color:white;width:186mm;margin:0 auto 20px;box-shadow:0 5px 20px #0001}.folio-cover{background:#eef1f8}}@media print{.folio-page{box-shadow:none}}
    """;
}
