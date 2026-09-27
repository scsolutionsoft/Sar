using System.Text;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Services;
public class PublicationService(ApplicationDbContext db, ExportService export, IFileStorage storage)
{
    public static readonly string[] Sections = ["ข่าวโรงเรียน", "กิจกรรมวิชาการ", "กิจกรรมนักเรียน", "ผลงานครู", "รางวัลนักเรียน"];
    public async Task<List<PublicationChoice>> ChoicesAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1); var end = start.AddMonths(1);
        var activities = await db.Activities.AsNoTracking().Where(x => x.Status == WorkflowStatus.Published && x.IsPublic && x.ActivityDate >= start && x.ActivityDate < end)
            .Select(x => new { x.Id, x.Title, Category = x.Category.Name }).ToListAsync();
        var awards = await db.Awards.AsNoTracking().Where(x => x.Status == WorkflowStatus.Published && x.IsPublic && x.AwardDate >= start && x.AwardDate < end)
            .Select(x => new { x.Id, x.AwardName, HasStudents = x.Students.Any() }).ToListAsync();
        return activities.Select(x => new PublicationChoice("a:" + x.Id, x.Title, x.Category.Contains("วิชาการ") ? Sections[1] : x.Category.Contains("นักเรียน") ? Sections[2] : Sections[0], false))
            .Concat(awards.Select(x => new PublicationChoice("w:" + x.Id, x.AwardName, x.HasStudents ? Sections[4] : Sections[3], false))).ToList();
    }
    public async Task<string> HtmlAsync(MonthlyPublication p)
    {
        var school = await db.Set<SchoolProfile>().AsNoTracking().FirstAsync();
        var b = new StringBuilder();
        string E(string? s) => ExportService.Escape(s);
        var cover = await db.Set<PublicationItem>().Where(x => x.MonthlyPublicationId == p.Id && x.ActivityId != null && x.Activity!.IsPublic && x.Activity.Status == WorkflowStatus.Published)
            .OrderBy(x => x.SortOrder).Select(x => x.Activity!.CoverImageId).FirstOrDefaultAsync();
        b.Append($"<div class='cover magazine-cover'><p class='magazine-kicker'>SCHOOL STORIES · วารสารประชาสัมพันธ์</p><p class='magazine-issue'>ฉบับเดือน {p.Month:00} / {p.Year + 543}</p><h1>{E(p.Title)}</h1><h2>{E(school.Name)}</h2>");
        if (cover.HasValue) b.Append(await ImageAsync(cover.Value));
        b.Append("<p>พื้นที่แห่งการเรียนรู้ ความคิดสร้างสรรค์ และเรื่องราวที่อยากบอกต่อ</p></div>");
        b.Append($"<section><h1>สารจากผู้อำนวยการ</h1><p>{E(p.DirectorMessage)}</p><p>{E(school.DirectorName)}</p></section>");
        b.Append("<section><h1>สารบัญ</h1><ol>"); foreach (var s in Sections) b.Append($"<li>{E(s)}</li>"); b.Append("<li>ประมวลภาพ</li><li>สถิติ</li></ol></section>");
        var items = await db.Set<PublicationItem>().Where(x => x.MonthlyPublicationId == p.Id).OrderBy(x => x.SortOrder).ToListAsync();
        var images = new List<Guid>(); int activityCount = 0, awardCount = 0;
        foreach (var section in Sections)
        {
            b.Append($"<section><h1>{E(section)}</h1>");
            foreach (var item in items.Where(x => x.Section == section))
            {
                if (item.ActivityId.HasValue) {
                    var a = await db.Activities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.ActivityId && x.Status == WorkflowStatus.Published && x.IsPublic);
                    if (a == null) continue;
                    activityCount++; b.Append($"<article><h2>{E(a.Title)}</h2><p>{Thai.Date(a.ActivityDate)} · {E(a.Location)}</p><p>{E(a.Description)}</p>");
                    if (a.CoverImageId.HasValue) b.Append(await ImageAsync(a.CoverImageId.Value));
                    b.Append("</article>"); images.AddRange(await db.Set<ActivityImage>().Where(x => x.ActivityId == a.Id).OrderBy(x => x.SortOrder).Select(x => x.FileId).ToListAsync());
                }
                if (item.AwardId.HasValue) {
                    var a = await db.Awards.AsNoTracking().Include(x => x.Teachers).ThenInclude(x => x.Teacher).Include(x => x.Students).ThenInclude(x => x.Student).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == item.AwardId && x.Status == WorkflowStatus.Published && x.IsPublic);
                    if (a == null) continue;
                    awardCount++; b.Append($"<article><h2>{E(a.AwardName)}</h2><p>{E(a.CompetitionName)} · {Thai.Level(a.AwardLevel)} · {E(a.AwardRank)} · {Thai.Date(a.AwardDate)}</p><p>{E(a.Description)}</p><p>{E(string.Join(", ", a.Teachers.Select(x => x.Teacher.Name).Concat(a.Students.Select(x => x.Student.Name))))}</p></article>");
                }
            }
            b.Append("</section>");
        }
        b.Append("<section><h1>ประมวลภาพ</h1><div class='gallery'>"); foreach (var id in images.Distinct()) b.Append(await ImageAsync(id)); b.Append("</div></section>");
        b.Append($"<section><h1>สถิติ</h1><p>กิจกรรม {activityCount} รายการ</p><p>รางวัล {awardCount} รายการ</p><p>รูปภาพ {images.Distinct().Count()} ภาพ</p></section>");
        b.Append($"<section class='cover'><h1>{E(school.Name)}</h1><p>{E(school.Address)}</p><p>{E(school.Contact)}</p><p>เรียนรู้ สร้างสรรค์ และเติบโตไปด้วยกัน</p></section>");
        const string magazineStyle = "<style>.magazine-cover{box-sizing:border-box;min-height:900px;background:linear-gradient(145deg,#182e62,#4565ad);color:white;border-radius:12px;padding:35px;justify-content:flex-start;gap:8px}.magazine-cover h1{font-size:36px;line-height:1.4;margin:14px 0}.magazine-cover h2{color:#dceaff;font-size:19px}.magazine-cover img{width:100%;height:370px;object-fit:cover;border-radius:12px;margin-top:14px}.magazine-kicker{letter-spacing:2px;font-size:12px}.magazine-issue{background:#ffffff20;border-radius:20px;padding:7px 15px;align-self:flex-start}section>h1{border-bottom:3px solid #566fd0;padding-bottom:12px}article{margin-bottom:28px}article>img{display:block;margin:18px auto;border-radius:10px}article h2{color:#3454a0}</style>";
        return ExportService.Document(p.Title, magazineStyle + b.ToString());
    }
    private async Task<string> ImageAsync(Guid id)
    {
        var f = await db.FileObjects.AsNoTracking().Where(x => x.FileId == id && x.ContentType.StartsWith("image/")).Select(x => new { x.ContentType, x.FileData, x.OriginalFileName }).SingleOrDefaultAsync();
        return f == null ? "" : $"<img alt='{ExportService.Escape(f.OriginalFileName)}' src='data:{f.ContentType};base64,{Convert.ToBase64String(f.FileData)}'>";
    }
    public async Task GenerateAsync(MonthlyPublication p, string user)
    {
        var bytes = await export.PdfAsync(await HtmlAsync(p));
        var f = await storage.StoreGeneratedAsync(bytes, $"publication-{p.Year + 543}-{p.Month:00}.pdf", user);
        // Replaced editions are hidden; they must not remain available through a previous public URL.
        var old = await db.Set<PublicationFile>().Where(x => x.MonthlyPublicationId == p.Id).Select(x => x.File).ToListAsync();
        foreach (var file in old) file.IsDeleted = true;
        p.Files.Add(new PublicationFile { File = f }); await db.SaveChangesAsync();
    }
}
