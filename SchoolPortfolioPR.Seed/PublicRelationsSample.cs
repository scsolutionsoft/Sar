using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;

internal static class PublicRelationsSample
{
    public static async Task RunAsync(ApplicationDbContext db, IConfiguration config, HttpContextAccessor http)
    {
        var admin = await db.Users.SingleAsync(x => x.Email == config["SeedAdmin:Email"]);
        http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, admin.Id), new Claim(ClaimTypes.Role, "SuperAdmin")], "seed"));
        var today = AnnouncementSchedule.ThaiDate(DateTimeOffset.UtcNow);
        var storage = new SqlFileStorage(db, config);
        var originals = await db.Activities.AsNoTracking().Where(x => x.ActivityCode.StartsWith("DEMO-ACT-"))
            .Include(x => x.Images).ThenInclude(x => x.File).OrderBy(x => x.ActivityCode).ToListAsync();
        if (originals.Count != 10 || originals.Any(x => x.Images.Count == 0)) throw new InvalidOperationException("Seed the ten sample activities and their images first.");
        async Task<FileObject> CopyImage(int index)
        {
            var source = originals[index].Images.OrderBy(x => x.SortOrder).First().File;
            using var stream = new MemoryStream(source.FileData);
            return await storage.StoreAsync(new FormFile(stream, 0, stream.Length, "file", source.OriginalFileName) { Headers = new HeaderDictionary(), ContentType = source.ContentType }, admin.Id);
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        string[] titles = ["เปิดโลกวิทยาศาสตร์ จุดประกายความคิดสร้างสรรค์", "เรียนรู้ภาษา เปิดประตูสู่โลกกว้าง", "กีฬาสร้างมิตรภาพ", "อ่านวันละนิด เปิดความคิดวันละเล่ม", "เติบโตไปด้วยกัน กับพื้นที่สีเขียว", "รู้ทันสื่อ ใช้ดิจิทัลอย่างสร้างสรรค์", "พบกันเร็ว ๆ นี้ กับห้องเรียนแห่งอนาคต", "กิจกรรมที่ผ่านมา: วันแห่งการเรียนรู้"];
        string[] summaries = ["รู้จักการทดลองและนวัตกรรม ผ่านเรื่องราวของห้องเรียนวิทยาศาสตร์", "พื้นที่ฝึกภาษาและแลกเปลี่ยนความคิด เรียนรู้ผ่านกิจกรรมที่สนุกและสร้างสรรค์", "พลังของทีม ความสามัคคี และน้ำใจนักกีฬา", "พบหนังสือและแรงบันดาลใจใหม่ ๆ ในห้องสมุดมีชีวิต", "ร่วมสร้างบรรยากาศการเรียนรู้ที่สดใส ด้วยการดูแลต้นไม้และสิ่งแวดล้อม", "เรียนรู้การใช้เทคโนโลยีอย่างปลอดภัยและรับผิดชอบ", "ตัวอย่างประกาศล่วงหน้า ข่าวนี้จะแสดงในวันเริ่มต้นที่กำหนด", "ตัวอย่างข่าวหมดอายุ เก็บไว้ให้ผู้ดูแลทดสอบการตั้งเวลา"];
        string[] categories = ["วิชาการ", "ภาษาและการเรียนรู้", "กิจกรรมนักเรียน", "ห้องสมุด", "สิ่งแวดล้อม", "เทคโนโลยี", "ประกาศล่วงหน้า", "กิจกรรมที่ผ่านมา"];
        int[] sources = [0, 3, 4, 8, 9, 6, 7, 1];
        AnnouncementSize[] sizes = [AnnouncementSize.Large, AnnouncementSize.Medium, AnnouncementSize.Small, AnnouncementSize.Small, AnnouncementSize.Medium, AnnouncementSize.Medium, AnnouncementSize.Small, AnnouncementSize.Small];
        for (var i = 0; i < titles.Length; i++)
        {
            var code = $"DEMO-PR-{i + 1:000}";
            if (await db.Announcements.IgnoreQueryFilters().AnyAsync(x => x.ReferenceCode == code)) continue;
            db.Add(new Announcement { ReferenceCode = code, Title = "[ตัวอย่าง] " + titles[i], Summary = summaries[i],
                Body = "ประกาศสมมติสำหรับสาธิตหน้าแรก ไม่ใช่กำหนดการจริงของโรงเรียน\n\n" + summaries[i] + "\n\nภาพสต็อกประกอบจาก Pexels ใช้เพื่อทดสอบรูปแบบประชาสัมพันธ์ ดูเครดิตภาพในเอกสาร SAMPLE-IMAGE-SOURCES ของโปรเจค",
                Category = categories[i], StartDate = i == 6 ? today.AddDays(7) : today.AddDays(-7), EndDate = i == 7 ? today.AddDays(-1) : today.AddDays(30),
                IsEnabled = true, SortOrder = (i + 1) * 10, Size = sizes[i], Tone = (AnnouncementTone)(i % 4), Image = await CopyImage(sources[i]) });
        }
        await db.SaveChangesAsync();

        // Separate public examples; the original ten private testing activities keep their visibility and workflow.
        var publicCodes = new[] { "PR-DEMO-ACT-001", "PR-DEMO-ACT-002", "PR-DEMO-ACT-003" };
        var existingPublication = await db.Publications.Include(x => x.Items).Include(x => x.Files)
            .FirstOrDefaultAsync(x => x.Title == "[ตัวอย่าง] เรื่องเล่าจากรั้วโรงเรียน");
        var month = new DateTime(today.Year, today.Month, 1);
        if (existingPublication == null)
        {
            while (await db.Publications.AnyAsync(x => x.Year == month.Year && x.Month == month.Month)) month = month.AddMonths(-1);
        }
        else month = new DateTime(existingPublication.Year, existingPublication.Month, 1);
        var stories = new List<Activity>();
        for (var i = 0; i < publicCodes.Length; i++)
        {
            var code = publicCodes[i];
            var story = await db.Activities.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.ActivityCode == code);
            if (story?.IsDeleted == true) throw new InvalidOperationException("A public sample was deleted; it will not be restored automatically.");
            if (story == null)
            {
                var original = originals[sources[i]]; var image = await CopyImage(sources[i]);
                story = new Activity { ActivityCode = code, Slug = $"pr-demo-story-{i + 1:000}", Title = "[ตัวอย่างวารสาร] " + titles[i],
                    ShortDescription = summaries[i], Description = "เรื่องราวสมมติสำหรับสาธิตวารสารประชาสัมพันธ์ ไม่ใช่ข่าวหรือผลงานจริง\n\n" + summaries[i] +
                    "\n\nเรียนรู้ผ่านการลงมือทำ\nกิจกรรมเปิดพื้นที่ให้แลกเปลี่ยนความคิดเห็น ฝึกทักษะ และต่อยอดความคิดสร้างสรรค์\n\nสิ่งที่เราได้เรียนรู้\nการทำงานร่วมกัน การตั้งคำถาม และความรับผิดชอบ เป็นรากฐานของการเรียนรู้ที่นำไปใช้ได้ในชีวิตประจำวัน\n\nภาพประกอบจาก Pexels ใช้สำหรับสาธิตเท่านั้น",
                    CategoryId = original.CategoryId, DepartmentId = original.DepartmentId, AcademicYearId = original.AcademicYearId, SubjectGroupId = original.SubjectGroupId,
                    ActivityDate = month.AddDays(i + 1), FiscalYear = month.Year + 543 + (month.Month >= 10 ? 1 : 0), Location = "พื้นที่การเรียนรู้ (ตัวอย่าง)", Organizer = "ทีมประชาสัมพันธ์ (ตัวอย่าง)",
                    Status = WorkflowStatus.Published, IsPublic = true, IsFeatured = false, PublishedDate = DateTime.UtcNow, CoverImageId = image.FileId };
                story.Images.Add(new ActivityImage { File = image }); db.Add(story);
            }
            stories.Add(story);
        }
        await db.SaveChangesAsync();
        var publication = existingPublication;
        if (publication == null)
        {
            publication = new MonthlyPublication { Title = "[ตัวอย่าง] เรื่องเล่าจากรั้วโรงเรียน", Year = month.Year, Month = month.Month,
                DirectorMessage = "วารสารฉบับตัวอย่างนี้รวบรวมเรื่องราวสมมติและภาพประกอบ เพื่อสาธิตการสื่อสารข่าวสารของโรงเรียน\n\nเรามุ่งสร้างพื้นที่ที่ทุกคนได้เรียนรู้ ลงมือทำ และแบ่งปันความสำเร็จร่วมกัน ขอเชิญติดตามเรื่องราวด้านวิทยาศาสตร์ ภาษา และกีฬา พร้อมประมวลภาพกิจกรรมในฉบับนี้\n\nข้อมูลทั้งหมดใช้เพื่อทดสอบระบบ ไม่มีผลรับรองการจัดกิจกรรมจริง" };
            for (var i = 0; i < stories.Count; i++) publication.Items.Add(new PublicationItem { ActivityId = stories[i].Id, SortOrder = i, Section = i == 2 ? PublicationService.Sections[2] : PublicationService.Sections[1] });
            db.Add(publication); await db.SaveChangesAsync();
        }
        if (existingPublication == null || publication.Files.Count == 0)
        {
            await new PublicationService(db, new ExportService(), storage).GenerateAsync(publication, admin.Id);
            publication.IsPublic = true; await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        Console.WriteLine($"PR sample ready: 8 announcements (6 current, 1 scheduled, 1 expired when seeded), 3 public stories, publication /publications/{publication.Year + 543}/{publication.Month}. Original private samples unchanged.");
    }
}
