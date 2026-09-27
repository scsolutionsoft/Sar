using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;

internal static class BrandingChecks
{
    public static async Task RunAsync()
    {
        const string root = "http://localhost:5093";
        var accounts = JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "SchoolPortfolioPR-Sar-2026", "secrets.json")).Build();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options, new HttpContextAccessor());
        var original = await db.Branding.AsNoTracking().SingleAsync(); var marker = "BRAND-CHECK-" + Guid.NewGuid().ToString("N")[..8];
        using var playwright = await Playwright.CreateAsync(); await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var errors = new List<string>(); Directory.CreateDirectory("artifacts/ui/branding");
        try
        {
            foreach (var account in accounts)
            {
                await using var context = await browser.NewContextAsync(); var page = await context.NewPageAsync(); page.PageError += (_, e) => errors.Add(e);
                await page.GotoAsync(root + "/Account/Login"); await page.Locator("#Email").FillAsync(account.Email); await page.Locator("#Password").FillAsync(account.Password); await page.Locator("form button").ClickAsync(); await page.WaitForURLAsync(root + (account.Role == "Student" ? "/Admissions" : "/Dashboard"));
                var admin = account.Role is "SuperAdmin" or "Administrator";
                await Assertions.Expect(page.Locator("#sidebar a[href='/Branding']")).ToHaveCountAsync(admin ? 1 : 0);
                if ((await context.APIRequest.GetAsync(root + "/Branding")).Status != (admin ? 200 : 403)) throw new Exception("Branding GET role check failed.");
                await page.GotoAsync(root + (admin ? "/Branding" : "/Documents/Edit"));
                var token = await page.Locator("main input[name=__RequestVerificationToken]").InputValueAsync();
                if (!admin)
                {
                    var denied = context.APIRequest.CreateFormData().Set("__RequestVerificationToken", token).Set("ProgramName", "unauthorized").Set("RowVersion", Convert.ToBase64String(original.RowVersion));
                    if ((await context.APIRequest.PostAsync(root + "/Branding", new() { Form = denied })).Status != 403) throw new Exception("Non-admin branding POST accepted.");
                }
                else
                {
                    var version = await page.Locator("#RowVersion").InputValueAsync();
                    var title = marker + " โรงเรียนแห่งการเรียนรู้ <b>ตัวอย่าง</b> " + account.Role;
                    await page.Locator("#ProgramName").FillAsync(title);
                    var drawing = await context.NewPageAsync(); await drawing.SetViewportSizeAsync(180, 180); await drawing.SetContentAsync("<body style='margin:0;background:#3158bf;color:white;display:grid;place-items:center;height:100vh;font:700 30px Arial'>LOGO</body>");
                    var png = await drawing.ScreenshotAsync(); await drawing.CloseAsync();
                    await page.Locator("#Logo").SetInputFilesAsync(new FilePayload { Name = marker + ".png", MimeType = "image/png", Buffer = png });
                    await page.Locator(".form-actions button").ClickAsync(); await page.WaitForURLAsync(root + "/Branding");
                    await Assertions.Expect(page.Locator(".program-name")).ToHaveTextAsync(title); await Assertions.Expect(page.Locator(".program-name b")).ToHaveCountAsync(0);
                    var saved = await db.Branding.AsNoTracking().Include(x => x.LogoFile).SingleAsync();
                    if (saved.ProgramName != title || !saved.LogoFile!.FileData.SequenceEqual(png)) throw new Exception("Branding binary persistence failed.");
                    var logoUrl = root + "/Branding/Logo/" + saved.LogoFileId;
                    await using var anonymous = await browser.NewContextAsync(); var login = await anonymous.NewPageAsync(); await login.GotoAsync(root + "/Account/Login");
                    await Assertions.Expect(login.Locator(".login-brand strong")).ToHaveTextAsync(title);
                    await Assertions.Expect(login).ToHaveTitleAsync("เข้าสู่ระบบ · " + title);
                    if (!await login.Locator(".login-brand img").EvaluateAsync<bool>("el => el.complete && el.naturalWidth > 0")) throw new Exception("Logo image did not load.");
                    if ((await anonymous.APIRequest.GetAsync(logoUrl)).Status != 200 || (await anonymous.APIRequest.GetAsync(root + "/Branding/Logo/" + Guid.NewGuid())).Status != 404) throw new Exception("Public logo lookup failed.");
                    await page.SetViewportSizeAsync(390, 844); if (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth + 1")) throw new Exception("Long program name overflows mobile.");
                    await page.ScreenshotAsync(new() { Path = "artifacts/ui/branding/" + account.Role + "-mobile.png", FullPage = true });
                    var stale = context.APIRequest.CreateFormData().Set("__RequestVerificationToken", token).Set("ProgramName", "stale").Set("RowVersion", version);
                    if ((await context.APIRequest.PostAsync(root + "/Branding", new() { Form = stale })).Status != 409) throw new Exception("Concurrent branding edit accepted.");
                    var currentVersion = await page.Locator("#RowVersion").InputValueAsync();
                    var invalid = context.APIRequest.CreateFormData().Set("__RequestVerificationToken", token).Set("ProgramName", " ").Set("RowVersion", currentVersion);
                    var invalidResponse = await context.APIRequest.PostAsync(root + "/Branding", new() { Form = invalid });
                    if (!(await invalidResponse.TextAsync()).Contains("validation-summary-errors")) throw new Exception("Empty name was not rejected.");
                    await page.Locator("#Logo").SetInputFilesAsync(new FilePayload { Name = marker + ".png", MimeType = "image/png", Buffer = "not-an-image-file"u8.ToArray() });
                    await page.Locator(".form-actions button").ClickAsync(); await Assertions.Expect(page.Locator(".validation-summary-errors")).ToBeVisibleAsync();
                    if ((await db.Branding.AsNoTracking().SingleAsync()).LogoFileId != saved.LogoFileId) throw new Exception("Invalid upload replaced logo.");
                    await page.GotoAsync(root + "/Branding"); await page.Locator("#RemoveLogo").CheckAsync(); await page.Locator(".form-actions button").ClickAsync();
                    await Assertions.Expect(page.Locator(".brand-mark .program-logo")).ToHaveCountAsync(0);
                    if ((await anonymous.APIRequest.GetAsync(logoUrl)).Status != 404) throw new Exception("Removed logo remains publicly accessible.");
                    Console.WriteLine("PASS " + account.Role + ": name/logo save, SQL binary, login/header/title, HTML escaping, mobile, concurrency, validation and logo removal.");
                }
                Console.WriteLine("PASS branding permissions: " + account.Email);
            }
            if (errors.Count > 0) throw new Exception(string.Join("\n", errors));
        }
        finally
        {
            await db.Branding.Where(x => x.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(x => x.ProgramName, original.ProgramName).SetProperty(x => x.LogoFileId, original.LogoFileId));
            if (original.LogoFileId.HasValue) await db.FileObjects.IgnoreQueryFilters().Where(x => x.FileId == original.LogoFileId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, false));
            await db.FileObjects.IgnoreQueryFilters().Where(x => x.OriginalFileName == marker + ".png").ExecuteDeleteAsync();
        }
        Console.WriteLine("PASS branding checks; original settings restored and test logos cleaned.");
    }
}
