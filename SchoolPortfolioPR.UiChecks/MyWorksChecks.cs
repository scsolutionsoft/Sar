using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;

internal static class MyWorksChecks
{
    public static async Task CheckFormsAsync()
    {
        const string root="http://localhost:5093";
        var account=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!.First(x=>x.Role=="Student");
        var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json")).Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});var page=await browser.NewPageAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});var marker="WORKFORM-"+Guid.NewGuid().ToString("N")[..10];
        await page.GotoAsync(root+"/Account/Login");await page.Locator("#Email").FillAsync(account.Email);await page.Locator("#Password").FillAsync(account.Password);await page.Locator("form button").ClickAsync();await page.WaitForURLAsync("**/Admissions");
        try{
            await page.GotoAsync(root+"/MyWorks");foreach(var label in new[]{"เพิ่มกิจกรรม","เพิ่มรางวัล","เพิ่มเกียรติบัตร","เพิ่มอัลบั้มภาพ"})await Assertions.Expect(page.Locator(".mywork-shortcuts").GetByText(label,new(){Exact=true})).ToBeVisibleAsync();
            var drawing=await browser.NewPageAsync();await drawing.SetViewportSizeAsync(640,400);await drawing.SetContentAsync("<body style='background:#e7f5ed;color:#217560;font:32px Arial;padding:50px'>Learning together</body>");var png=await drawing.ScreenshotAsync();await drawing.CloseAsync();
            await page.GotoAsync(root+"/MyWorks/Edit?kind=Activity");await Assertions.Expect(page.Locator(".mywork-savebar button")).ToHaveTextAsync("บันทึกกิจกรรม");await Assertions.Expect(page.Locator(".section-title h2").First).ToHaveTextAsync("รายละเอียดกิจกรรม");
            await page.Locator("#Title").FillAsync("กิจกรรมจิตอาสาของฉัน "+marker);await page.Locator("#Description").FillAsync("เรียนรู้การทำงานร่วมกันและแบ่งปันให้ชุมชน");await page.Locator("#Uploads").SetInputFilesAsync(new FilePayload{Name=marker+".png",MimeType="image/png",Buffer=png});await page.Locator(".mywork-savebar button").ClickAsync();await page.WaitForURLAsync("**/MyWorks/Details/*");var id=int.Parse(page.Url.Split('/').Last());
            await page.GotoAsync(root+"/MyWorks/Edit/"+id);await page.Locator("#Location").FillAsync("โรงเรียน");await page.Locator(".mywork-savebar button").ClickAsync();await page.WaitForURLAsync("**/MyWorks/Details/"+id);if((await db.PersonalWorks.AsNoTracking().SingleAsync(x=>x.Id==id)).Location!="โรงเรียน")throw new Exception("Owner metadata edit failed");
            var before=await db.FileObjects.CountAsync(x=>x.OriginalFileName.StartsWith(marker));
            await page.Locator("#workFiles").SetInputFilesAsync(new[]{new FilePayload{Name=marker+"-valid.png",MimeType="image/png",Buffer=png},new FilePayload{Name=marker+"-bad.png",MimeType="image/png",Buffer=new byte[20]}});await page.Locator("[data-work-upload] button").ClickAsync();await page.WaitForLoadStateAsync();if(await db.FileObjects.CountAsync(x=>x.OriginalFileName.StartsWith(marker))!=before)throw new Exception("Failed batch partially persisted");
            await page.Locator("#workFiles").SetInputFilesAsync(new FilePayload{Name=marker+"-extra.png",MimeType="image/png",Buffer=png});await page.Locator("#uploadCaption").FillAsync("ภาพเพิ่มเติม");await page.Locator("[data-work-upload] button").ClickAsync();await page.WaitForLoadStateAsync();await Assertions.Expect(page.Locator("[data-work-media]")).ToHaveCountAsync(2);
            var removed=await db.PersonalWorkMedia.AsNoTracking().Where(x=>x.PersonalWorkId==id).OrderByDescending(x=>x.Id).FirstAsync();await page.Locator("[data-work-media='"+removed.Id+"'] form[action='/MyWorks/DeleteMedia'] button").ClickAsync();await page.Locator("#confirmDelete").ClickAsync();await page.WaitForLoadStateAsync();await Assertions.Expect(page.Locator("[data-work-media]")).ToHaveCountAsync(1);if((await page.Context.APIRequest.GetAsync(root+"/files/"+removed.FileId)).Status!=404)throw new Exception("Removed image accessible");
            await page.GotoAsync(root+"/MyWorks?q="+marker);await page.Locator(".mywork-cover img").ScrollIntoViewIfNeededAsync();await page.WaitForFunctionAsync("()=>Array.from(document.querySelectorAll('.mywork-cover img')).every(x=>x.complete&&x.naturalWidth>0)");await page.EvaluateAsync("window.scrollTo(0,0)");Directory.CreateDirectory("artifacts/ui/myworks");await page.ScreenshotAsync(new(){Path="artifacts/ui/myworks/final-desktop.png",FullPage=true});
            await page.SetViewportSizeAsync(390,844);await page.ScreenshotAsync(new(){Path="artifacts/ui/myworks/final-mobile.png",FullPage=true});await page.GotoAsync(root+"/MyWorks/Edit/"+id);if(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1"))throw new Exception("Editor mobile overflow");await page.ScreenshotAsync(new(){Path="artifacts/ui/myworks/final-form-mobile.png",FullPage=true});
            Console.WriteLine("PASS final Thai labels, owner metadata edit, atomic batch rejection, additional upload, confirmed media removal, revoked file access, loaded cover and mobile editor.");
        }finally{
            var ids=await db.PersonalWorks.IgnoreQueryFilters().Where(x=>x.Title.Contains(marker)).Select(x=>x.Id).ToListAsync();await db.PersonalWorkMedia.IgnoreQueryFilters().Where(x=>ids.Contains(x.PersonalWorkId)).ExecuteDeleteAsync();await db.PersonalWorks.IgnoreQueryFilters().Where(x=>ids.Contains(x.Id)).ExecuteDeleteAsync();await db.FileObjects.IgnoreQueryFilters().Where(x=>x.OriginalFileName.StartsWith(marker)).ExecuteDeleteAsync();
        }
    }
    public static async Task RunAsync()
    {
        const string root="http://localhost:5093";
        var accounts=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json")).Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});
        var marker="MYWORK-"+Guid.NewGuid().ToString("N")[..10];var sessions=new List<(IBrowserContext Context,IPage Page,PersonalWork Work)>();var errors=new List<string>();
        Directory.CreateDirectory("artifacts/ui/myworks");
        var drawing=await browser.NewPageAsync();await drawing.SetViewportSizeAsync(640,400);await drawing.SetContentAsync("<body style='background:#e7f5ed;color:#217560;font:32px Arial;padding:50px'>My achievement<br><small>Learning together</small></body>");var png=await drawing.ScreenshotAsync();var pdf=await drawing.PdfAsync();await drawing.CloseAsync();
        async Task<IAPIResponse> Post(IPage page,string action,Dictionary<string,object> fields)
        {
            fields["__RequestVerificationToken"]=(await page.Locator("input[name=__RequestVerificationToken]").First.InputValueAsync());
            var form=page.Context.APIRequest.CreateFormData();foreach(var (key,value) in fields)form.Set(key,value.ToString()!);
            return await page.Context.APIRequest.PostAsync(root+"/MyWorks/"+action,new(){Form=form,MaxRedirects=0});
        }
        async Task<string> Version(IPage page,int id){await page.GotoAsync(root+"/MyWorks/Details/"+id);return await page.Locator("input[name=rowVersion]").First.InputValueAsync();}
        static void Check(bool good,string error){if(!good)throw new Exception(error);}
        try{
            foreach(var account in accounts){
                var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});var page=await context.NewPageAsync();page.PageError+=(_,e)=>errors.Add(e);
                await page.GotoAsync(root+"/Account/Login");await page.Locator("#Email").FillAsync(account.Email);await page.Locator("#Password").FillAsync(account.Password);await page.Locator("form button").ClickAsync();await page.WaitForURLAsync(root+(account.Role=="Student"?"/Admissions":"/Dashboard"));
                await Assertions.Expect(page.Locator("#sidebar a[href='/MyWorks']")).ToHaveCountAsync(1);
                var userId=await db.Users.Where(x=>x.Email==account.Email).Select(x=>x.Id).SingleAsync();PersonalWork? certificate=null;
                foreach(var kind in Enum.GetValues<PersonalWorkKind>()){
                    await page.GotoAsync(root+"/MyWorks/Edit?kind="+kind);await page.Locator("#Title").FillAsync(marker+" "+account.Email+" "+kind);await page.Locator("#Description").FillAsync("ทดสอบผลงานส่วนตัวและหลักฐาน");
                    await page.Locator("#Uploads").SetInputFilesAsync(new FilePayload{Name=marker+(kind==PersonalWorkKind.Certificate?".pdf":".png"),MimeType=kind==PersonalWorkKind.Certificate?"application/pdf":"image/png",Buffer=kind==PersonalWorkKind.Certificate?pdf:png});
                    await page.Locator(".mywork-savebar button").ClickAsync();await page.WaitForURLAsync("**/MyWorks/Details/*");
                    var work=await db.PersonalWorks.AsNoTracking().SingleAsync(x=>x.OwnerId==userId&&x.Title==marker+" "+account.Email+" "+kind);
                    var media=await db.PersonalWorkMedia.AsNoTracking().Include(x=>x.File).SingleAsync(x=>x.PersonalWorkId==work.Id);
                    Check(media.File!.FileData.SequenceEqual(kind==PersonalWorkKind.Certificate?pdf:png),"Binary upload mismatch");
                    if(kind==PersonalWorkKind.Certificate)certificate=work;
                    if(kind==PersonalWorkKind.Album){
                        var form=page.Locator("[data-work-link]");await form.Locator("..").Locator("summary").ClickAsync();await page.Locator("#cloudUrl").FillAsync("https://example.com/album");await page.Locator("#cloudCaption").FillAsync("ภาพบนคลาวด์");await form.Locator("button").ClickAsync();await page.WaitForLoadStateAsync();
                        var cloud=page.Locator(".mywork-cloud");await Assertions.Expect(cloud).ToHaveAttributeAsync("href","https://example.com/album");await Assertions.Expect(cloud).ToHaveAttributeAsync("target","_blank");Check(await cloud.GetAttributeAsync("data-media-preview")==null,"Cloud should navigate directly");
                        var caption=page.Locator("[data-work-media='"+media.Id+"'] .mywork-caption-editor");await caption.Locator("summary").ClickAsync();await caption.Locator("textarea").FillAsync("คำบรรยาย <b>ภาพกิจกรรม</b>");await caption.Locator("button").ClickAsync();await page.WaitForLoadStateAsync();await Assertions.Expect(page.Locator("[data-work-media='"+media.Id+"'] .mywork-media-copy > p")).ToHaveTextAsync("คำบรรยาย <b>ภาพกิจกรรม</b>");
                        await page.Locator("[data-media-type=image]").First.ClickAsync();await Assertions.Expect(page.Locator("#mediaSurface img")).ToBeVisibleAsync();await page.Keyboard.PressAsync("Escape");
                        var v=await Version(page,work.Id);var count=await db.PersonalWorkMedia.CountAsync(x=>x.PersonalWorkId==work.Id);
                        await Post(page,"Link",new(){["id"]=work.Id,["rowVersion"]=v,["url"]="javascript:alert(1)"});Check(await db.PersonalWorkMedia.CountAsync(x=>x.PersonalWorkId==work.Id)==count,"Unsafe URL saved");
                        await page.Locator("#workFiles").SetInputFilesAsync(new FilePayload{Name=marker+".pdf",MimeType="application/pdf",Buffer=pdf});await page.Locator("[data-work-upload] button").ClickAsync();await page.WaitForLoadStateAsync();Check(await db.PersonalWorkMedia.CountAsync(x=>x.PersonalWorkId==work.Id)==count,"Album accepted PDF");
                    }
                }
                sessions.Add((context,page,certificate!));
                await page.GotoAsync(root+"/MyWorks?q="+marker);await Assertions.Expect(page.Locator("[data-work-id]")).ToHaveCountAsync(4);
                await page.ScreenshotAsync(new(){Path="artifacts/ui/myworks/"+account.Email.Split('@')[0]+"-desktop.png",FullPage=true});
                if(account.Role is "Student" or "SuperAdmin"){
                    await page.SetViewportSizeAsync(390,844);Check(!await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1"),"Index mobile overflow");await page.ScreenshotAsync(new(){Path="artifacts/ui/myworks/"+account.Email.Split('@')[0]+"-mobile.png",FullPage=true});
                    await Version(page,certificate!.Id);Check(!await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1"),"Detail mobile overflow");await page.ScreenshotAsync(new(){Path="artifacts/ui/myworks/"+account.Email.Split('@')[0]+"-detail-mobile.png",FullPage=true});await page.SetViewportSizeAsync(1440,1000);
                }
                Console.WriteLine("PASS "+account.Email+": four categories, SQL binary files, image preview, cloud links, caption escaping, validation, search and responsive layout.");
            }
            // Every account attempts to read/mutate another account's records, including administrators.
            for(var i=0;i<sessions.Count;i++){
                var (context,page,own)=sessions[i];var foreign=sessions[(i+1)%sessions.Count].Work;var foreignMedia=await db.PersonalWorkMedia.AsNoTracking().SingleAsync(x=>x.PersonalWorkId==foreign.Id);
                foreach(var route in new[]{"/MyWorks/Details/"+foreign.Id,"/MyWorks/Edit/"+foreign.Id,"/files/"+foreignMedia.FileId,"/files/"+foreignMedia.FileId+"?download=true"})Check((await context.APIRequest.GetAsync(root+route)).Status==404,"Foreign read allowed: "+route);
                var v=await Version(page,own.Id);
                foreach(var action in new[]{"Delete","DeleteMedia","Media","Link","Upload","SavePdf","Edit"}){
                    var res=await Post(page,action,new(){["id"]=foreign.Id,["mediaId"]=foreignMedia.Id,["rowVersion"]=Convert.ToBase64String(foreign.RowVersion),["Title"]="forged",["Kind"]="Certificate",["WorkDate"]="2026-09-27",["url"]="https://example.com"});Check(res.Status==404,"Foreign mutation allowed: "+action+" status "+res.Status);
                }
                Check((await Post(page,"Media",new(){["id"]=own.Id,["mediaId"]=foreignMedia.Id,["rowVersion"]=v,["caption"]="forged"})).Status==404,"Cross-parent media accepted");
            }
            foreach(var (context,page,own) in sessions){
                var v=await Version(page,own.Id);
                var local=await db.PersonalWorkMedia.AsNoTracking().SingleAsync(x=>x.PersonalWorkId==own.Id);
                await page.Locator("[data-media-type=pdf]").ClickAsync();await Assertions.Expect(page.Locator("#mediaSurface canvas")).ToBeVisibleAsync(new(){Timeout=60000});await page.Keyboard.PressAsync("Escape");
                Check((await Post(page,"Media",new(){["id"]=own.Id,["mediaId"]=local.Id,["rowVersion"]=v,["caption"]="บันทึกใหม่"})).Status==302,"Caption update failed");
                Check((await Post(page,"Delete",new(){["id"]=own.Id,["rowVersion"]=v})).Status==409,"Stale delete allowed");
                v=await Version(page,own.Id);Check((await Post(page,"SavePdf",new(){["id"]=own.Id,["mediaId"]=local.Id})).Status==302,"PDF copy failed");await Post(page,"SavePdf",new(){["id"]=own.Id,["mediaId"]=local.Id});
                Check(await db.PersonalDocuments.CountAsync(x=>x.OwnerId==own.OwnerId&&x.FileId==local.FileId)==1,"PDF duplicate saved");
                Check((await Post(page,"Delete",new(){["id"]=own.Id,["rowVersion"]=v})).Status==302,"Owner deletion failed");Check((await context.APIRequest.GetAsync(root+"/MyWorks/Details/"+own.Id)).Status==404,"Deleted work readable");Check((await context.APIRequest.GetAsync(root+"/files/"+local.FileId)).Status==200,"Saved PDF lost after work deletion");
            }
            await using var anonymous=await browser.NewContextAsync();Check((await anonymous.APIRequest.GetAsync(root+"/MyWorks",new(){MaxRedirects=0})).Status==302,"Anonymous collection accessible");
            Check(errors.Count==0,string.Join("\n",errors));Console.WriteLine("PASS all roles: owner-only read/write/files, stale-write rejection, PDF popup, idempotent PDF copy, soft deletion and preserved document copies.");
        }finally{
            var workIds=await db.PersonalWorks.IgnoreQueryFilters().Where(x=>x.Title.StartsWith(marker)).Select(x=>x.Id).ToListAsync();
            var fileIds=await db.FileObjects.IgnoreQueryFilters().Where(x=>x.OriginalFileName.StartsWith(marker)).Select(x=>x.FileId).ToListAsync();
            await db.PersonalDocuments.IgnoreQueryFilters().Where(x=>fileIds.Contains(x.FileId)).ExecuteDeleteAsync();
            await db.PersonalWorkMedia.IgnoreQueryFilters().Where(x=>workIds.Contains(x.PersonalWorkId)).ExecuteDeleteAsync();
            await db.PersonalWorks.IgnoreQueryFilters().Where(x=>workIds.Contains(x.Id)).ExecuteDeleteAsync();
            await db.FileObjects.IgnoreQueryFilters().Where(x=>fileIds.Contains(x.FileId)).ExecuteDeleteAsync();
            foreach(var s in sessions)await s.Context.DisposeAsync();
        }
    }
}
