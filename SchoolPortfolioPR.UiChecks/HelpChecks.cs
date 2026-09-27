using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;

internal static class HelpChecks
{
    public static async Task RunAsync()
    {
        const string root="http://localhost:5093";
        var accounts=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json")).Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});var errors=new List<string>();var keys=new HashSet<string>();Directory.CreateDirectory("artifacts/ui/help");
        foreach(var account in accounts){
            await using var context=await browser.NewContextAsync();var page=await context.NewPageAsync();page.PageError+=(_,e)=>errors.Add(e);
            await page.GotoAsync(root+"/Account/Login");await page.Locator("#Email").FillAsync(account.Email);await page.Locator("#Password").FillAsync(account.Password);await page.Locator("form button").ClickAsync();await page.WaitForURLAsync(root+(account.Role=="Student"?"/Admissions":"/Dashboard"));
            await Assertions.Expect(page.Locator("#sidebar a[href='/Help']")).ToHaveCountAsync(1);await Assertions.Expect(page.Locator(".help-entry-banner a")).ToBeVisibleAsync();
            await page.GotoAsync(root+"/Help");var key=(await page.Locator("[data-help-key]").GetAttributeAsync("data-help-key"))!;if(!keys.Add(key))throw new Exception("Help progress key shared by accounts.");
            var topics=await page.Locator(".help-topic").EvaluateAllAsync<string[]>("els=>els.map(e=>e.dataset.helpTopic)");var expected=new List<string>{"start","myworks","activities","awards","gallery","publications","documents","reports",account.Role=="Student"?"admissions":"portfolios"};
            if(account.Role is not ("Student" or "Viewer"))expected.Add("workflow");if(account.Role is "SuperAdmin" or "Administrator" or "PRAdmin")expected.Add("announcements");if(account.Role is "SuperAdmin" or "Administrator")expected.AddRange(new[]{"masters","branding","audit"});if(account.Role=="SuperAdmin")expected.Add("users");
            if(!topics.Order().SequenceEqual(expected.Order()))throw new Exception("Help topics mismatch for "+account.Role);
            foreach(var batch in topics.Chunk(3)){var checks=await Task.WhenAll(batch.Select(async topic=>{var r=await context.APIRequest.GetAsync(root+"/Help/Wizard?topic="+topic);return r.Status;}));if(checks.Any(x=>x!=200))throw new Exception("Authorized guide unavailable.");}
            foreach(var forbidden in new[]{"users","branding","announcements","workflow","admissions","portfolios"}.Except(topics))if((await context.APIRequest.GetAsync(root+"/Help/Wizard?topic="+forbidden)).Status!=404)throw new Exception("Forbidden guide exposed: "+forbidden);
            if((await context.APIRequest.GetAsync(root+"/Help/Wizard?topic=start&step=99")).Status!=400)throw new Exception("Invalid wizard step accepted.");
            await page.GotoAsync(root+"/Help/Wizard?topic=start");await page.Locator("[data-help-next]").ClickAsync();await Assertions.Expect(page.Locator(".help-wizard")).ToHaveAttributeAsync("data-help-step","2");
            await page.GotoAsync(root+"/Help");await Assertions.Expect(page.Locator("[data-help-topic=start] [data-help-resume]")).ToHaveTextAsync("อ่านต่อ");await page.Locator("[data-help-topic=start] [data-help-resume]").ClickAsync();await Assertions.Expect(page.Locator(".help-wizard")).ToHaveAttributeAsync("data-help-step","2");
            var work=await page.RunAndWaitForPopupAsync(()=>page.Locator("[data-help-action]").ClickAsync());await work.WaitForLoadStateAsync();if(!work.Url.StartsWith(root))throw new Exception("Help action left local application.");await work.CloseAsync();
            await page.Locator("[data-help-next]").ClickAsync();await page.Locator("[data-help-complete]").ClickAsync();await Assertions.Expect(page.Locator("[data-help-topic=start] [data-help-status]")).ToContainTextAsync("อ่านครบแล้ว");
            await page.GotoAsync(root+"/Help/Wizard?topic=start&step=3");await page.Locator("[data-help-restart]").ClickAsync();await Assertions.Expect(page.Locator(".help-wizard")).ToHaveAttributeAsync("data-help-step","1");await Assertions.Expect(page.Locator(".help-wizard [data-help-status]")).Not.ToContainTextAsync("อ่านหัวข้อนี้ครบแล้ว");
            if(account.Role is "Student" or "SuperAdmin"){
                await page.SetViewportSizeAsync(390,844);if(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1"))throw new Exception("Help mobile overflow.");await page.ScreenshotAsync(new(){Path="artifacts/ui/help/"+account.Email.Split('@')[0]+"-mobile.png",FullPage=true});
                await using var plain=await browser.NewContextAsync(new(){JavaScriptEnabled=false,StorageState=await context.StorageStateAsync()});var p=await plain.NewPageAsync();await p.GotoAsync(root+"/Help/Wizard?topic=start");await p.Locator("[data-help-next]").ClickAsync();await Assertions.Expect(p.Locator(".help-wizard")).ToHaveAttributeAsync("data-help-step","2");
            }
            Console.WriteLine("PASS "+account.Email+": role topics, authorized/forbidden routes, guide steps, new-tab action, resume, completion and restart.");
            if(account.Role=="Teacher"){
                var marker="AUTO-"+Guid.NewGuid().ToString("N")[..10];
                try{
                    async Task<Activity> Create(string suffix){await page.GotoAsync(root+"/Activities/Edit");await Assertions.Expect(page.Locator("#Slug")).Not.ToBeEditableAsync();await page.Locator("#ActivityCode").FillAsync(marker+suffix);await page.Locator("#Title").FillAsync("ข่าวชื่อซ้ำทดสอบ "+marker);await page.Locator("#Description").FillAsync("ทดสอบลิงก์อัตโนมัติ");await page.Locator("#CategoryId").SelectOptionAsync(new SelectOptionValue{Index=1});await page.Locator("#AcademicYearId").SelectOptionAsync(new SelectOptionValue{Index=1});await page.Locator("#TeacherRole").SelectOptionAsync("0");await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/activity/*");return await db.Activities.AsNoTracking().SingleAsync(x=>x.ActivityCode==marker+suffix);}
                    var one=await Create("-1");var two=await Create("-2");if(one.Slug==two.Slug||!Regex.IsMatch(one.Slug,"^[a-z0-9]+(?:-[a-z0-9]+)*$"))throw new Exception("Automatic slugs invalid or duplicate.");
                    await page.GotoAsync(root+"/Activities/Edit/"+one.Id);await page.Locator("#Title").FillAsync("");await page.EvaluateAsync("document.querySelector('main form').noValidate=true");await page.Locator(".form-actions button").ClickAsync();await Assertions.Expect(page.Locator(".validation-summary-errors")).ToBeVisibleAsync();await Assertions.Expect(page.Locator("#Slug")).ToHaveValueAsync(one.Slug);
                    await page.Locator("#Title").FillAsync("แก้ชื่อข่าว "+marker);await page.EvaluateAsync("document.querySelector('#Slug').value='forged-slug-change'");await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/activity/"+one.Id);if((await db.Activities.AsNoTracking().SingleAsync(x=>x.Id==one.Id)).Slug!=one.Slug)throw new Exception("Edit/forged POST changed existing slug.");
                    var url=root+$"/news/{one.ActivityDate.Year+543}/{one.Slug}";if((await context.APIRequest.GetAsync(url)).Status!=404)throw new Exception("Draft news public URL accessible.");await db.Activities.Where(x=>x.Id==one.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,WorkflowStatus.Published).SetProperty(x=>x.IsPublic,true));if((await context.APIRequest.GetAsync(url)).Status!=200)throw new Exception("Generated public URL failed after publication.");
                    Console.WriteLine("PASS automatic slug: create without entry, same-title uniqueness, validation redisplay, stable edit, forged value ignored and publication-gated URL.");
                }finally{await db.Activities.IgnoreQueryFilters().Where(x=>x.ActivityCode.StartsWith(marker)).ExecuteDeleteAsync();}
            }
        }
        if(errors.Count>0)throw new Exception(string.Join("\n",errors));Console.WriteLine("PASS guides for all roles and automatic slugs; no JavaScript errors; temporary activities cleaned.");
    }
}
