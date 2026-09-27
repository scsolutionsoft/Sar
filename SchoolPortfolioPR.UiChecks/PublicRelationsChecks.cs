using System.Security.Cryptography;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;

internal static class PublicRelationsChecks
{
    public static async Task RunAsync(bool usePrAccount = false)
    {
        var secrets = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "SchoolPortfolioPR-Sar-2026", "secrets.json");
        var config = new ConfigurationBuilder().AddJsonFile(secrets).AddEnvironmentVariables().Build();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options, new HttpContextAccessor());
        var today = AnnouncementSchedule.ThaiDate(DateTimeOffset.UtcNow);
        string Iso(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var demo = await db.Announcements.AsNoTracking().Where(x => x.ReferenceCode != null && x.ReferenceCode.StartsWith("DEMO-PR-")).ToListAsync();
        if (demo.Count != 8) throw new Exception("Expected 8 sample announcements.");
        var visible = demo.Where(AnnouncementSchedule.VisibleOn(today).Compile()).OrderBy(x => x.SortOrder).ToList();
        var hidden = demo.Except(visible).ToList();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var publicContext = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1100 } });
        var page = await publicContext.NewPageAsync(); var errors = new List<string>(); page.PageError += (_, e) => errors.Add(e);
        const string root = "http://localhost:5093";
        Directory.CreateDirectory("artifacts/ui");
        await page.GotoAsync(root);
        foreach (var item in visible) await Assertions.Expect(page.Locator($"[data-announcement-id='{item.Id}']")).ToBeVisibleAsync();
        foreach (var item in hidden)
        {
            await Assertions.Expect(page.Locator($"[data-announcement-id='{item.Id}']")).ToHaveCountAsync(0);
            if ((await publicContext.APIRequest.GetAsync(root + "/announcements/" + item.Id)).Status != 404 ||
                (await publicContext.APIRequest.GetAsync(root + "/files/" + item.ImageId)).Status != 404) throw new Exception("Hidden announcement or image leaked.");
        }
        await page.WaitForFunctionAsync("() => Array.from(document.querySelectorAll('.pr-board img')).every(i => i.complete && i.naturalWidth > 0)");
        var large = await page.Locator(".pr-size-large").First.BoundingBoxAsync();
        var small = await page.Locator(".pr-size-small").First.BoundingBoxAsync();
        if (large!.Width <= small!.Width * 1.5 || large.Height <= small.Height * 1.5) throw new Exception("Card dimensions were not applied.");
        await page.ScreenshotAsync(new() { Path = "artifacts/ui/pr-home-desktop.png", FullPage = true });
        await page.Locator(".pr-image-button").First.ClickAsync();
        await Assertions.Expect(page.Locator("#mediaViewer")).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#mediaSurface img')?.naturalWidth > 0");
        await page.Keyboard.PressAsync("Escape"); await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync();
        await page.SetViewportSizeAsync(390, 844); await page.ScreenshotAsync(new() { Path = "artifacts/ui/pr-home-mobile.png", FullPage = true });
        if (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth + 1")) throw new Exception("Homepage overflows mobile viewport.");
        Console.WriteLine("PASS public board: current cards, hidden scheduled/expired posts and media, mixed sizes, image popup, mobile layout.");

        var publication = await db.Publications.AsNoTracking().SingleAsync(x => x.Title == "[ตัวอย่าง] เรื่องเล่าจากรั้วโรงเรียน");
        await page.SetViewportSizeAsync(1440, 1100);
        await page.GotoAsync(root + $"/publications/{publication.Year + 543}/{publication.Month}");
        await page.WaitForFunctionAsync("() => document.querySelector('#viewerStatus')?.textContent.includes('หน้า 1 จาก')", null, new() { Timeout = 60000 });
        var pages = int.Parse(await page.Locator("#pageCount").InnerTextAsync()); if (pages < 5) throw new Exception("Sample publication is incomplete.");
        await page.ScreenshotAsync(new() { Path = "artifacts/ui/pr-sample-publication.png", FullPage = true });
        await page.Locator("#next").ClickAsync(); await Assertions.Expect(page.Locator("#pageNumber")).ToHaveValueAsync("2");
        await page.Locator("[data-media-type=pdf]").ClickAsync(); await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new() { Timeout = 60000 });
        await page.Keyboard.PressAsync("Escape"); await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync();
        var pdf = await db.Set<PublicationFile>().Where(x => x.MonthlyPublicationId == publication.Id).OrderByDescending(x => x.GeneratedAt).Select(x => x.File).FirstAsync();
        if (!SqlFileStorage.ValidSignature(pdf.FileData, ".pdf") || pdf.FileSize != pdf.FileData.LongLength || pdf.FileHash != Convert.ToHexString(SHA256.HashData(pdf.FileData))) throw new Exception("Publication binary mismatch.");
        Console.WriteLine($"PASS sample publication: {pages} pages, page navigation, PDF popup, binary PDF stored in SQL.");
        await page.GotoAsync(root + "/Announcements/Edit"); if (!page.Url.Contains("/Account/Login")) throw new Exception("Anonymous editor access was not denied.");

        await using var adminContext = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1100 } });
        var admin = await adminContext.NewPageAsync(); admin.PageError += (_, e) => errors.Add(e);
        var account = usePrAccount ? System.Text.Json.JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!.Single(x=>x.Role=="PRAdmin") : null;
        await admin.GotoAsync(root + "/Account/Login"); await admin.Locator("#Email").FillAsync(account?.Email ?? config["SeedAdmin:Email"]!); await admin.Locator("#Password").FillAsync(account?.Password ?? config["SeedAdmin:Password"]!);
        await admin.Locator("form button").ClickAsync(); await admin.WaitForURLAsync("**/Dashboard");
        await admin.GotoAsync(root + "/Announcements"); await admin.ScreenshotAsync(new() { Path = "artifacts/ui/pr-board-management.png", FullPage = true });
        var marker = "CHECK-PR-" + Guid.NewGuid().ToString("N");
        int? fixtureId = null; var fileIds = new HashSet<Guid>();
        try
        {
            await admin.GotoAsync(root + "/Announcements/Edit");
            await admin.Locator("#Title").FillAsync(marker); await admin.Locator("#Summary").FillAsync("ทดสอบข่าวประชาสัมพันธ์");
            await admin.Locator("#StartDate").FillAsync(Iso(today)); await admin.Locator("#EndDate").FillAsync(Iso(today));
            await admin.Locator("#IsEnabled").CheckAsync(); await admin.Locator("input[name=Size][value=Large]").CheckAsync();
            await admin.Locator("#Tone").SelectOptionAsync("1");
            await admin.Locator("#announcementForm details summary").ClickAsync();
            var source = admin.Locator("input[name=SourceImageId]").Nth(1); var sourceId = Guid.Parse((await source.GetAttributeAsync("value"))!); await source.CheckAsync();
            await admin.ScreenshotAsync(new() { Path = "artifacts/ui/pr-editor.png", FullPage = true });
            async Task Save() {
                await admin.Locator("#announcementForm button[type=submit]").ClickAsync();
                try { await admin.WaitForURLAsync(root + "/Announcements", new() { Timeout = 15000 }); }
                catch {
                    Console.WriteLine("Save failed at " + admin.Url + ": " + await admin.Locator("main").InnerTextAsync());
                    await admin.ScreenshotAsync(new() { Path = "artifacts/ui/pr-save-failure.png", FullPage = true }); throw;
                }
            }
            await Save();
            var fixture = await db.Announcements.AsNoTracking().SingleAsync(x => x.Title == marker); fixtureId = fixture.Id; fileIds.Add(fixture.ImageId!.Value);
            if (fixture.ImageId == sourceId || fixture.Size != AnnouncementSize.Large || fixture.Tone != AnnouncementTone.Teal) throw new Exception("Image ownership or layout persistence failed.");
            if ((await publicContext.APIRequest.GetAsync(root + "/announcements/" + fixture.Id)).Status != 200 || (await publicContext.APIRequest.GetAsync(root + "/files/" + fixture.ImageId)).Status != 200) throw new Exception("Current fixture was not public.");
            async Task Edit() => await admin.GotoAsync(root + "/Announcements/Edit/" + fixture.Id);
            await Edit();
            // A second open editor must not overwrite changes saved from the first tab.
            var stalePage = await adminContext.NewPageAsync(); await stalePage.GotoAsync(root + "/Announcements/Edit/" + fixture.Id);
            await admin.Locator("input[name=Size][value=Small]").CheckAsync(); await admin.Locator("#SortOrder").FillAsync("5");
            await admin.Locator("#StartDate").FillAsync(Iso(today.AddDays(1))); await admin.Locator("#EndDate").FillAsync(Iso(today.AddDays(2))); await Save();
            var conflict = await stalePage.RunAndWaitForResponseAsync(() => stalePage.Locator("#announcementForm button[type=submit]").ClickAsync(), r => r.Request.Method == "POST");
            if (conflict.Status != 409) throw new Exception("Stale editor update was not rejected: " + conflict.Status);
            await stalePage.CloseAsync();
            if ((await publicContext.APIRequest.GetAsync(root + "/files/" + fixture.ImageId)).Status != 404) throw new Exception("Future image is public.");
            await Edit(); await admin.Locator("#StartDate").FillAsync(Iso(today.AddDays(-2))); await admin.Locator("#EndDate").FillAsync(Iso(today.AddDays(-1))); await Save();
            if ((await publicContext.APIRequest.GetAsync(root + "/announcements/" + fixture.Id)).Status != 404) throw new Exception("Expired fixture is public.");
            await Edit(); await admin.Locator("#StartDate").FillAsync(Iso(today)); await admin.Locator("#EndDate").FillAsync(Iso(today)); await admin.Locator("#IsEnabled").UncheckAsync(); await Save();
            if ((await publicContext.APIRequest.GetAsync(root + "/files/" + fixture.ImageId)).Status != 404) throw new Exception("Disabled image is public.");
            await Edit(); await admin.Locator("#RemoveImage").CheckAsync(); await Save();
            if ((await db.Announcements.AsNoTracking().SingleAsync(x => x.Id == fixture.Id)).ImageId != null) throw new Exception("Removing the image failed.");
            await Edit();
            var originalBytes = await db.FileObjects.Where(x => x.FileId == sourceId).Select(x => x.FileData).SingleAsync();
            await admin.Locator("#Upload").SetInputFilesAsync(new FilePayload { Name = "fixture-upload.jpg", MimeType = "image/jpeg", Buffer = originalBytes });
            await admin.Locator("#IsEnabled").CheckAsync(); await Save();
            var uploaded = await db.Announcements.AsNoTracking().SingleAsync(x => x.Id == fixture.Id); fileIds.Add(uploaded.ImageId!.Value);
            var uploadFile = await db.FileObjects.AsNoTracking().SingleAsync(x => x.FileId == uploaded.ImageId);
            if (!uploadFile.FileData.SequenceEqual(originalBytes)) throw new Exception("Uploaded image bytes changed.");
            await Edit(); await admin.Locator("form[data-confirm] button").ClickAsync(); await admin.Locator("#confirmDelete").ClickAsync(); await admin.WaitForURLAsync(root + "/Announcements");
            if ((await publicContext.APIRequest.GetAsync(root + "/files/" + uploaded.ImageId)).Status != 404) throw new Exception("Deleted announcement media remained public.");
            if (!await db.Announcements.IgnoreQueryFilters().AnyAsync(x => x.Id == fixture.Id && x.IsDeleted)) throw new Exception("Soft delete failed.");
            Console.WriteLine("PASS editor: create, image selection/copy, sizing/order, stale update protection, future/expired/disabled visibility, remove/upload image, soft delete.");
        }
        finally
        {
            // Clean up only the exact test fixture and the binary copies created by this test.
            var fixture = await db.Announcements.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Title == marker);
            if (fixture != null)
            {
                fixtureId = fixture.Id; if (fixture.ImageId.HasValue) fileIds.Add(fixture.ImageId.Value);
                await db.Announcements.IgnoreQueryFilters().Where(x => x.Id == fixture.Id && x.Title == marker).ExecuteDeleteAsync();
            }
            foreach (var id in fileIds) await db.FileObjects.IgnoreQueryFilters().Where(x => x.FileId == id).ExecuteDeleteAsync();
        }
        if (errors.Count > 0) throw new Exception(string.Join("\n", errors));
        if (await db.Activities.CountAsync(x => x.ActivityCode.StartsWith("DEMO-ACT-") && !x.IsPublic) != 10) throw new Exception("Private originals changed visibility.");
        Console.WriteLine("PASS: no JavaScript errors; original 10 private samples preserved; test fixture cleaned.");
    }
}
