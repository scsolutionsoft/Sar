using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

var secrets = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "SchoolPortfolioPR-Sar-2026", "secrets.json");
var config = new ConfigurationBuilder().AddJsonFile(secrets, optional: true).AddEnvironmentVariables().Build();
var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options, http);
var codes = Enumerable.Range(1, 10).Select(i => $"DEMO-ACT-{i:000}").ToArray();
if (args.Contains("--pr-demo"))
{
    await PublicRelationsSample.RunAsync(db, config, http);
    return;
}
if (args.Contains("--images") || args.Contains("--verify-images"))
{
    await SampleImages.RunAsync(db, config, http, codes, args);
    return;
}
if (!args.Contains("--verify"))
{
    var adminEmail = config["SeedAdmin:Email"];
    var admin = await db.Users.SingleAsync(x => x.Email == adminEmail);
    http.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, admin.Id), new Claim(ClaimTypes.Role, "SuperAdmin")], "seed"));
    await using var transaction = await db.Database.BeginTransactionAsync();
    async Task<T> Named<T>(string name) where T : NamedRecord, new()
    {
        var existing = db.Set<T>().Local.FirstOrDefault(x => x.Name == name && !x.IsDeleted) ?? await db.Set<T>().FirstOrDefaultAsync(x => x.Name == name);
        if (existing != null) return existing;
        var item = new T { Name = name }; db.Add(item); return item;
    }
    var departments = new[] { await Named<Department>("ฝ่ายวิชาการ (ตัวอย่าง)"), await Named<Department>("ฝ่ายกิจการนักเรียน (ตัวอย่าง)"), await Named<Department>("ฝ่ายบริหารทั่วไป (ตัวอย่าง)") };
    var subjects = new[] { await Named<SubjectGroup>("วิทยาศาสตร์และเทคโนโลยี (ตัวอย่าง)"), await Named<SubjectGroup>("คณิตศาสตร์ (ตัวอย่าง)"), await Named<SubjectGroup>("ภาษาไทย (ตัวอย่าง)"), await Named<SubjectGroup>("ภาษาต่างประเทศ (ตัวอย่าง)"), await Named<SubjectGroup>("สุขศึกษาและพลศึกษา (ตัวอย่าง)") };
    var categories = new[] { await Named<ActivityCategory>("วิชาการ"), await Named<ActivityCategory>("กิจกรรมนักเรียน"), await Named<ActivityCategory>("ข่าวโรงเรียน") };
    var tag = await Named<Tag>("ข้อมูลตัวอย่าง");
    var today = DateTime.Today;
    var schoolYear = today.Month >= 5 ? today.Year : today.Year - 1;
    var academicYear = await db.Set<AcademicYear>().FirstOrDefaultAsync(x => x.StartDate <= today && x.EndDate >= today);
    if (academicYear == null) { academicYear = new AcademicYear { Name = (schoolYear + 543).ToString(), StartDate = new(schoolYear, 5, 1), EndDate = new(schoolYear + 1, 4, 30) }; db.Add(academicYear); }
    var teachers = new List<Teacher>(); var students = new List<Student>();
    for (var i = 0; i < 5; i++)
    {
        var code = $"DEMO-T-{i + 1:000}";
        var teacher = await db.Set<Teacher>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Code == code);
        if (teacher == null) { teacher = new Teacher { Code = code, Name = $"ครูตัวอย่าง {i + 1:00}", Department = departments[i % 3], SubjectGroup = subjects[i] }; db.Add(teacher); }
        if (teacher.IsDeleted) throw new InvalidOperationException($"{code} was deleted; sample seeding will not restore it automatically.");
        teachers.Add(teacher);
    }
    for (var i = 0; i < 10; i++)
    {
        var code = $"DEMO-S-{i + 1:000}";
        var student = await db.Set<Student>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Code == code);
        if (student == null) { student = new Student { Code = code, Name = $"นักเรียนตัวอย่าง {i + 1:00}", Classroom = $"ม.{i % 3 + 1}/{i % 2 + 1}", Department = departments[1] }; db.Add(student); }
        if (student.IsDeleted) throw new InvalidOperationException($"{code} was deleted; sample seeding will not restore it automatically.");
        students.Add(student);
    }
    await db.SaveChangesAsync();
    string[] titles = ["ค่ายวิทยาศาสตร์และนวัตกรรม", "แข่งขันคิดเลขเร็ว", "วันภาษาไทยแห่งชาติ", "ค่ายภาษาอังกฤษเพื่อการสื่อสาร", "กีฬาสีสัมพันธ์", "โครงการจิตอาสาพัฒนาโรงเรียน", "อบรมการใช้สื่อดิจิทัลอย่างปลอดภัย", "นิทรรศการโครงงานนักเรียน", "กิจกรรมห้องสมุดมีชีวิต", "ปลูกต้นไม้เพิ่มพื้นที่สีเขียว"];
    string[] summaries = ["เรียนรู้การทดลองและออกแบบสิ่งประดิษฐ์จากวัสดุใกล้ตัว", "ฝึกทักษะการคำนวณและแก้ปัญหาผ่านการแข่งขันแบบทีม", "ส่งเสริมการอ่าน การเขียน และการใช้ภาษาไทยอย่างสร้างสรรค์", "ฝึกฟัง พูด อ่าน และเขียนภาษาอังกฤษผ่านสถานการณ์จำลอง", "พัฒนาทักษะกีฬา น้ำใจนักกีฬา และการทำงานร่วมกัน", "นักเรียนร่วมปรับปรุงพื้นที่ส่วนรวมและฝึกความรับผิดชอบ", "รู้เท่าทันข่าวปลอม ปกป้องข้อมูลส่วนตัว และใช้อินเทอร์เน็ตอย่างเหมาะสม", "นำเสนอผลงานการเรียนรู้และแลกเปลี่ยนแนวคิดระหว่างกลุ่มสาระ", "สร้างนิสัยรักการอ่านผ่านกิจกรรมเล่านิทานและแนะนำหนังสือ", "เรียนรู้สิ่งแวดล้อมด้วยการปลูกและดูแลต้นไม้ในโรงเรียน"];
    string[] locations = ["ห้องปฏิบัติการวิทยาศาสตร์", "ห้องคณิตศาสตร์", "หอประชุม", "ห้องภาษาอังกฤษ", "สนามกีฬา", "ลานกิจกรรม", "ห้องคอมพิวเตอร์", "อาคารอเนกประสงค์", "ห้องสมุด", "สวนการเรียนรู้"];
    WorkflowStatus[] statuses = [WorkflowStatus.Draft, WorkflowStatus.Submitted, WorkflowStatus.Reviewed, WorkflowStatus.Approved, WorkflowStatus.Published, WorkflowStatus.Draft, WorkflowStatus.Submitted, WorkflowStatus.Published, WorkflowStatus.Rejected, WorkflowStatus.Archived];
    int[] offsets = [0, 2, 5, 8, 12, 16, 22, 30, 38, 48];
    int[] departmentIndices = [0, 0, 0, 0, 1, 1, 2, 0, 0, 2];
    var exporter = new ExportService(); var storage = new SqlFileStorage(db, config);
    var created = 0;
    for (var i = 0; i < 10; i++)
    {
        // Keep existing examples, including their edits and soft-deleted state.
        if (await db.Activities.IgnoreQueryFilters().AnyAsync(x => x.ActivityCode == codes[i])) continue;
        var date = today.AddDays(-offsets[i]);
        var activity = new Activity {
            ActivityCode = codes[i], Slug = $"demo-activity-{i + 1:000}", Title = $"[ตัวอย่าง] {titles[i]}", ShortDescription = summaries[i],
            Description = $"ข้อมูลสมมติสำหรับทดสอบระบบ ไม่ใช่กิจกรรมหรือผลการดำเนินงานจริง\n\nวัตถุประสงค์\n{summaries[i]}\n\nรูปแบบกิจกรรม\n1. ชี้แจงและแบ่งกลุ่มผู้เข้าร่วม\n2. ลงมือปฏิบัติและแลกเปลี่ยนเรียนรู้\n3. สรุปผลและประเมินความพึงพอใจ\n\nผลที่คาดว่าจะได้รับ\nผู้เข้าร่วมพัฒนาทักษะที่เกี่ยวข้อง ฝึกการทำงานเป็นทีม และสามารถนำความรู้ไปประยุกต์ใช้ได้",
            ActivityDate = date, EndDate = date, Location = locations[i], Organizer = "คณะทำงานกิจกรรม (ข้อมูลตัวอย่าง)",
            Department = departments[departmentIndices[i]], SubjectGroup = subjects[i % 5], Category = categories[i < 4 || i == 7 ? 0 : i == 6 ? 2 : 1], AcademicYear = academicYear,
            FiscalYear = date.Year + 543 + (date.Month >= 10 ? 1 : 0), IsProject = i is 5 or 7 or 9, IsFeatured = i is 0 or 7, IsPublic = false, Status = statuses[i],
            PublishedDate = statuses[i] is WorkflowStatus.Published or WorkflowStatus.Archived ? DateTime.SpecifyKind(date.AddHours(3), DateTimeKind.Utc) : null
        };
        activity.Teachers.Add(new ActivityTeacher { Teacher = teachers[i % 5], Role = activity.IsProject ? TeacherRole.ProjectOwner : TeacherRole.Coach });
        activity.Teachers.Add(new ActivityTeacher { Teacher = teachers[(i + 1) % 5], Role = TeacherRole.Advisor });
        for (var j = 0; j < 3; j++) activity.Students.Add(new ActivityStudent { Student = students[(i + j) % 10] });
        activity.Tags.Add(new ActivityTag { Tag = tag });
        if (i is 1 or 3 or 4 or 7)
        {
            var award = new Award { AwardName = $"[ตัวอย่าง] รางวัล{titles[i]}", CompetitionName = $"เวทีทักษะ {titles[i]} (สมมติ)", AwardLevel = i switch { 1 => AwardLevel.Provincial, 3 => AwardLevel.Regional, 4 => AwardLevel.School, _ => AwardLevel.National },
                AwardRank = i % 2 == 0 ? "รองชนะเลิศอันดับ 1" : "เหรียญทอง", Organizer = "หน่วยงานตัวอย่าง", AwardDate = date, AcademicYear = academicYear, Department = activity.Department, SubjectGroup = activity.SubjectGroup,
                Description = "ข้อมูลรางวัลและเกียรติบัตรสมมติ ใช้ทดสอบแฟ้มผลงานและรายงานเท่านั้น", Status = activity.Status, IsPublic = false, PublishedDate = activity.PublishedDate };
            award.Teachers.Add(new AwardTeacher { Teacher = teachers[i % 5], Role = TeacherRole.Coach });
            award.Students.Add(new AwardStudent { Student = students[i] }); award.Students.Add(new AwardStudent { Student = students[(i + 1) % 10] });
            var html = ExportService.Document("เกียรติบัตรตัวอย่าง", $"<h1>เกียรติบัตรตัวอย่าง — ไม่มีผลรับรองจริง</h1><h2>{ExportService.Escape(award.AwardName)}</h2><p>{ExportService.Escape(award.AwardRank)} · ระดับ{Thai.Level(award.AwardLevel)}</p><p>มอบให้ นักเรียนตัวอย่าง {i + 1:00} และนักเรียนตัวอย่าง {(i + 1) % 10 + 1:00}</p><p>{Thai.Date(date)}</p><p>เอกสารสมมติสำหรับทดสอบระบบ SchoolPortfolioPR</p>");
            var file = await storage.StoreGeneratedAsync(await exporter.PdfAsync(html), $"demo-certificate-{i + 1:000}.pdf", admin.Id);
            award.Certificates.Add(new AwardCertificate { File = file }); activity.Awards.Add(award);
        }
        if (i == 0)
        {
            List<ReportRow> rows = [new("กิจกรรมตัวอย่าง", titles[i], date, activity.Department.Name, activity.Category.Name, "ครูและนักเรียนตัวอย่าง")];
            activity.Files.Add(new ActivityFile { File = await storage.StoreGeneratedAsync(await exporter.PdfAsync(exporter.ReportHtml("กำหนดการกิจกรรมตัวอย่าง", rows)), "demo-agenda.pdf", admin.Id) });
            async Task Attach(byte[] data, string name, string mime)
            {
                using var stream = new MemoryStream(data); var upload = new FormFile(stream, 0, data.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = mime };
                activity.Files.Add(new ActivityFile { File = await storage.StoreAsync(upload, admin.Id) });
            }
            await Attach(exporter.Word("รายละเอียดกิจกรรมตัวอย่าง", rows), "demo-details.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            await Attach(exporter.Excel("สรุปกิจกรรมตัวอย่าง", rows), "demo-summary.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        db.Add(activity); await db.SaveChangesAsync(); created++;
    }
    await transaction.CommitAsync(); Console.WriteLine($"Created {created} sample activities. Existing records were kept unchanged.");
}
var activities = await db.Activities.AsNoTracking().Where(x => codes.Contains(x.ActivityCode)).OrderBy(x => x.ActivityCode).Select(x => new { x.Id, x.ActivityCode, x.Title, x.Status, x.IsPublic, Teachers = x.Teachers.Count, Students = x.Students.Count, Files = x.Files.Count, Awards = x.Awards.Count }).ToListAsync();
if (activities.Count != 10 || activities.Any(x => x.IsPublic || x.Teachers != 2 || x.Students != 3)) throw new InvalidOperationException("Sample activity verification did not match expected counts or visibility.");
var activityIds = activities.Select(x => x.Id).ToArray();
var awardsCount = await db.Awards.CountAsync(x => x.ActivityId.HasValue && activityIds.Contains(x.ActivityId.Value));
var certificatesCount = await db.Set<AwardCertificate>().CountAsync(x => x.Award.ActivityId.HasValue && activityIds.Contains(x.Award.ActivityId.Value));
Console.WriteLine(JsonSerializer.Serialize(new { Activities = activities.Count, Awards = awardsCount, Certificates = certificatesCount, Teachers = await db.Set<Teacher>().CountAsync(x => x.Code.StartsWith("DEMO-T-")), Students = await db.Set<Student>().CountAsync(x => x.Code.StartsWith("DEMO-S-")), Items = activities }, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
if (args.Contains("--verify-ui"))
{
    using var playwright = await Playwright.CreateAsync(); await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
    var page = await browser.NewPageAsync(); var baseUrl = args.FirstOrDefault(x => x.StartsWith("http")) ?? "http://localhost:5093";
    await page.GotoAsync(baseUrl + "/Account/Login"); await page.Locator("#Email").FillAsync(config["SeedAdmin:Email"]!); await page.Locator("#Password").FillAsync(config["SeedAdmin:Password"]!);
    await page.Locator("form button").ClickAsync(); await page.WaitForURLAsync("**/Dashboard");
    await page.GotoAsync(baseUrl + "/Activities?q=DEMO-ACT-"); await Assertions.Expect(page.Locator("tbody tr")).ToHaveCountAsync(10);
    await page.GotoAsync(baseUrl + "/activity/" + activities[0].Id); await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("[ตัวอย่าง]");
    foreach (var extension in new[] { "pdf", "docx", "xlsx" }) await Assertions.Expect(page.Locator($"a:has-text('demo-{(extension == "pdf" ? "agenda" : extension == "docx" ? "details" : "summary")}.{extension}')")).ToHaveCountAsync(1);
    await using var anonymous = await browser.NewContextAsync(); var request = await anonymous.APIRequest.GetAsync(baseUrl + "/activity/" + activities[4].Id); if (request.Status != 404) throw new InvalidOperationException("Sample activity was publicly accessible.");
    Console.WriteLine("UI verified: 10 sample rows, activity details, three attachments, and anonymous access denied.");
}
