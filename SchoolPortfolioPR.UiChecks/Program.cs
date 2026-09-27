using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Services;

if (args.Contains("--my-works-forms")) { await MyWorksChecks.CheckFormsAsync(); return; }
if (args.Contains("--my-works")) { await MyWorksChecks.RunAsync(); return; }
if (args.Contains("--pr")) { await PublicRelationsChecks.RunAsync(args.Contains("--demo-pr")); return; }
if (args.Contains("--roles")) { await RoleReadinessChecks.RunAsync(args.Contains("--workflow-only")); return; }
if (args.Contains("--help-wizard")) { await HelpChecks.RunAsync(); return; }
if (args.Contains("--gallery")) { await GalleryChecks.RunAsync(); return; }
if (args.Contains("--branding")) { await BrandingChecks.RunAsync(); return; }
if (args.Contains("--student-awards")) { await StudentAwardChecks.RunAsync(); return; }
if (args.Contains("--student-menu")) { await StudentMenuChecks.RunAsync(); return; }
if (args.Contains("--participants")) { await ParticipantPickerChecks.RunAsync(); return; }
if (args.Contains("--personal")) { await PersonalPortfolioChecks.RunAsync(args.Contains("--workflow-only"), args.Contains("--refresh-sample")); return; }

try {
    var secrets = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "SchoolPortfolioPR-Sar-2026", "secrets.json");
    var config = new ConfigurationBuilder().AddJsonFile(secrets).AddEnvironmentVariables().Build();
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
    await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
    var page = await context.NewPageAsync(); var errors = new List<string>();
    page.PageError += (_, error) => errors.Add(error);
    page.Console += (_, message) => { if (message.Type == "error") Console.WriteLine("Browser diagnostic: " + message.Text); };
    var baseUrl = args.FirstOrDefault() ?? "http://localhost:5093";
    Directory.CreateDirectory("artifacts/ui");
    await page.GotoAsync(baseUrl + "/Account/Login");
    await page.Locator("#Email").FillAsync(config["SeedAdmin:Email"]!); await page.Locator("#Password").FillAsync(config["SeedAdmin:Password"]!);
    await page.Locator("form button").ClickAsync(); await page.WaitForURLAsync("**/Dashboard");
    await page.EvaluateAsync("document.fonts.ready");
    await page.ScreenshotAsync(new() { Path = "artifacts/ui/dashboard-desktop.png", FullPage = true });
    foreach (var route in new[] { "/Activities", "/Activities/Edit", "/Awards", "/Awards/Edit", "/Portfolios", "/Publications", "/Reports", "/Masters?kind=Teacher", "/Users", "/Audit" }) {
        var response = await page.GotoAsync(baseUrl + route); if (response?.Status != 200) throw new Exception("Page failed: " + route);
        await Assertions.Expect(page.Locator("[data-nav].active")).ToHaveCountAsync(1);
    }
    await page.GotoAsync(baseUrl + "/Activities?q=DEMO-ACT-001");
    await page.Locator("tbody tr a").First.ClickAsync(); var activityUrl = page.Url;
    var pdfLink = page.Locator(".file-name[data-media-type=pdf]").First;
    await Assertions.Expect(pdfLink).ToBeVisibleAsync();
    await page.ScreenshotAsync(new() { Path = "artifacts/ui/activity-desktop.png", FullPage = true });
    var modal = page.Locator("#mediaViewer");
    async Task ReadyPdf() { await Assertions.Expect(modal).ToBeVisibleAsync(); try { await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new() { Timeout = 60000 }); } catch { Console.WriteLine("Preview status: " + await page.Locator("#mediaStatus").InnerTextAsync()); throw; } await Assertions.Expect(page.Locator("#mediaStage")).ToHaveAttributeAsync("aria-busy", "false"); }
    async Task Close() { await page.GetByRole(AriaRole.Button, new() { Name = "ปิดตัวอย่าง", Exact = true }).ClickAsync(); await Assertions.Expect(modal).ToBeHiddenAsync(); }
    await pdfLink.ClickAsync(); await ReadyPdf();
    if (context.Pages.Count != 1 || page.Url != activityUrl) throw new Exception("PDF navigated away from activity");
    var oldWidth = await page.Locator("#mediaSurface canvas").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
    await page.Locator("#mediaZoomIn").ClickAsync(); await Assertions.Expect(page.Locator("#mediaZoom")).ToHaveTextAsync("125%");
    await Assertions.Expect(page.Locator("#mediaStage")).ToHaveAttributeAsync("aria-busy", "false");
    var newWidth = await page.Locator("#mediaSurface canvas").EvaluateAsync<double>("el => el.getBoundingClientRect().width"); if (newWidth <= oldWidth) throw new Exception("PDF zoom did not enlarge page");
    await page.ScreenshotAsync(new() { Path = "artifacts/ui/pdf-desktop.png" });
    var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("#mediaDownload").ClickAsync()); if (await download.FailureAsync() != null) throw new Exception("Download failed");
    await page.Keyboard.PressAsync("Escape"); await Assertions.Expect(modal).ToBeHiddenAsync(); await Assertions.Expect(pdfLink).ToBeFocusedAsync();
    Console.WriteLine("PASS activity PDF: shared popup, zoom, download, Escape and focus return.");

    // Local file previews exercise images and multipage PDFs without uploading or changing database records.
    var fixture = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 720, Height = 450 } });
    await fixture.SetContentAsync("<body style='margin:0;background:#eaf0ff;font-family:Arial'><div style='padding:70px;color:#355bb7'><h1 style='font-size:44px'>School activity</h1><p>Image preview test · 01</p><div style='width:120px;height:120px;background:#6589f5;border-radius:30px'></div></div></body>");
    var imageOne = await fixture.ScreenshotAsync();
    await fixture.SetContentAsync("<body style='margin:0;background:#e9f8f3;font-family:Arial'><div style='padding:70px;color:#167d69'><h1 style='font-size:44px'>Learning together</h1><p>Image preview test · 02</p><div style='width:120px;height:120px;background:#3caa92;border-radius:60px'></div></div></body>");
    var imageTwo = await fixture.ScreenshotAsync(); await fixture.CloseAsync();
    var pdf = await new ExportService().PdfAsync(ExportService.Document("เอกสารตัวอย่างสามหน้า", "<h1>หน้าที่หนึ่ง</h1><p>ทดสอบการแสดงผลภาษาไทย</p><section><h1>หน้าที่สอง</h1></section><section><h1>หน้าที่สาม</h1></section>"));
    await page.Locator("input[type=file]").SetInputFilesAsync(new[] {
        new FilePayload { Name = "ภาพกิจกรรม-01.png", MimeType = "image/png", Buffer = imageOne },
        new FilePayload { Name = "ภาพกิจกรรม-02.png", MimeType = "image/png", Buffer = imageTwo },
        new FilePayload { Name = "เอกสารตัวอย่างสามหน้า.pdf", MimeType = "application/pdf", Buffer = pdf }
    });
    var localImages = page.Locator(".preview-list [data-media-type=image]");
    await localImages.First.ClickAsync(); await Assertions.Expect(page.Locator("#mediaSurface img")).ToBeVisibleAsync();
    await page.Locator("#mediaFullscreen").ClickAsync(); await Assertions.Expect(page.Locator("#mediaFullscreen")).ToHaveAttributeAsync("aria-label", "ออกจากเต็มจอ");
    await page.Locator("#mediaFullscreen").ClickAsync(); await Assertions.Expect(page.Locator("#mediaFullscreen")).ToHaveAttributeAsync("aria-label", "เปิดเต็มจอ");
    await page.ScreenshotAsync(new() { Path = "artifacts/ui/image-desktop.png" });
    await page.Keyboard.PressAsync("ArrowRight"); await Assertions.Expect(page.Locator("#mediaTitle")).ToHaveTextAsync("ภาพกิจกรรม-02.png");
    await page.Keyboard.PressAsync("ArrowRight"); await ReadyPdf(); await Assertions.Expect(page.Locator("#mediaPageCount")).ToHaveTextAsync("3");
    await page.Locator("#mediaPageNext").ClickAsync(); await Assertions.Expect(page.Locator("#mediaStatus")).ToHaveTextAsync("PDF หน้า 2 จาก 3");
    await page.Locator("#mediaPage").FillAsync("99"); await page.Locator("#mediaPage").PressAsync("Tab"); await Assertions.Expect(page.Locator("#mediaPage")).ToHaveValueAsync("3");
    await page.Locator("#mediaPagePrevious").ClickAsync(); await Assertions.Expect(page.Locator("#mediaStatus")).ToHaveTextAsync("PDF หน้า 2 จาก 3");
    await Close();
    // Switch rapidly between PDF and image: cancelled work must not replace the new preview.
    await pdfLink.ClickAsync(); await Close(); await localImages.First.ClickAsync();
    await Assertions.Expect(page.Locator("#mediaSurface img")).ToBeVisibleAsync(); await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToHaveCountAsync(0);
    await Close(); Console.WriteLine("PASS mixed image/PDF group, thumbnail navigation, multipage controls and reopening.");

    await page.SetViewportSizeAsync(390, 844);
    await localImages.First.ClickAsync(); await Assertions.Expect(page.Locator("#mediaSurface img")).ToBeVisibleAsync();
    await page.ScreenshotAsync(new() { Path = "artifacts/ui/image-mobile.png" });
    if (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth + 1")) throw new Exception("Image popup mobile overflow");
    await Close(); await pdfLink.ClickAsync(); await ReadyPdf();
    await page.ScreenshotAsync(new() { Path = "artifacts/ui/pdf-mobile.png" });
    if (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth + 1")) throw new Exception("PDF popup mobile overflow");
    await Close(); await page.GotoAsync(baseUrl + "/Dashboard");
    await page.ScreenshotAsync(new() { Path = "artifacts/ui/dashboard-mobile.png", FullPage = true });
    await page.Locator("[data-menu]").ClickAsync(); await Assertions.Expect(page.Locator(".sidebar")).ToBeVisibleAsync(); await page.Locator("[data-menu-close]").ClickAsync(new() { Position = new() { X = 320, Y = 50 } });
    await Assertions.Expect(page.Locator("[data-menu]")).ToHaveAttributeAsync("aria-expanded", "false");
    if (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth + 1")) throw new Exception("Dashboard mobile overflow");
    Console.WriteLine("PASS mobile popup, responsive dashboard and navigation.");

    await page.SetViewportSizeAsync(1440, 1000); await page.GotoAsync(baseUrl + "/Awards?q=" + Uri.EscapeDataString("[ตัวอย่าง]")); await page.Locator("tbody tr a").First.ClickAsync();
    await page.Locator(".file-name[data-media-type=pdf]").First.ClickAsync(); await ReadyPdf(); await Close();
    await page.GotoAsync(baseUrl + "/Portfolios"); await page.Locator("#TeacherId").SelectOptionAsync(new SelectOptionValue { Label = "ครูตัวอย่าง 02" });
    await page.GetByRole(AriaRole.Button, new() { Name = "แสดงแฟ้มผลงาน", Exact = true }).ClickAsync();
    await page.Locator(".file-name[data-media-type=pdf]").First.ClickAsync(); await ReadyPdf(); await Close();
    await page.GotoAsync(baseUrl + "/Reports"); var reportUrl = page.Url;
    await page.Locator("[data-preview-report]").ClickAsync(); await ReadyPdf(); if (page.Url != reportUrl) throw new Exception("Report popup changed page URL"); await Close();
    Console.WriteLine("PASS certificates, portfolios and generated report use the same PDF popup.");
    await page.EvaluateAsync("""
        () => { const a=document.createElement('a');a.href='/files/00000000-0000-0000-0000-000000000000';a.dataset.mediaPreview='';a.dataset.mediaType='pdf';a.dataset.mediaTitle='<img src=x> missing.pdf';a.textContent='Missing preview test';a.id='missingPreview';document.querySelector('main').prepend(a); }
        """);
    await page.Locator("#missingPreview").ClickAsync(); await Assertions.Expect(page.Locator(".media-error")).ToBeVisibleAsync();
    await Assertions.Expect(page.Locator("#mediaTitle")).ToHaveTextAsync("<img src=x> missing.pdf"); await Assertions.Expect(page.Locator("#mediaTitle img")).ToHaveCountAsync(0); await Close();
    if (errors.Count > 0) throw new Exception("Browser JavaScript errors: " + string.Join("; ", errors));
    Console.WriteLine("PASS missing-file fallback, escaped filenames and no JavaScript errors. Database records unchanged.");
} catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
