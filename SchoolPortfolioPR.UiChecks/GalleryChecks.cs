using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;

internal static class GalleryChecks
{
    public static async Task RunAsync()
    {
        const string root="http://localhost:5093";
        var accounts=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var teacher=accounts.First(x=>x.Role=="Teacher");var student=accounts.First(x=>x.Role=="Student");
        var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json")).Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        var owner=await db.Users.SingleAsync(x=>x.Email==teacher.Email);var marker="GALLERY-"+Guid.NewGuid().ToString("N")[..10];
        var year=await db.Set<AcademicYear>().Select(x=>x.Id).FirstAsync();var category=await db.Set<ActivityCategory>().Select(x=>x.Id).FirstAsync();
        Activity Fixture(string suffix)=>new(){ActivityCode=marker+suffix,Title=marker+suffix,Slug=(marker+suffix).ToLowerInvariant(),DepartmentId=owner.DepartmentId!.Value,CategoryId=category,AcademicYearId=year,ActivityDate=DateTime.Today,Status=WorkflowStatus.Draft};
        var own=Fixture("-own");var other=Fixture("-other");db.AddRange(own,other);await db.SaveChangesAsync();
        await db.Activities.Where(x=>x.Id==own.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CreatedBy,owner.Id));
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});var contexts=new List<IBrowserContext>();var errors=new List<string>();Directory.CreateDirectory("artifacts/ui/gallery");
        async Task<(IBrowserContext Context,IPage Page)> Login(TestCredential c){var context=await browser.NewContextAsync();contexts.Add(context);var p=await context.NewPageAsync();p.PageError+=(_,e)=>errors.Add(e);await p.GotoAsync(root+"/Account/Login");await p.Locator("#Email").FillAsync(c.Email);await p.Locator("#Password").FillAsync(c.Password);await p.Locator("form button").ClickAsync();await p.WaitForURLAsync(root+(c.Role=="Student"?"/Admissions":"/Dashboard"));return(context,p);}
        try{
            var (context,page)=await Login(teacher);
            if((await context.APIRequest.GetAsync(root+"/Gallery/Entry?activityId="+other.Id)).Status!=403)throw new Exception("Teacher may edit another owner's album.");
            await page.GotoAsync(root+"/Gallery/Entry?activityId="+own.Id);
            var drawing=await context.NewPageAsync();await drawing.SetViewportSizeAsync(400,240);await drawing.SetContentAsync("<body style='background:#3d77aa;color:white;font:30px Arial;padding:40px'>Activity gallery</body>");var png=await drawing.ScreenshotAsync();await drawing.CloseAsync();
            var caption="ภาพกิจกรรม <b>ทดสอบ</b> "+marker;
            await page.Locator("#Files").SetInputFilesAsync(new[]{new FilePayload{Name=marker+"-1.png",MimeType="image/png",Buffer=png},new FilePayload{Name=marker+"-2.png",MimeType="image/png",Buffer=png}});
            await page.Locator("#Caption").FillAsync(caption);await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/activity/"+own.Id+"#activityGallery");
            var images=await db.Set<ActivityImage>().AsNoTracking().Include(x=>x.File).Where(x=>x.ActivityId==own.Id).OrderBy(x=>x.SortOrder).ToListAsync();
            if(images.Count!=2||images.Any(x=>x.Caption!=caption||!x.File.FileData.SequenceEqual(png)))throw new Exception("Batch image/caption binary persistence failed.");
            await Assertions.Expect(page.Locator("[data-gallery-item]")).ToHaveCountAsync(2);await Assertions.Expect(page.Locator("[data-gallery-item] b")).ToHaveCountAsync(0);
            await page.Locator("[data-gallery-item] [data-media-preview]").First.ClickAsync();await Assertions.Expect(page.Locator("#mediaSurface img")).ToBeVisibleAsync();await Assertions.Expect(page.Locator("#mediaTitle")).ToHaveTextAsync(caption);await page.Keyboard.PressAsync("Escape");
            await page.GotoAsync(root+$"/Gallery/Entry?activityId={own.Id}&fileId={images[0].FileId}");await page.Locator("#Caption").FillAsync("คำบรรยายรายภาพ "+marker);await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/activity/"+own.Id+"#activityGallery");
            if((await db.Set<ActivityImage>().AsNoTracking().SingleAsync(x=>x.ActivityId==own.Id&&x.FileId==images[0].FileId)).Caption!="คำบรรยายรายภาพ "+marker)throw new Exception("Individual caption update failed.");
            await page.GotoAsync(root+$"/Gallery/Entry?activityId={own.Id}&source=External");
            var token=await page.Locator("main input[name=__RequestVerificationToken]").InputValueAsync();var version=await page.Locator("#RowVersion").InputValueAsync();
            var invalid=context.APIRequest.CreateFormData().Set("__RequestVerificationToken",token).Set("ActivityId",own.Id.ToString()).Set("Source","External").Set("RowVersion",version).Set("ExternalUrl","javascript:alert(1)");
            var response=await context.APIRequest.PostAsync(root+"/Gallery/Entry",new(){Form=invalid});if(!(await response.TextAsync()).Contains("validation-summary-errors")||await db.ActivityImageLinks.AnyAsync(x=>x.ActivityId==own.Id))throw new Exception("Unsafe cloud URL accepted.");
            const string cloud="https://drive.google.com/drive/folders/gallery-check?usp=sharing";
            await page.Locator("#ExternalUrl").FillAsync(cloud);await page.Locator("#Caption").FillAsync("อัลบั้มคลาวด์ "+marker);await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/activity/"+own.Id+"#activityGallery");
            var link=await db.ActivityImageLinks.AsNoTracking().SingleAsync(x=>x.ActivityId==own.Id);
            await Assertions.Expect(page.Locator("[data-gallery-item]")).ToHaveCountAsync(3);
            var external=page.Locator("[data-gallery-item] a.gallery-cloud");await Assertions.Expect(external).Not.ToHaveAttributeAsync("data-media-preview","");
            await context.RouteAsync("https://drive.google.com/**",r=>r.FulfillAsync(new(){ContentType="text/html",Body="<h1>Cloud album test destination</h1>"}));
            var popup=await page.RunAndWaitForPopupAsync(()=>external.ClickAsync());await popup.WaitForLoadStateAsync();if(popup.Url!=cloud)throw new Exception("Cloud link did not open the original URL.");await popup.CloseAsync();await Assertions.Expect(page.Locator("#mediaViewer")).ToBeHiddenAsync();
            var stale=context.APIRequest.CreateFormData().Set("__RequestVerificationToken",token).Set("ActivityId",own.Id.ToString()).Set("Source","External").Set("RowVersion",version).Set("ExternalUrl",cloud).Set("LinkId",link.Id.ToString());
            if((await context.APIRequest.PostAsync(root+"/Gallery/Entry",new(){Form=stale})).Status!=409)throw new Exception("Stale gallery edit accepted.");
            await page.GotoAsync(root+$"/Gallery/Entry?activityId={own.Id}&linkId={link.Id}");await page.Locator("#Caption").FillAsync("คลาวด์แก้ไข "+marker);await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/activity/"+own.Id+"#activityGallery");
            await page.SetViewportSizeAsync(390,844);if(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1"))throw new Exception("Gallery mobile overflow.");await page.Locator("#activityGallery").ScreenshotAsync(new(){Path="artifacts/ui/gallery/album-mobile.png"});
            await page.GotoAsync(root+"/Gallery?q="+Uri.EscapeDataString("คลาวด์แก้ไข "+marker));await Assertions.Expect(page.Locator(".gallery-albums .gallery-entry")).ToHaveCountAsync(1);
            // A valid first image followed by a corrupt image must not partially persist.
            await page.GotoAsync(root+"/Gallery/Entry?activityId="+own.Id);await page.Locator("#Files").SetInputFilesAsync(new[]{new FilePayload{Name=marker+"-rollback.png",MimeType="image/png",Buffer=png},new FilePayload{Name=marker+"-bad.png",MimeType="image/png",Buffer="bad-image-content"u8.ToArray()}});await page.Locator(".form-actions button").ClickAsync();await Assertions.Expect(page.Locator(".validation-summary-errors")).ToBeVisibleAsync();if(await db.FileObjects.AnyAsync(x=>x.OriginalFileName==marker+"-rollback.png"))throw new Exception("Failed batch partially persisted.");
            Console.WriteLine("PASS teacher: batch upload/binary/captions, per-image editing, popup preview, cloud add/edit/direct navigation, URL validation, stale version, search, mobile, foreign-owner denial and atomic batch failure.");
            await db.Activities.Where(x=>x.Id==own.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,WorkflowStatus.Published).SetProperty(x=>x.IsPublic,true));
            var (studentContext,studentPage)=await Login(student);await studentPage.GotoAsync(root+"/activity/"+own.Id);await Assertions.Expect(studentPage.Locator("[data-gallery-item]")).ToHaveCountAsync(3);await Assertions.Expect(studentPage.Locator("a[href^='/Gallery/Entry']")).ToHaveCountAsync(0);
            if((await studentContext.APIRequest.GetAsync(root+"/Gallery/Entry?activityId="+own.Id)).Status!=403)throw new Exception("Student gallery edit accessible.");
            await studentPage.GotoAsync(root+"/Documents/Edit");var studentToken=await studentPage.Locator("main input[name=__RequestVerificationToken]").InputValueAsync();var denied=studentContext.APIRequest.CreateFormData().Set("__RequestVerificationToken",studentToken).Set("activityId",own.Id.ToString()).Set("linkId",link.Id.ToString());foreach(var action in new[]{"Entry","DeleteLink"})if((await studentContext.APIRequest.PostAsync(root+"/Gallery/"+action,new(){Form=denied})).Status!=403)throw new Exception("Student changed gallery.");
            await db.Activities.Where(x=>x.Id==own.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,WorkflowStatus.Draft).SetProperty(x=>x.IsPublic,false));
            if((await studentContext.APIRequest.GetAsync(root+"/activity/"+own.Id)).Status!=404||(await studentContext.APIRequest.GetAsync(root+"/files/"+images[0].FileId)).Status!=404)throw new Exception("Private gallery/file visible.");
            await page.GotoAsync(root+"/activity/"+own.Id);var delete=page.Locator("form[action='/Gallery/DeleteLink']");var deleteToken=await delete.Locator("input[name=__RequestVerificationToken]").InputValueAsync();var current=await delete.Locator("input[name=rowVersion]").InputValueAsync();var remove=context.APIRequest.CreateFormData().Set("__RequestVerificationToken",deleteToken).Set("activityId",own.Id.ToString()).Set("linkId",link.Id.ToString()).Set("rowVersion",current);if((await context.APIRequest.PostAsync(root+"/Gallery/DeleteLink",new(){Form=remove})).Status!=200||await db.ActivityImageLinks.AnyAsync(x=>x.Id==link.Id))throw new Exception("Cloud link removal failed.");
            if(errors.Count>0)throw new Exception(string.Join("\n",errors));Console.WriteLine("PASS read-only student, private album/file restrictions and soft deletion of cloud links; no JavaScript errors.");
        }finally{
            foreach(var c in contexts)await c.DisposeAsync();var ids=new[]{own.Id,other.Id};var files=await db.Set<ActivityImage>().IgnoreQueryFilters().Where(x=>ids.Contains(x.ActivityId)).Select(x=>x.FileId).ToListAsync();
            await db.Activities.IgnoreQueryFilters().Where(x=>ids.Contains(x.Id)).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CoverImageId,(Guid?)null));await db.ActivityImageLinks.IgnoreQueryFilters().Where(x=>ids.Contains(x.ActivityId)).ExecuteDeleteAsync();await db.Set<ActivityImage>().IgnoreQueryFilters().Where(x=>ids.Contains(x.ActivityId)).ExecuteDeleteAsync();await db.Activities.IgnoreQueryFilters().Where(x=>ids.Contains(x.Id)&&x.ActivityCode.StartsWith(marker)).ExecuteDeleteAsync();await db.FileObjects.IgnoreQueryFilters().Where(x=>files.Contains(x.FileId)).ExecuteDeleteAsync();
        }
        Console.WriteLine("PASS activity gallery; temporary records/files cleaned.");
    }
}
