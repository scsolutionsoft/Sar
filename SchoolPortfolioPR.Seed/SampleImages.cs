using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;

internal static class SampleImages
{
    private record Photo(int Id, string Label);
    private static readonly Photo[] Photos = [
        new(4033019, "ห้องปฏิบัติการวิทยาศาสตร์"),
        new(36488453, "อุปกรณ์การเรียนคณิตศาสตร์"),
        new(18620043, "หนังสือเพื่อการเรียนรู้"),
        new(265076, "การเรียนรู้ภาษาอังกฤษ"),
        new(15305007, "สนามกีฬาและบาสเกตบอล"),
        new(27176068, "การดูแลพื้นที่สีเขียว"),
        new(20432916, "ห้องเรียนดิจิทัล"),
        new(6683440, "อุปกรณ์การนำเสนอผลงาน"),
        new(18620043, "หนังสือในห้องสมุด"),
        new(12969371, "ต้นกล้าและการปลูกต้นไม้")
    ];
    private static string Name(int i) => $"ภาพตัวอย่าง-{i + 1:000}-{Photos[i].Label}-pexels-{Photos[i].Id}.jpg";

    public static async Task RunAsync(ApplicationDbContext db, IConfiguration config, HttpContextAccessor http, string[] codes, string[] args)
    {
        var activities = await db.Activities.Where(x => codes.Contains(x.ActivityCode))
            .Include(x => x.Images).ThenInclude(x => x.File).OrderBy(x => x.ActivityCode).ToListAsync();
        if (activities.Count != codes.Length) throw new InvalidOperationException("Create all 10 sample activities before adding images.");
        var added = 0;
        if (args.Contains("--images"))
        {
            var admin = await db.Users.SingleAsync(x => x.Email == config["SeedAdmin:Email"]);
            http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, admin.Id), new Claim(ClaimTypes.Role, "SuperAdmin")], "seed"));
            // Download and validate before starting the database transaction. Files are never saved under wwwroot.
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            client.MaxResponseContentBufferSize = 10 * 1024 * 1024;
            var downloads = new Dictionary<int, byte[]>();
            for (var i = 0; i < Photos.Length; i++)
            {
                if (activities[i].Images.Any(x => x.File.OriginalFileName == Name(i))) continue;
                var photo = Photos[i];
                var url = $"https://images.pexels.com/photos/{photo.Id}/pexels-photo-{photo.Id}.jpeg?auto=compress&cs=tinysrgb&w=1280&fm=jpg";
                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync();
                if (response.Content.Headers.ContentType?.MediaType != "image/jpeg" || !SqlFileStorage.ValidSignature(bytes, ".jpg"))
                    throw new InvalidDataException($"Invalid JPEG for photo {photo.Id}.");
                downloads[i] = bytes;
                Console.WriteLine($"Downloaded {codes[i]}: Pexels {photo.Id}, {bytes.Length} bytes.");
            }
            var storage = new SqlFileStorage(db, config);
            await using var transaction = await db.Database.BeginTransactionAsync();
            foreach (var (i, bytes) in downloads)
            {
                using var stream = new MemoryStream(bytes);
                var upload = new FormFile(stream, 0, bytes.Length, "file", Name(i)) { Headers = new HeaderDictionary(), ContentType = "image/jpeg" };
                var file = await storage.StoreAsync(upload, admin.Id);
                activities[i].Images.Add(new ActivityImage { File = file, SortOrder = activities[i].Images.Select(x => x.SortOrder).DefaultIfEmpty(-1).Max() + 1 });
                activities[i].CoverImageId ??= file.FileId;
                added++;
            }
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            db.ChangeTracker.Clear();
        }

        // Read persisted bytes back from SQL, including the existing PDF/Word/Excel examples.
        var names = Enumerable.Range(0, Photos.Length).Select(Name).ToArray();
        // Public announcements/publications may own independent copies with the same display filename.
        // Verify the original sample activities' files, not every copy across the database.
        var files = await db.Set<ActivityImage>().AsNoTracking()
            .Where(x => codes.Contains(x.Activity.ActivityCode) && names.Contains(x.File.OriginalFileName))
            .Select(x => x.File).ToListAsync();
        if (files.Count != 10) throw new InvalidOperationException("Expected 10 persisted sample images.");
        var documents = await db.FileObjects.AsNoTracking().Where(x => x.OriginalFileName.StartsWith("demo-") && !x.ContentType.StartsWith("image/")).ToListAsync();
        foreach (var file in files.Concat(documents))
            if (file.FileData.LongLength != file.FileSize || file.FileSize <= 0 || Convert.ToHexString(SHA256.HashData(file.FileData)) != file.FileHash || !SqlFileStorage.ValidSignature(file.FileData, file.FileExtension))
                throw new InvalidDataException($"Binary round-trip failed for {file.OriginalFileName}.");
        var columnType = await db.Database.SqlQueryRaw<string>("SELECT DATA_TYPE AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='portfolio' AND TABLE_NAME='FileObjects' AND COLUMN_NAME='FileData' AND CHARACTER_MAXIMUM_LENGTH=-1").SingleAsync();
        if (columnType != "varbinary") throw new InvalidOperationException("FileData must be varbinary(max).");
        var saved = await db.Activities.AsNoTracking().Where(x => codes.Contains(x.ActivityCode)).OrderBy(x => x.ActivityCode)
            .Select(x => new { x.Id, x.ActivityCode, x.CoverImageId, x.IsPublic, Images = x.Images.Select(i => i.File.OriginalFileName).ToList() }).ToListAsync();
        for (var i = 0; i < saved.Count; i++)
            if (!saved[i].CoverImageId.HasValue || !saved[i].Images.Contains(Name(i))) throw new InvalidOperationException("Missing sample gallery or cover.");
        Console.WriteLine(JsonSerializer.Serialize(new { Added = added, Activities = saved.Count, Images = files.Count, Documents = documents.Count, ImageBytes = files.Sum(x => x.FileSize), SqlType = "varbinary(max)", HashesVerified = true }));

        if (!args.Contains("--verify-ui")) return;
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
        var page = await context.NewPageAsync();
        var baseUrl = args.FirstOrDefault(x => x.StartsWith("http")) ?? "http://localhost:5093";
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync(baseUrl + "/Account/Login");
        await page.Locator("#Email").FillAsync(config["SeedAdmin:Email"]!);
        await page.Locator("#Password").FillAsync(config["SeedAdmin:Password"]!);
        await page.Locator("form button").ClickAsync(); await page.WaitForURLAsync("**/Dashboard");
        // Block remote requests: covers, galleries and documents must work entirely through our application.
        await context.RouteAsync("**/*", async route => {
            if (new Uri(route.Request.Url).Authority == new Uri(baseUrl).Authority) await route.ContinueAsync();
            else await route.AbortAsync();
        });
        Directory.CreateDirectory("artifacts/ui");
        foreach (var activity in saved)
        {
            await page.GotoAsync(baseUrl + "/activity/" + activity.Id);
            await page.WaitForFunctionAsync("() => document.querySelector('.article-cover')?.naturalWidth > 0");
            await page.Locator(".gallery [data-media-preview]").First.ClickAsync();
            await Assertions.Expect(page.Locator("#mediaViewer")).ToBeVisibleAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('#mediaSurface img')?.naturalWidth > 0");
            await Assertions.Expect(page.Locator("#mediaStage")).ToHaveAttributeAsync("aria-busy", "false");
            if (activity == saved[0]) await page.ScreenshotAsync(new() { Path = "artifacts/ui/sample-image-popup.png" });
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync();
        }
        foreach (var file in files.Concat(documents))
        {
            var response = await context.APIRequest.GetAsync(baseUrl + "/files/" + file.FileId);
            if (response.Status != 200 || Convert.ToHexString(SHA256.HashData(await response.BodyAsync())) != file.FileHash)
                throw new InvalidOperationException("HTTP binary round-trip failed.");
        }
        await page.GotoAsync(baseUrl + "/activity/" + saved[0].Id);
        await page.Locator(".file-name[data-media-type=pdf]").First.ClickAsync();
        await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new() { Timeout = 60000 });
        await page.Keyboard.PressAsync("Escape"); await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync();
        await page.ScreenshotAsync(new() { Path = "artifacts/ui/sample-activity-with-images.png", FullPage = true });
        await using var anonymous = await browser.NewContextAsync();
        foreach (var activity in saved.Where(x => !x.IsPublic))
        {
            var response = await anonymous.APIRequest.GetAsync(baseUrl + "/files/" + activity.CoverImageId);
            if (response.Status != 404) throw new InvalidOperationException("Private cover was publicly accessible.");
        }
        if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
        Console.WriteLine("PASS: 10 covers and image popups, PDF popup, binary HTTP hashes, private access, no remote image dependency.");
    }
}
