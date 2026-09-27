using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;

internal static class PersonalPortfolioChecks
{
    public static async Task RunAsync(bool skipAccounts = false, bool refreshSample = false)
    {
        var credentials=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var secrets=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json");
        var config=new ConfigurationBuilder().AddJsonFile(secrets).AddEnvironmentVariables().Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});
        const string root="http://localhost:5093";Directory.CreateDirectory("artifacts/ui");
        var errors=new List<string>();
        async Task<IPage> Login(IBrowserContext context,TestCredential c){var page=await context.NewPageAsync();page.PageError+=(_,error)=>errors.Add(error);await page.GotoAsync(root+"/Account/Login");await page.Locator("#Email").FillAsync(c.Email);await page.Locator("#Password").FillAsync(c.Password);await page.Locator("form button").ClickAsync();await page.WaitForURLAsync(root+(c.Role=="Student"?"/Admissions":"/Dashboard"));return page;}
        var primary=credentials.Single(x=>x.Email=="demo.student@school.local");
        var owner=await db.Users.SingleAsync(x=>x.Email==primary.Email);
        var sample=await db.AdmissionPortfolios.AsNoTracking().SingleAsync(x=>x.OwnerId==owner.Id);
        var victimDocument=await db.PersonalDocuments.AsNoTracking().Where(x=>x.OwnerId==owner.Id).OrderBy(x=>x.Id).FirstAsync();
        foreach(var c in skipAccounts ? new List<TestCredential>() : credentials)
        {
            await using var context=await browser.NewContextAsync();var page=await Login(context,c);
            var user=await db.Users.SingleAsync(x=>x.Email==c.Email);
            var roleNames=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==user.Id select r.Name).ToListAsync();
            if(roleNames.Count!=1||roleNames[0]!=c.Role)throw new Exception("Incorrect role for test account.");
            var library=await page.GotoAsync(root+"/Documents");if(library?.Status!=200)throw new Exception("Library unavailable.");
            var own=await db.PersonalDocuments.AsNoTracking().FirstAsync(x=>x.OwnerId==user.Id);
            if((await context.APIRequest.GetAsync(root+"/files/"+own.FileId)).Status!=200)throw new Exception("Own PDF denied.");
            if(user.Id!=owner.Id){
                foreach(var url in new[]{"/files/"+victimDocument.FileId,"/files/"+sample.PdfFileId,"/Admissions/Details/"+sample.Id,"/Admissions/Edit/"+sample.Id,"/Admissions/Preview/"+sample.Id,"/Admissions/Viewer/"+sample.Id,"/Documents/Edit/"+victimDocument.Id})
                    if((await context.APIRequest.GetAsync(root+url)).Status!=(url.StartsWith("/Admissions")&&c.Role!="Student"?403:404))throw new Exception("Cross-owner read leaked for "+c.Role+": "+url);
            }
            if((await context.APIRequest.GetAsync(root+"/Users")).Status!=(c.Role=="SuperAdmin"?200:403))throw new Exception("Users policy failed: "+c.Role);
            if((await context.APIRequest.GetAsync(root+"/Announcements")).Status!=(c.Role is "SuperAdmin" or "Administrator" or "PRAdmin"?200:403))throw new Exception("Publisher policy failed: "+c.Role);
            Console.WriteLine("PASS account "+c.Email+": login, role, own PDF, cross-owner privacy, management policies.");
        }
        await using(var anonymous=await browser.NewContextAsync()){
            if((await anonymous.APIRequest.GetAsync(root+"/files/"+sample.PdfFileId)).Status!=404)throw new Exception("Anonymous personal file access.");
        }
        await using var studentContext=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1050}});
        var student=await Login(studentContext,primary);
        if(refreshSample){
            await student.GotoAsync(root+"/Admissions/Details/"+sample.Id);
            await student.Locator("#generatePortfolio").ClickAsync();
            await student.WaitForURLAsync(root+"/Admissions/Viewer/"+sample.Id,new(){Timeout=90000});
            sample=await db.AdmissionPortfolios.AsNoTracking().SingleAsync(x=>x.Id==sample.Id);
        }
        var csrf=await studentContext.APIRequest.PostAsync(root+"/Admissions/Delete",new(){Form=studentContext.APIRequest.CreateFormData().Set("id",sample.Id.ToString())});if(csrf.Status!=400)throw new Exception("CSRF missing-token protection failed: "+csrf.Status);
        await student.GotoAsync(root+"/Admissions/Details/"+sample.Id);await student.ScreenshotAsync(new(){Path="artifacts/ui/admission-studio.png",FullPage=true});
        await student.GotoAsync(root+"/Admissions/Viewer/"+sample.Id);
        await student.WaitForFunctionAsync("() => document.querySelector('#viewerStatus')?.textContent.includes('หน้า 1 จาก')",null,new(){Timeout=60000});
        await student.ScreenshotAsync(new(){Path="artifacts/ui/admission-flipbook.png",FullPage=true});
        await student.Locator("#next").ClickAsync();await Assertions.Expect(student.Locator("#pageNumber")).ToHaveValueAsync("2");
        await student.Locator("[data-media-type=pdf]").ClickAsync();await Assertions.Expect(student.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new(){Timeout=60000});await student.Keyboard.PressAsync("Escape");
        await Assertions.Expect(student.Locator("#mediaViewer")).ToBeHiddenAsync();
        await student.Locator("#pageNumber").FillAsync("5");await student.Locator("#pageNumber").PressAsync("Tab");await Assertions.Expect(student.Locator("#viewerStatus")).ToContainTextAsync("หน้า 5 จาก");await student.ScreenshotAsync(new(){Path="artifacts/ui/admission-infographic.png",FullPage=true});
        var pdf=await db.FileObjects.AsNoTracking().SingleAsync(x=>x.FileId==sample.PdfFileId);
        if(PersonalPdfService.PageCount(pdf.FileData)!=sample.ExportedPages||Convert.ToHexString(SHA256.HashData(pdf.FileData))!=pdf.FileHash)throw new Exception("Generated binary PDF mismatch.");
        await File.WriteAllBytesAsync("artifacts/private/student-sample-portfolio.pdf",pdf.FileData);
        await student.SetViewportSizeAsync(390,844);await student.GotoAsync(root+"/Admissions");await student.ScreenshotAsync(new(){Path="artifacts/ui/admission-mobile.png",FullPage=true});
        if(await student.EvaluateAsync<bool>("document.documentElement.scrollWidth > innerWidth+1"))throw new Exception("Portfolio index overflows mobile.");
        await student.SetViewportSizeAsync(1440,1050);
        Console.WriteLine($"PASS private sample: {sample.ExportedPages} pages, PDF binary, flip navigation, popup, infographic and mobile.");

        // Destructive checks use only uniquely named test fixtures and their independent binary files.
        var marker="CHECK-PERSONAL-"+Guid.NewGuid().ToString("N");int? fixtureId=null;var files=new HashSet<Guid>();var docs=new HashSet<int>();
        try{
            var previousReportId=await db.PersonalDocuments.IgnoreQueryFilters().Where(x=>x.OwnerId==owner.Id).MaxAsync(x=>(int?)x.Id)??0;
            await student.GotoAsync(root+"/Reports?Kind=AnnualActivities&FiscalYear=2569");
            await student.Locator("form[action='/Reports/SavePdf'] button").ClickAsync();await student.WaitForURLAsync(root+"/Documents",new(){Timeout=90000});
            var savedReport=await db.PersonalDocuments.AsNoTracking().SingleAsync(x=>x.OwnerId==owner.Id&&x.Id>previousReportId);
            docs.Add(savedReport.Id);files.Add(savedReport.FileId);
            var savedReportFile=await db.FileObjects.AsNoTracking().SingleAsync(x=>x.FileId==savedReport.FileId);
            if(PersonalPdfService.PageCount(savedReportFile.FileData)!=savedReport.Pages)throw new Exception("Saved report PDF invalid.");
            await student.GotoAsync(root+"/Documents/Edit");await student.Locator("#Title").FillAsync(marker+" report");await student.Locator("#ReportText").FillAsync("รายงานตัวอย่างสำหรับทดสอบการสร้าง PDF ของเจ้าของบัญชี");
            await student.Locator("main form button").ClickAsync();await student.WaitForURLAsync(root+"/Documents");
            var report=await db.PersonalDocuments.AsNoTracking().SingleAsync(x=>x.OwnerId==owner.Id&&x.Title==marker+" report");docs.Add(report.Id);files.Add(report.FileId);
            await student.GotoAsync(root+"/Documents/Edit");await student.Locator("#Title").FillAsync(marker+" upload");await student.Locator("#Upload").SetInputFilesAsync(new FilePayload{Name="evidence.pdf",MimeType="application/pdf",Buffer=pdf.FileData});await student.Locator("main form button").ClickAsync();await student.WaitForURLAsync(root+"/Documents");
            var uploaded=await db.PersonalDocuments.AsNoTracking().SingleAsync(x=>x.OwnerId==owner.Id&&x.Title==marker+" upload");docs.Add(uploaded.Id);files.Add(uploaded.FileId);
            await student.GotoAsync(root+"/Admissions/Edit");await student.Locator("#Title").FillAsync(marker);await student.Locator("#FullName").FillAsync("นักเรียนทดสอบ");await student.Locator("#School").FillAsync("โรงเรียนทดสอบ");await student.Locator("#MaxPages").FillAsync("1");await student.Locator("#IncludeContents").UncheckAsync();await student.Locator($"input[name=DocumentIds][value='{report.Id}']").CheckAsync();
            // Form-bound owner fields must not change ownership.
            await student.EvaluateAsync("() => {const i=document.createElement('input');i.type='hidden';i.name='OwnerId';i.value='other-user';document.querySelector('#admissionForm').append(i)}");
            async Task SaveProfile(){await student.Locator("#admissionForm button[type=submit],#admissionForm button:not([type])").Last.ClickAsync();await student.WaitForURLAsync("**/Admissions/Details/*");}
            await SaveProfile();var fixture=await db.AdmissionPortfolios.AsNoTracking().SingleAsync(x=>x.Title==marker);fixtureId=fixture.Id;if(fixture.OwnerId!=owner.Id)throw new Exception("Owner spoofing succeeded.");
            var audit=await db.AuditLogs.AsNoTracking().SingleAsync(x=>x.Entity==nameof(AdmissionPortfolio)&&x.EntityId==fixture.Id.ToString()&&x.Action=="Added");
            using(var auditJson=JsonDocument.Parse(audit.NewValue!)){
                if(auditJson.RootElement.TryGetProperty("Title",out _)||auditJson.RootElement.TryGetProperty("FullName",out _)||!auditJson.RootElement.TryGetProperty("OwnerId",out _))throw new Exception("Personal content exposed in audit log.");
            }
            async Task Generate(){await student.Locator("#generatePortfolio").ClickAsync();await student.WaitForLoadStateAsync(LoadState.Load);}
            await Generate();await Assertions.Expect(student.Locator("[role=alert]")).ToContainTextAsync("เกินที่กำหนด",new(){Timeout=90000});
            if((await db.AdmissionPortfolios.AsNoTracking().SingleAsync(x=>x.Id==fixture.Id)).PdfFileId!=null)throw new Exception("Over-limit PDF persisted.");
            await student.GotoAsync(root+"/Admissions/Edit/"+fixture.Id);await student.Locator("#MaxPages").FillAsync("20");await student.Locator("#Skills").FillAsync("การวิเคราะห์ | 75\nทีมเวิร์ก | 90");await SaveProfile();
            var sourceImage=await db.FileObjects.AsNoTracking().FirstAsync(x=>x.ContentType=="image/jpeg");
            for(var i=0;i<2;i++){
                await student.GotoAsync(root+$"/Admissions/Entry?portfolioId={fixture.Id}");await student.Locator("#Title").FillAsync(marker+" entry "+i);await student.Locator("#Role").FillAsync("ผู้นำเสนอ");await student.Locator("#Reflection").FillAsync("เรียนรู้การใช้หลักฐานและการทำงานร่วมกัน");await student.Locator("#Description").FillAsync("วางแผนและทดลองผลงาน");await student.Locator("#PersonalDocumentId").SelectOptionAsync(report.Id.ToString());
                if(i==0)await student.Locator("#ImageUpload").SetInputFilesAsync(new FilePayload{Name="project.jpg",MimeType="image/jpeg",Buffer=sourceImage.FileData});
                await student.Locator("#entryForm button").ClickAsync();await student.WaitForURLAsync(root+"/Admissions/Details/"+fixture.Id);
            }
            foreach(var id in await db.Set<PortfolioEntry>().Where(x=>x.AdmissionPortfolioId==fixture.Id&&x.ImageId!=null).Select(x=>x.ImageId!.Value).ToListAsync())files.Add(id);
            await student.Locator("details summary").Filter(new(){HasText="เรียงลำดับผลงาน"}).ClickAsync();await student.Locator("[data-sortable] [data-down]").First.ClickAsync();await student.Locator("form[action='/Admissions/Order'] button:not([type])").ClickAsync();await student.WaitForURLAsync(root+"/Admissions/Details/"+fixture.Id);
            await Generate();await student.WaitForURLAsync(root+"/Admissions/Viewer/"+fixture.Id,new(){Timeout=90000});
            var generated=await db.AdmissionPortfolios.AsNoTracking().SingleAsync(x=>x.Id==fixture.Id);files.Add(generated.PdfFileId!.Value);
            if(generated.ExportedPages!=7)throw new Exception("Expected 4 main pages + 2 works + 1 deduplicated evidence page; got "+generated.ExportedPages);
            await student.GotoAsync(root+"/Documents");await student.Locator($"[data-document-id='{report.Id}'] form button").ClickAsync();await student.Locator("#confirmDelete").ClickAsync();await student.WaitForURLAsync(root+"/Documents");await Assertions.Expect(student.Locator("[role=alert]")).ToContainTextAsync("ใช้อยู่ในแฟ้ม");
            await student.GotoAsync(root+"/Admissions/Edit/"+fixture.Id);await student.Locator("#Bio").FillAsync("Updated content invalidates the old export");await SaveProfile();
            if((await studentContext.APIRequest.GetAsync(root+"/files/"+generated.PdfFileId)).Status!=404)throw new Exception("Stale portfolio PDF still downloadable.");
            // A second student cannot write any of the first student's records, even with a valid token.
            await using var otherContext=await browser.NewContextAsync();var other=await Login(otherContext,credentials.Single(x=>x.Email=="demo.student2@school.local"));await other.GotoAsync(root+"/Documents/Edit");
            var token=await other.Locator("main input[name=__RequestVerificationToken]").InputValueAsync();
            foreach(var url in new[]{"/Admissions/Delete","/Admissions/Generate","/Documents/Delete"}){
                var id=url.StartsWith("/Documents")?report.Id:fixture.Id;
                var status=await other.EvaluateAsync<int>("async a=>{const f=new FormData();f.set('__RequestVerificationToken',a.token);f.set('id',a.id);f.set('rowVersion',a.version);return (await fetch(a.url,{method:'POST',body:f})).status}",new{url,token,id=id.ToString(),version=Convert.ToBase64String(generated.RowVersion)});
                if(status!=404)throw new Exception("Cross-owner POST expected 404; got "+status);
            }
            await student.GotoAsync(root+"/Admissions/Details/"+fixture.Id);await student.Locator("form[action='/Admissions/Delete'] button").ClickAsync();await student.Locator("#confirmDelete").ClickAsync();await student.WaitForURLAsync(root+"/Admissions");
            if((await studentContext.APIRequest.GetAsync(root+"/Admissions/Details/"+fixture.Id)).Status!=404)throw new Exception("Deleted portfolio still visible.");
            Console.WriteLine("PASS library/report/upload; portfolio create, owner spoof protection, page cap, entries, images/order, evidence deduplication, export invalidation, cross-owner writes, soft delete.");
        }finally{
            var fixtures=await db.AdmissionPortfolios.IgnoreQueryFilters().AsNoTracking().Where(x=>x.OwnerId==owner.Id&&x.Title==marker).ToListAsync();
            foreach(var p in fixtures){if(p.PdfFileId.HasValue)files.Add(p.PdfFileId.Value);if(p.PortraitId.HasValue)files.Add(p.PortraitId.Value);foreach(var id in await db.Set<PortfolioEntry>().IgnoreQueryFilters().Where(x=>x.AdmissionPortfolioId==p.Id&&x.ImageId!=null).Select(x=>x.ImageId!.Value).ToListAsync())files.Add(id);await db.Set<PortfolioDocument>().IgnoreQueryFilters().Where(x=>x.AdmissionPortfolioId==p.Id).ExecuteDeleteAsync();await db.Set<PortfolioEntry>().IgnoreQueryFilters().Where(x=>x.AdmissionPortfolioId==p.Id).ExecuteDeleteAsync();await db.AdmissionPortfolios.IgnoreQueryFilters().Where(x=>x.Id==p.Id&&x.OwnerId==owner.Id).ExecuteDeleteAsync();}
            foreach(var d in await db.PersonalDocuments.IgnoreQueryFilters().AsNoTracking().Where(x=>x.OwnerId==owner.Id&&(x.Title.StartsWith(marker)||docs.Contains(x.Id))).ToListAsync()){files.Add(d.FileId);await db.PersonalDocuments.IgnoreQueryFilters().Where(x=>x.Id==d.Id&&x.OwnerId==owner.Id).ExecuteDeleteAsync();}
            foreach(var id in files)await db.FileObjects.IgnoreQueryFilters().Where(x=>x.FileId==id).ExecuteDeleteAsync();
        }
        if(errors.Count!=0)throw new Exception(string.Join("\n",errors));Console.WriteLine("PASS personal portfolio checks; no JavaScript errors; temporary fixtures cleaned.");
    }
}
