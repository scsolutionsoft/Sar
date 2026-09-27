using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;

internal static class StudentAwardChecks
{
    public static async Task RunAsync()
    {
        const string root = "http://localhost:5093";
        var accounts = JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "SchoolPortfolioPR-Sar-2026", "secrets.json")).Build();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options, http);
        var credentials = accounts.Where(x => x.Role == "Student").OrderBy(x => x.Email).Take(2).ToArray();
        var users = new[] { await db.Users.Include(x => x.Student).SingleAsync(x => x.Email == credentials[0].Email), await db.Users.Include(x => x.Student).SingleAsync(x => x.Email == credentials[1].Email) };
        var admin = accounts.First(x => x.Role == "SuperAdmin");
        var marker = "AWARD-ACL-" + Guid.NewGuid().ToString("N")[..10];
        var department = await db.Set<Department>().Select(x => x.Id).FirstAsync();
        var year = await db.Set<AcademicYear>().Select(x => x.Id).FirstAsync();
        var source = await db.FileObjects.AsNoTracking().FirstAsync(x => x.ContentType == "application/pdf");
        FileObject Binary(string name) => new() { OriginalFileName = marker + name + ".pdf", StoredFileName = Guid.NewGuid() + ".pdf", FileData = source.FileData, FileSize = source.FileSize, ContentType = source.ContentType, FileExtension = source.FileExtension, FileHash = source.FileHash, UploadedBy = "acl-test" };
        Award Award(string name, bool isPublic, WorkflowStatus status, params int[] recipients) => new() { AwardName = marker + name, CompetitionName = marker, DepartmentId = department, AcademicYearId = year, AwardDate = DateTime.Today, Status = status, IsPublic = isPublic, Students = recipients.Select(i => new AwardStudent { StudentId = users[i].StudentId!.Value }).ToList() };
        var awards = new[] { Award("-own-private", false, WorkflowStatus.Published, 0), Award("-other-public", true, WorkflowStatus.Published, 1), Award("-shared", true, WorkflowStatus.Published, 0, 1), Award("-draft", true, WorkflowStatus.Draft, 0) };
        var files = new[] { Binary("-student-one"), Binary("-student-two"), Binary("-unassigned"), Binary("-magazine") };
        awards[2].Certificates = new List<AwardCertificate> { new() { StudentId = users[0].StudentId, File = files[0] }, new() { StudentId = users[1].StudentId, File = files[1] }, new() { File = files[2] } };
        var pubYear = 2399;
        while (await db.Publications.AnyAsync(x => x.Year == pubYear && x.Month == 12)) pubYear--;
        var publication = new MonthlyPublication { Title = marker + "-magazine", Year = pubYear, Month = 12, IsPublic = true, Items = new List<PublicationItem> { new() { Award = awards[2] } }, Files = new List<PublicationFile> { new() { File = files[3] } } };
        db.AddRange(awards); db.Add(publication); await db.SaveChangesAsync();
        using var playwright = await Playwright.CreateAsync(); await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var contexts = new List<IBrowserContext>(); var errors = new List<string>();
        async Task<(IBrowserContext Context, IPage Page)> Login(TestCredential account)
        {
            var c = await browser.NewContextAsync(); contexts.Add(c); var p = await c.NewPageAsync(); p.PageError += (_, e) => errors.Add(e);
            await p.GotoAsync(root + "/Account/Login"); await p.Locator("#Email").FillAsync(account.Email); await p.Locator("#Password").FillAsync(account.Password); await p.Locator("form button").ClickAsync(); await p.WaitForURLAsync(root + (account.Role == "Student" ? "/Admissions" : "/Dashboard")); return (c, p);
        }
        async Task Status(IBrowserContext c, string path, int expected)
        {
            var r = await c.APIRequest.GetAsync(root + path); if (r.Status != expected) throw new Exception($"{path}: expected {expected}, got {r.Status}");
        }
        try
        {
            var students = new[] { await Login(credentials[0]), await Login(credentials[1]) };
            for (var i = 0; i < 2; i++)
            {
                var (c, p) = students[i];
                await p.GotoAsync(root + "/Awards?q=" + marker);
                await Assertions.Expect(p.Locator("tbody tr")).ToHaveCountAsync(2);
                await Assertions.Expect(p.Locator("main")).ToContainTextAsync(awards[i].AwardName);
                await Assertions.Expect(p.Locator("main")).Not.ToContainTextAsync(awards[1-i].AwardName);
                await Status(c, "/Awards/Details/" + awards[i].Id, 200); await Status(c, "/Awards/Details/" + awards[1-i].Id, 404); await Status(c, "/Awards/Details/" + awards[3].Id, 404);
                await p.GotoAsync(root + "/Awards/Details/" + awards[2].Id);
                await Assertions.Expect(p.Locator("main")).ToContainTextAsync(users[i].Student!.Name);
                await Assertions.Expect(p.Locator("main")).Not.ToContainTextAsync(users[1-i].Student!.Name);
                await Assertions.Expect(p.Locator(".file-name")).ToHaveCountAsync(1);
                await Assertions.Expect(p.Locator(".file-name")).ToContainTextAsync(files[i].OriginalFileName);
                foreach (var download in new[] { "", "?download=true" })
                {
                    await Status(c, "/files/" + files[i].FileId + download, 200);
                    await Status(c, "/files/" + files[1-i].FileId + download, 404);
                    await Status(c, "/files/" + files[2].FileId + download, 404);
                }
                await p.GotoAsync(root + "/"); await Assertions.Expect(p.Locator("main")).Not.ToContainTextAsync(awards[1-i].AwardName);
                var report = await c.APIRequest.GetAsync(root + "/Reports?Kind=AwardsByYear&format=print&StudentId=" + users[1-i].StudentId);
                var html = WebUtility.HtmlDecode(await report.TextAsync());
                if (report.Status != 200 || !html.Contains(awards[i].AwardName) || html.Contains(awards[1-i].AwardName) || html.Contains(users[1-i].Student!.Name)) throw new Exception("Report exposed another recipient or accepted forged StudentId.");
                await Status(c, "/Reports?Kind=AwardsByYear&format=xlsx", 200);
                await Status(c, "/Dashboard", 200);
                await Status(c, $"/publications/{publication.Year+543}/12", 404); await Status(c, "/Publications/Qr/" + publication.Id, 404); await Status(c, "/files/" + files[3].FileId, 404);
                await p.GotoAsync(root + "/Publications"); await Assertions.Expect(p.Locator("main")).Not.ToContainTextAsync(publication.Title);
                Console.WriteLine($"PASS student {i+1}: own awards, search, group recipient names, file preview/download, homepage, dashboard, forged report filters, Excel and compiled magazine restrictions.");
            }
            var (adminContext, adminPage) = await Login(admin);
            await adminPage.GotoAsync(root + "/Awards/Details/" + awards[2].Id);
            await Assertions.Expect(adminPage.Locator(".file-name")).ToHaveCountAsync(3);
            var form = adminPage.Locator($"[data-certificate-assignment='{files[2].FileId}']");
            var token = await form.Locator("input[name=__RequestVerificationToken]").InputValueAsync();
            var version = await form.Locator("input[name=rowVersion]").InputValueAsync();
            var invalid = adminContext.APIRequest.CreateFormData().Set("__RequestVerificationToken", token).Set("id", awards[2].Id.ToString()).Set("fileId", files[2].FileId.ToString()).Set("rowVersion", version).Set("studentId", "2147483647");
            if ((await adminContext.APIRequest.PostAsync(root + "/Awards/AssignCertificate", new() { Form = invalid })).Status != 400) throw new Exception("Unenrolled recipient accepted.");
            await form.Locator("select[name=studentId]").SelectOptionAsync(users[0].StudentId!.Value.ToString());
            await form.Locator("button").ClickAsync(); await adminPage.WaitForURLAsync("**/Awards/Details/" + awards[2].Id);
            await Status(students[0].Context, "/files/" + files[2].FileId, 200); await Status(students[1].Context, "/files/" + files[2].FileId, 404);
            invalid.Set("studentId", users[1].StudentId!.Value.ToString());
            if ((await adminContext.APIRequest.PostAsync(root + "/Awards/AssignCertificate", new() { Form = invalid })).Status != 409) throw new Exception("Stale assignment version accepted.");
            await adminPage.GotoAsync(root + "/Awards/Details/" + awards[2].Id);
            await adminPage.Locator("#certificateStudentId").SelectOptionAsync(users[1].StudentId!.Value.ToString());
            await adminPage.Locator("#uploadFiles").SetInputFilesAsync(new FilePayload { Name = marker + "-uploaded.pdf", MimeType = "application/pdf", Buffer = source.FileData });
            await adminPage.Locator("form[enctype='multipart/form-data'] button").ClickAsync(); await adminPage.WaitForURLAsync("**/Awards/Details/" + awards[2].Id);
            var uploaded = await db.Set<AwardCertificate>().AsNoTracking().SingleAsync(x => x.AwardId == awards[2].Id && x.File.OriginalFileName == marker + "-uploaded.pdf");
            if (uploaded.StudentId != users[1].StudentId) throw new Exception("Upload lost recipient.");
            await Status(students[0].Context, "/files/" + uploaded.FileId, 404); await Status(students[1].Context, "/files/" + uploaded.FileId, 200);
            await Status(adminContext, "/files/" + files[3].FileId, 200);
            var studentPage = students[0].Page; await studentPage.GotoAsync(root + "/Documents/Edit");
            var studentToken = await studentPage.Locator("main input[name=__RequestVerificationToken]").InputValueAsync();
            var forbidden = students[0].Context.APIRequest.CreateFormData().Set("__RequestVerificationToken", studentToken).Set("id", awards[2].Id.ToString());
            if ((await students[0].Context.APIRequest.PostAsync(root + "/Awards/AssignCertificate", new() { Form = forbidden })).Status != 403) throw new Exception("Student changed a recipient.");
            await db.Set<AwardStudent>().Where(x => x.AwardId == awards[2].Id && x.StudentId == users[0].StudentId).ExecuteDeleteAsync();
            await Status(students[0].Context, "/Awards/Details/" + awards[2].Id, 404); await Status(students[0].Context, "/files/" + files[0].FileId, 404);
            http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "missing-student-user"), new Claim(ClaimTypes.Role, "Student") }, "test"));
            var access = new AccessService(db, http);
            if (await (await access.AwardsAsync()).AnyAsync() || await (await access.CertificatesAsync()).AnyAsync()) throw new Exception("Unlinked student did not fail closed.");
            if (errors.Count != 0) throw new Exception(string.Join("\n", errors));
            Console.WriteLine("PASS admin recipient assignment/upload, invalid recipient, stale version, student POST denial, membership revocation, missing student mapping and no browser errors.");
        }
        finally
        {
            foreach (var c in contexts) await c.DisposeAsync();
            var ids = awards.Select(x => x.Id).ToArray();
            var fileIds = await db.Set<AwardCertificate>().IgnoreQueryFilters().Where(x => ids.Contains(x.AwardId)).Select(x => x.FileId).ToListAsync(); fileIds.Add(files[3].FileId);
            await db.Set<PublicationFile>().IgnoreQueryFilters().Where(x => x.MonthlyPublicationId == publication.Id).ExecuteDeleteAsync();
            await db.Set<PublicationItem>().IgnoreQueryFilters().Where(x => x.MonthlyPublicationId == publication.Id).ExecuteDeleteAsync();
            await db.Publications.IgnoreQueryFilters().Where(x => x.Id == publication.Id && x.Title == publication.Title).ExecuteDeleteAsync();
            await db.Set<AwardCertificate>().IgnoreQueryFilters().Where(x => ids.Contains(x.AwardId)).ExecuteDeleteAsync();
            await db.Set<AwardStudent>().IgnoreQueryFilters().Where(x => ids.Contains(x.AwardId)).ExecuteDeleteAsync();
            await db.Awards.IgnoreQueryFilters().Where(x => ids.Contains(x.Id) && x.CompetitionName == marker).ExecuteDeleteAsync();
            await db.FileObjects.IgnoreQueryFilters().Where(x => fileIds.Contains(x.FileId)).ExecuteDeleteAsync();
        }
        Console.WriteLine("PASS student award isolation; temporary fixtures cleaned.");
    }
}
