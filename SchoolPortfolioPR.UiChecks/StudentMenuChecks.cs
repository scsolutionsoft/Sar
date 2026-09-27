using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;

internal static class StudentMenuChecks
{
    public static async Task RunAsync()
    {
        const string root="http://localhost:5093";
        var accounts=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json")).Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        var owner=await db.Users.AsNoTracking().SingleAsync(x=>x.Email=="demo.teacher@school.local");
        var source=await db.PersonalDocuments.Where(x=>x.OwnerId==owner.Id).Select(x=>x.File).AsNoTracking().FirstAsync();
        var binary=new FileObject{OriginalFileName="menu-role-check.pdf",StoredFileName=Guid.NewGuid()+".pdf",FileData=source.FileData,FileSize=source.FileSize,ContentType=source.ContentType,FileExtension=source.FileExtension,FileHash=source.FileHash,UploadedBy=owner.Id};
        var fixture=new AdmissionPortfolio{Title="CHECK-STUDENT-MENU-"+Guid.NewGuid(),OwnerId=owner.Id,FullName="ทดสอบสิทธิ์เดิม",School="โรงเรียนทดสอบ",PdfFile=binary};
        db.Add(fixture);await db.SaveChangesAsync();
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});var errors=new List<string>();Directory.CreateDirectory("artifacts/ui/menu");
        try{
            foreach(var c in accounts){
                await using var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1050}});var page=await context.NewPageAsync();page.PageError+=(_,e)=>errors.Add(e);
                await page.GotoAsync(root+"/Account/Login");await page.Locator("#Email").FillAsync(c.Email);await page.Locator("#Password").FillAsync(c.Password);await page.Locator("form button").ClickAsync();await page.WaitForURLAsync(root+(c.Role=="Student"?"/Admissions":"/Dashboard"));
                await page.GotoAsync(root+"/Dashboard");var student=c.Role=="Student";
                await Assertions.Expect(page.Locator("#sidebar a[href='/Admissions']")).ToHaveCountAsync(student?1:0);
                await Assertions.Expect(page.Locator("[data-role-workspace] a[href='/Admissions']")).ToHaveCountAsync(student?1:0);
                await Assertions.Expect(page.Locator("#sidebar a[href='/Portfolios']")).ToHaveCountAsync(student?0:1);
                if(student)await Assertions.Expect(page.Locator("a[href^='/Portfolios']")).ToHaveCountAsync(0);
                if((await context.APIRequest.GetAsync(root+"/Portfolios")).Status!=(student?403:200))throw new Exception("School portfolio role restriction failed.");
                if((await context.APIRequest.GetAsync(root+"/Reports")).Status!=200)throw new Exception("Reports unavailable.");
                if(await page.Locator(".nav-frame").CountAsync()<4)throw new Exception("Menu sections missing.");
                if(await page.Locator(".workspace-menu-card").CountAsync()!=await page.Locator(".workspace-menu-card .workspace-menu-icon svg").CountAsync())throw new Exception("Shortcut icon missing.");
                await page.ScreenshotAsync(new(){Path=$"artifacts/ui/menu/{c.Email.Split('@')[0]}-desktop.png"});
                await page.SetViewportSizeAsync(390,844);await page.Locator("[data-menu]").ClickAsync();await Assertions.Expect(page.Locator("#sidebar")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("open"));
                if(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1 || document.querySelector('#sidebar').scrollWidth>document.querySelector('#sidebar').clientWidth+1"))throw new Exception("Mobile menu overflow.");
                await page.ScreenshotAsync(new(){Path=$"artifacts/ui/menu/{c.Email.Split('@')[0]}-mobile.png"});await page.Keyboard.PressAsync("Escape");await Assertions.Expect(page.Locator("[data-menu]")).ToHaveAttributeAsync("aria-expanded","false");
                await page.GotoAsync(root+"/Documents");if(!student){await Assertions.Expect(page.Locator("main a[href^='/Admissions']")).ToHaveCountAsync(0);await Assertions.Expect(page.Locator("main")).Not.ToContainTextAsync("แฟ้มสะสมผลงานที่ส่งออกแล้ว");}
                if((await context.APIRequest.GetAsync(root+"/files/"+source.FileId)).Status!=(c.Email==owner.Email?200:404))throw new Exception("Personal documents permission changed.");
                if((await context.APIRequest.GetAsync(root+"/files/"+binary.FileId)).Status!=404)throw new Exception("Former non-student portfolio file accessible.");
                if(student){if((await context.APIRequest.GetAsync(root+"/Admissions/Edit")).Status!=200)throw new Exception("Student denied editor.");}
                else{
                    foreach(var path in new[]{"/Admissions","/Admissions/Edit","/Admissions/Details/"+fixture.Id,"/Admissions/Entry?portfolioId="+fixture.Id,"/Admissions/Preview/"+fixture.Id,"/Admissions/Viewer/"+fixture.Id})if((await context.APIRequest.GetAsync(root+path)).Status!=403)throw new Exception("Non-student GET allowed: "+path);
                    await page.GotoAsync(root+"/Documents/Edit");var token=await page.Locator("main input[name=__RequestVerificationToken]").InputValueAsync();
                    foreach(var action in new[]{"Edit","Entry","DeleteEntry","Order","Generate","Delete"}){
                        var form=context.APIRequest.CreateFormData().Set("__RequestVerificationToken",token).Set("id",fixture.Id.ToString()).Set("portfolioId",fixture.Id.ToString());
                        if((await context.APIRequest.PostAsync(root+"/Admissions/"+action,new(){Form=form})).Status!=403)throw new Exception("Non-student POST allowed: "+action);
                    }
                }
                Console.WriteLine($"PASS {c.Email}: student-only menu and routes, framed icons, mobile open/close, private library and file-role restriction.");
            }
            if(errors.Count>0)throw new Exception(string.Join("\n",errors));
        }finally{
            await db.AdmissionPortfolios.IgnoreQueryFilters().Where(x=>x.Id==fixture.Id&&x.Title==fixture.Title).ExecuteDeleteAsync();
            await db.FileObjects.IgnoreQueryFilters().Where(x=>x.FileId==binary.FileId).ExecuteDeleteAsync();
        }
        Console.WriteLine("PASS student-only admissions and framed navigation; temporary fixture cleaned.");
    }
}
