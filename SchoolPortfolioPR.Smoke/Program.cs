using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

var secrets = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "SchoolPortfolioPR-Sar-2026", "secrets.json");
var config = new ConfigurationBuilder().AddJsonFile(secrets).AddEnvironmentVariables().Build();
var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "smoke-test"), new Claim(ClaimTypes.Role, "SuperAdmin")], "test")) } };
if (!args.Contains("--browser-only"))
await using (var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options, http))
{
    await using var transaction = await db.Database.BeginTransactionAsync();
    try {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var teacher = new Teacher { Name = "ครูทดสอบ", Code = "SMOKE-" + tag }; var student = new Student { Name = "นักเรียนทดสอบ", Code = "SMOKE-" + tag }; db.AddRange(teacher, student);
        var a = new Activity { ActivityCode = "SMOKE-" + tag, Slug = "smoke-" + tag, Title = "กิจกรรมทดสอบระบบ", Description = "เนื้อหาภาษาไทยสำหรับการทดสอบ", ActivityDate = DateTime.Today,
            DepartmentId = await db.Set<Department>().Select(x => x.Id).FirstAsync(), CategoryId = await db.Set<ActivityCategory>().Select(x => x.Id).FirstAsync(), AcademicYearId = await db.Set<AcademicYear>().Select(x => x.Id).FirstAsync(),
            FiscalYear = DateTime.Today.Year + 543, Status = WorkflowStatus.Published, IsPublic = true, PublishedDate = DateTime.UtcNow };
        a.Teachers.Add(new ActivityTeacher { Teacher = teacher }); a.Students.Add(new ActivityStudent { Student = student }); db.Add(a); await db.SaveChangesAsync();
        if (a.RowVersion.Length == 0 || !await db.AuditLogs.AnyAsync(x => x.Entity == "Activity" && x.EntityId == a.Id.ToString())) throw new Exception("Audit/concurrency failure");
        var award = new Award { AwardName = "รางวัลทดสอบ", AcademicYearId = a.AcademicYearId, DepartmentId = a.DepartmentId, ActivityId = a.Id, AwardDate = DateTime.Today, Status = WorkflowStatus.Published, IsPublic = true };
        award.Teachers.Add(new AwardTeacher { Teacher = teacher, Role = TeacherRole.Coach }); award.Students.Add(new AwardStudent { Student = student }); db.Add(award); await db.SaveChangesAsync();
        Console.WriteLine("PASS: SQL inserts, relationships, rowversion and audit.");
        var reports = new ReportService(new AccessService(db, http));
        var rows = await reports.BuildAsync(new ReportFilter { Kind = "TeacherPortfolio", TeacherId = teacher.Id });
        if (rows.Count != 2) throw new Exception("Portfolio did not include activity and award");
        var export = new ExportService(); var pdf = await export.PdfAsync(export.ReportHtml("รายงานทดสอบภาษาไทย", rows));
        if (!System.Text.Encoding.ASCII.GetString(pdf, 0, 5).Equals("%PDF-")) throw new Exception("Invalid PDF");
        Console.WriteLine("PASS: teacher portfolio and Thai report PDF.");
        var storage = new SqlFileStorage(db, config); var file = await storage.StoreGeneratedAsync(pdf, "smoke.pdf", "smoke-test"); award.Certificates.Add(new AwardCertificate { File = file }); await db.SaveChangesAsync();
        var roundtrip = await db.FileObjects.AsNoTracking().SingleAsync(x => x.FileId == file.FileId); if (!roundtrip.FileData.SequenceEqual(pdf)) throw new Exception("Binary storage mismatch");
        var publication = new MonthlyPublication { Title = "วารสารทดสอบ", Year = 2399, Month = 12, DirectorMessage = "สารจากผู้อำนวยการ ภาษาไทย" };
        publication.Items.Add(new PublicationItem { Activity = a, Section = PublicationService.Sections[0] }); publication.Items.Add(new PublicationItem { Award = award, Section = PublicationService.Sections[4] }); db.Add(publication); await db.SaveChangesAsync();
        var service = new PublicationService(db, export, storage); await service.GenerateAsync(publication, "smoke-test");
        if (!await db.Set<PublicationFile>().AnyAsync(x => x.MonthlyPublicationId == publication.Id)) throw new Exception("Publication generation failed");
        db.Remove(a); await db.SaveChangesAsync(); if (await db.Activities.AnyAsync(x => x.Id == a.Id) || await db.Set<ActivityTeacher>().AnyAsync(x => x.ActivityId == a.Id)) throw new Exception("Soft delete filter failed");
        Console.WriteLine("PASS: SQL relationships, rowversion, audit, portfolios, binary files, Thai PDF, monthly publication and soft delete. Test transaction rolled back.");
    } finally { await transaction.RollbackAsync(); }
}
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
var baseUrl = args.FirstOrDefault(x => !x.StartsWith("--")) ?? "http://localhost:5093";
var response = await page.GotoAsync(baseUrl); if (response?.Status != 200) throw new Exception("Home did not load");
Directory.CreateDirectory("artifacts"); await page.ScreenshotAsync(new() { Path = "artifacts/home-desktop.png", FullPage = true });
await page.SetViewportSizeAsync(390, 844); await page.ScreenshotAsync(new() { Path = "artifacts/home-mobile.png", FullPage = true });
if (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth + 1")) throw new Exception("Mobile overflow");
await page.SetViewportSizeAsync(1440, 1000);
await page.GotoAsync(baseUrl + "/Account/Login"); await page.Locator("#Email").FillAsync(config["SeedAdmin:Email"]!); await page.Locator("#Password").FillAsync(config["SeedAdmin:Password"]!);
await page.Locator("button[type=submit], form button").ClickAsync(); await page.WaitForURLAsync("**/Dashboard");
if (!args.Contains("--forms-only")) {
foreach (var path in new[] { "/Dashboard", "/Activities", "/Activities/Edit", "/Awards", "/Awards/Edit", "/Portfolios", "/Reports", "/Publications", "/Publications/Edit", "/Masters?kind=Teacher", "/Masters/Edit?kind=Teacher", "/Masters?kind=SchoolProfile", "/Users", "/Users/Edit", "/Audit" }) {
    response = await page.GotoAsync(baseUrl + path); if (response?.Status != 200 || await page.Locator("body").InnerTextAsync() is var text && text.Contains("ไม่สามารถดำเนินการได้ในขณะนี้")) throw new Exception("Page failed: " + path);
    Console.WriteLine("PASS page: " + path);
}
await page.GotoAsync(baseUrl + "/Dashboard"); await page.ScreenshotAsync(new() { Path = "artifacts/dashboard.png", FullPage = true });
await page.SetViewportSizeAsync(390, 844); await page.ScreenshotAsync(new() { Path = "artifacts/dashboard-mobile.png", FullPage = true });
if (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth + 1")) throw new Exception("Dashboard mobile overflow");
await page.GotoAsync(baseUrl + "/Reports");
foreach (var format in new[] { "pdf", "docx", "xlsx" }) {
    if (format == "pdf") {
        await page.Locator("[data-preview-report]").ClickAsync();
        await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new() { Timeout = 60000 });
    }
    var download = await page.RunAndWaitForDownloadAsync(() => format == "pdf" ? page.Locator("#mediaDownload").ClickAsync() : page.Locator($"button[value='{format}']").ClickAsync());
    if (await download.FailureAsync() != null) throw new Exception("Export failed: " + format);
    await download.SaveAsAsync("artifacts/report." + format); Console.WriteLine("PASS download: " + format);
    if (format == "pdf") { await page.GetByRole(AriaRole.Button, new() { Name = "ปิดตัวอย่าง", Exact = true }).ClickAsync(); await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync(); }
}
var csrf = await page.APIRequest.PostAsync(baseUrl + "/Activities/Delete", new() { Form = page.APIRequest.CreateFormData().Set("id", "0") });
if (csrf.Status != 400) throw new Exception("CSRF request was not rejected");
}
await using var anonymous = await browser.NewContextAsync(); var anon = await anonymous.NewPageAsync();
await anon.GotoAsync(baseUrl + "/Dashboard"); if (!anon.Url.Contains("/Account/Login")) throw new Exception("Anonymous dashboard access");
if (errors.Count > 0) throw new Exception("Browser errors: " + string.Join(";", errors));
Console.WriteLine("PASS: desktop/mobile pages, login, report downloads, CSRF and anonymous authorization. Screenshots in artifacts/.");

// These records must be committed so the separate web process can read them.
// Finally removes only this run's exact IDs and their own audit rows.
var token = "SMOKE-UI-" + Guid.NewGuid().ToString("N")[..8];
int? activityId = null, publicationId = null; var ownedFileIds = new List<Guid>();
await using var cleanup = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options, http);
try {
    await page.SetViewportSizeAsync(1440, 1000);
    await page.GotoAsync(baseUrl + "/Activities/Edit");
    await page.Locator("#ActivityCode").FillAsync(token); await page.Locator("#Title").FillAsync("ทดสอบฟอร์มกิจกรรม " + token);
    await page.Locator("#Description").FillAsync("รายละเอียดทดสอบภาษาไทย <script>ไม่ควรถูกประมวลผล</script>"); 
    await page.Locator("#CategoryId").SelectOptionAsync(new SelectOptionValue { Index = 1 }); await page.Locator("#DepartmentId").SelectOptionAsync(new SelectOptionValue { Index = 1 });
    await page.Locator("#AcademicYearId").SelectOptionAsync(new SelectOptionValue { Index = 1 });
    await page.Locator("button").Filter(new() { HasText = "บันทึก" }).ClickAsync();
    await page.WaitForURLAsync("**/activity/*"); activityId = int.Parse(new Uri(page.Url).Segments.Last());
    await page.Locator("text=รายละเอียดทดสอบภาษาไทย").First.WaitForAsync();
    var imageBytes = await page.Locator("h1").ScreenshotAsync();
    await page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = token + ".png", MimeType = "image/png", Buffer = imageBytes });
    await page.GetByRole(AriaRole.Button, new() { Name = "อัปโหลด", Exact = true }).ClickAsync(); await page.WaitForLoadStateAsync();
    await page.Locator(".gallery [data-media-preview]").First.ClickAsync();
    await Assertions.Expect(page.Locator("#mediaSurface img")).ToBeVisibleAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "ปิดตัวอย่าง", Exact = true }).ClickAsync();
    await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync();
    await page.Locator("summary").Filter(new() { HasText = "จัดการภาพปกและลำดับภาพ" }).ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "บันทึกลำดับและภาพปก" }).WaitForAsync();
    ownedFileIds.AddRange(await cleanup.Set<ActivityImage>().Where(x => x.ActivityId == activityId).Select(x => x.FileId).ToListAsync());
    if (ownedFileIds.Count != 1) throw new Exception("Image upload not persisted");
    var privateFile = await anon.APIRequest.GetAsync(baseUrl + "/files/" + ownedFileIds[0]); if (privateFile.Status != 404) throw new Exception("Private image leaked");
    await page.GetByRole(AriaRole.Button, new() { Name = "บันทึกลำดับและภาพปก" }).ClickAsync(); await page.WaitForLoadStateAsync();
    foreach (var status in new[] { "ส่งตรวจ", "ตรวจแล้ว", "อนุมัติ", "เผยแพร่" }) { await page.GetByRole(AriaRole.Button, new() { Name = status, Exact = true }).ClickAsync(); await page.WaitForLoadStateAsync(); }
    await page.ScreenshotAsync(new() { Path = "artifacts/activity.png", FullPage = true });
    var privateActivity = await anon.APIRequest.GetAsync(baseUrl + "/activity/" + activityId); if (privateActivity.Status != 404) throw new Exception("Nonpublic published activity leaked");
    Console.WriteLine("PASS: activity form, upload, cover/order, workflow and private file authorization.");
    await page.GotoAsync(baseUrl + "/Publications/Edit?year=2398&month=11");
    await page.Locator("#Title").FillAsync("ทดสอบวารสาร " + token); await page.Locator("#DirectorMessage").FillAsync("สารจากผู้อำนวยการ ทดสอบภาษาไทย");
    await page.GetByRole(AriaRole.Button, new() { Name = "บันทึกวารสาร", Exact = true }).ClickAsync(); await page.WaitForURLAsync("**/Publications/Manage/*");
    publicationId = int.Parse(new Uri(page.Url).Segments.Last());
    await page.GetByRole(AriaRole.Button, new() { Name = "สร้าง PDF และบันทึกการเผยแพร่", Exact = true }).ClickAsync();
    await page.WaitForURLAsync("**/publications/2941/11", new() { Timeout = 90000 });
    await page.Locator("#pageCount").Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex(@"^\d+$") }).WaitForAsync();
    await page.Locator("#toc option").First.WaitForAsync();
    await Assertions.Expect(page.Locator("#toc")).ToContainTextAsync("สารจากผู้อำนวยการ");
    if (int.Parse(await page.Locator("#pageCount").InnerTextAsync()) < 11) throw new Exception("Publication sections missing");
    await page.Locator("#next").ClickAsync(); await Assertions.Expect(page.Locator("#pageNumber")).ToHaveValueAsync("2");
    await page.Locator("#zoomIn").ClickAsync(); await page.Locator("#previous").ClickAsync(); await Assertions.Expect(page.Locator("#pageNumber")).ToHaveValueAsync("1");
    await page.ScreenshotAsync(new() { Path = "artifacts/flipbook.png", FullPage = true });
    await page.Locator("#flipbook [data-media-preview][data-media-type=pdf]").ClickAsync();
    await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new() { Timeout = 60000 });
    await page.GetByRole(AriaRole.Button, new() { Name = "ปิดตัวอย่าง", Exact = true }).ClickAsync();
    await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync();
    await page.GotoAsync(baseUrl + "/Publications");
    await page.Locator($"[data-media-preview][data-media-title='ทดสอบวารสาร {token}.pdf']").ClickAsync();
    await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new() { Timeout = 60000 });
    await page.GetByRole(AriaRole.Button, new() { Name = "ปิดตัวอย่าง", Exact = true }).ClickAsync();
    Console.WriteLine("PASS: publication form, generation, PDF.js, table of contents, next/previous and zoom.");
    if (errors.Count > 0) throw new Exception("Browser errors: " + string.Join(";", errors));
} finally {
    activityId ??= await cleanup.Activities.IgnoreQueryFilters().Where(x => x.ActivityCode == token).Select(x => (int?)x.Id).SingleOrDefaultAsync();
    publicationId ??= await cleanup.Publications.IgnoreQueryFilters().Where(x => x.Title == "ทดสอบวารสาร " + token).Select(x => (int?)x.Id).SingleOrDefaultAsync();
    await using var tx = await cleanup.Database.BeginTransactionAsync();
    if (publicationId.HasValue) {
        ownedFileIds.AddRange(await cleanup.Set<PublicationFile>().IgnoreQueryFilters().Where(x => x.MonthlyPublicationId == publicationId).Select(x => x.FileId).ToListAsync());
        await cleanup.Set<PublicationFile>().IgnoreQueryFilters().Where(x => x.MonthlyPublicationId == publicationId).ExecuteDeleteAsync();
        await cleanup.Set<PublicationItem>().IgnoreQueryFilters().Where(x => x.MonthlyPublicationId == publicationId).ExecuteDeleteAsync();
        await cleanup.Publications.IgnoreQueryFilters().Where(x => x.Id == publicationId && x.Title == "ทดสอบวารสาร " + token).ExecuteDeleteAsync();
    }
    if (activityId.HasValue) {
        ownedFileIds.AddRange(await cleanup.Set<ActivityImage>().IgnoreQueryFilters().Where(x => x.ActivityId == activityId).Select(x => x.FileId).ToListAsync());
        await cleanup.Set<ActivityImage>().IgnoreQueryFilters().Where(x => x.ActivityId == activityId).ExecuteDeleteAsync();
        await cleanup.Set<ActivityFile>().IgnoreQueryFilters().Where(x => x.ActivityId == activityId).ExecuteDeleteAsync();
        await cleanup.Activities.IgnoreQueryFilters().Where(x => x.Id == activityId && x.ActivityCode == token).ExecuteDeleteAsync();
    }
    foreach (var id in ownedFileIds.Distinct()) await cleanup.FileObjects.IgnoreQueryFilters().Where(x => x.FileId == id).ExecuteDeleteAsync();
    var ownIds = ownedFileIds.Select(x => x.ToString()).ToArray();
    await cleanup.AuditLogs.Where(x => x.Entity == "Activity" && x.EntityId == activityId.ToString() || x.Entity == "MonthlyPublication" && x.EntityId == publicationId.ToString() || x.Entity == "FileObject" && ownIds.Contains(x.EntityId) || x.Entity == "ActivityImage" && x.EntityId.StartsWith(activityId + ",") || x.Entity == "PublicationFile" && x.EntityId.StartsWith(publicationId + ",")).ExecuteDeleteAsync();
    await tx.CommitAsync(); Console.WriteLine("Removed this run's temporary browser records and files.");
}
