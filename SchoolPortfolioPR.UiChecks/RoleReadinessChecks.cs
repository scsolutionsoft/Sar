using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;

internal static class RoleReadinessChecks
{
    public static async Task RunAsync(bool workflowsOnly = false)
    {
        const string root="http://localhost:5093";
        var credentials=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!;
        var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json")).Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});
        Directory.CreateDirectory("artifacts/ui/roles");
        var errors=new List<string>(); var pages=new Dictionary<string,IPage>();
        async Task<IPage> Login(TestCredential account){var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1000}});var page=await context.NewPageAsync();page.PageError+=(_,e)=>errors.Add(e);await page.GotoAsync(root+"/Account/Login");await page.Locator("#Email").FillAsync(account.Email);await page.Locator("#Password").FillAsync(account.Password);await page.Locator("form button").ClickAsync();await page.WaitForURLAsync(root+(account.Role=="Student"?"/Admissions":"/Dashboard"));return page;}
        async Task CheckPage(IPage page,string route,int expected=200){var response=await page.GotoAsync(root+route);if(response?.Status!=expected)throw new Exception($"{route}: expected {expected}, got {response?.Status}");}
        async Task<int> Post(IPage page,string route,Dictionary<string,string> fields){await page.GotoAsync(root+"/Documents/Edit");fields["__RequestVerificationToken"]=await page.Locator("main input[name=__RequestVerificationToken]").InputValueAsync();return await page.EvaluateAsync<int>("async a=>{const f=new FormData();for(const [k,v] of Object.entries(a.fields))f.append(k,v);return (await fetch(a.route,{method:'POST',body:f})).status}",new{route,fields});}

        foreach(var c in credentials){
            var page=await Login(c);pages[c.Email]=page;
            if(workflowsOnly) continue;
            var editor=Roles.Editors.Contains(c.Role);var admin=Roles.Admins.Contains(c.Role);var publisher=admin||c.Role=="PRAdmin";
            await CheckPage(page,"/Dashboard");await Assertions.Expect(page.Locator("[data-role-workspace]")).ToContainTextAsync(Roles.Labels[c.Role]);
            var navigation=await page.Locator("#sidebar nav a").EvaluateAllAsync<string[]>("links=>links.map(a=>a.getAttribute('href'))");
            if(navigation.Contains("/Workflows")!=editor||navigation.Contains("/Announcements")!=publisher||navigation.Contains("/Users")!=(c.Role=="SuperAdmin"))throw new Exception("Navigation mismatch for "+c.Role);
            var shortcuts=await page.Locator("[data-role-workspace] a").EvaluateAllAsync<string[]>("links=>links.map(a=>a.getAttribute('href'))");
            foreach(var route in navigation.Concat(shortcuts).Concat(new[]{"/Documents/Edit"}).Distinct())await CheckPage(page,route);
            await CheckPage(page,"/Admissions/Edit",c.Role=="Student"?200:403);
            if(navigation.Contains("/Admissions")!=(c.Role=="Student"))throw new Exception("Admissions menu is not student-only.");
            foreach(var (route,allowed) in new[]{("/Activities/Edit",editor),("/Awards/Edit",editor),("/Workflows",editor),("/Announcements/Edit",publisher),("/Publications/Edit",publisher),("/Masters/Edit?kind=Student",admin),("/Audit",admin),("/Users/Edit",c.Role=="SuperAdmin")}){
                await CheckPage(page,route,allowed?200:403);
                if(!allowed){var postRoute=route.Split('?')[0];if(postRoute.EndsWith("Edit")&&await Post(page,postRoute,new())!=403)throw new Exception("Unauthorized write permitted: "+c.Role+" "+postRoute);}
            }
            await CheckPage(page,"/Activities");await Assertions.Expect(page.Locator("a[href='/Activities/Edit']")).ToHaveCountAsync(editor?1:0);
            if(editor&&!publisher){await CheckPage(page,"/Activities/Edit");if(await page.Locator("#DepartmentId option").CountAsync()!=2)throw new Exception("Out-of-department choice visible.");}
            if(c.Role=="Student"){
                var u=await db.Users.AsNoTracking().SingleAsync(x=>x.Email==c.Email);
                await CheckPage(page,"/Portfolios?StudentId=999999&TeacherId=1",403);
                await CheckPage(page,"/Reports?StudentId=999999&TeacherId=1");
                await Assertions.Expect(page.Locator("#StudentId")).ToHaveValueAsync(u.StudentId.ToString()!);
                if(await page.Locator("#StudentId option").CountAsync()!=2||await page.Locator("#TeacherId option").CountAsync()!=1)throw new Exception("Student picker exposed other people.");
            }
            var pdf=await page.APIRequest.GetAsync(root+"/Reports?Kind=AnnualActivities&format=pdf");
            if(pdf.Status!=200||PersonalPdfService.PageCount(await pdf.BodyAsync())<1)throw new Exception("Report PDF unavailable for "+c.Role);
            await CheckPage(page,"/Dashboard");await page.ScreenshotAsync(new(){Path=$"artifacts/ui/roles/{c.Email.Split('@')[0]}.png",FullPage=true});
            await page.SetViewportSizeAsync(390,844);if(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1"))throw new Exception("Mobile overflow: "+c.Role);await page.SetViewportSizeAsync(1440,1000);
            Console.WriteLine($"PASS {c.Email}: role workspace, {navigation.Length} menu routes, shortcuts/forms, forbidden GET/POST, scoped choices, PDF and mobile.");
        }

        IPage Role(string role)=>pages[credentials.First(x=>x.Role==role).Email];
        var teacher=Role("Teacher");var department=Role("DepartmentAdmin");var approver=Role("Approver");var pr=Role("PRAdmin");var super=Role("SuperAdmin");
        var teacherUser=await db.Users.AsNoTracking().SingleAsync(x=>x.Email=="demo.teacher@school.local");
        var marker="ROLE-"+Guid.NewGuid().ToString("N")[..10];var activityIds=new List<int>();var awardIds=new List<int>();var fileIds=new HashSet<Guid>();int? tagId=null;string? userId=null;
        try{
            await teacher.GotoAsync(root+"/Activities/Edit");await teacher.Locator("#ActivityCode").FillAsync(marker);await teacher.Locator("#Title").FillAsync(marker+" activity");await teacher.Locator("#Description").FillAsync("กิจกรรมชั่วคราวเพื่อทดสอบการส่งงานตามบทบาท");await teacher.Locator("#CategoryId").SelectOptionAsync(new SelectOptionValue{Index=1});await teacher.Locator("#AcademicYearId").SelectOptionAsync(new SelectOptionValue{Index=1});await teacher.Locator("#TeacherRole").SelectOptionAsync("0");
            await teacher.Locator("main form .form-actions button").ClickAsync();await teacher.WaitForURLAsync("**/activity/*");
            var activityId=int.Parse(new Uri(teacher.Url).Segments.Last());activityIds.Add(activityId);
            var image=await teacher.Locator("h1").ScreenshotAsync();await teacher.Locator("input[type=file]").SetInputFilesAsync(new FilePayload{Name=marker+".png",MimeType="image/png",Buffer=image});await teacher.GetByRole(AriaRole.Button,new(){Name="อัปโหลด",Exact=true}).ClickAsync();await teacher.WaitForLoadStateAsync();
            await teacher.GotoAsync(root+"/Awards/Edit");await teacher.Locator("#AwardName").FillAsync(marker+" award");await teacher.Locator("#CompetitionName").FillAsync("ทดสอบการแข่งขัน");await teacher.Locator("#AwardRank").FillAsync("ตัวอย่าง");await teacher.Locator("#Organizer").FillAsync("ผู้จัดทดสอบ");await teacher.Locator("#Description").FillAsync("ทดสอบรางวัลตามลำดับงาน");await teacher.Locator("#AwardLevel").SelectOptionAsync("0");await teacher.Locator("#TeacherRole").SelectOptionAsync("0");await teacher.Locator("#AcademicYearId").SelectOptionAsync(new SelectOptionValue{Index=1});await teacher.Locator("#ActivityId").SelectOptionAsync(activityId.ToString());await teacher.Locator("main form .form-actions button").ClickAsync();
            if(teacher.Url.EndsWith("/Awards/Edit"))throw new Exception("Award validation: "+await teacher.Locator("[data-valmsg-summary]").InnerTextAsync());
            await teacher.WaitForURLAsync("**/Awards/Details/*");
            var awardId=int.Parse(new Uri(teacher.Url).Segments.Last());awardIds.Add(awardId);
            await teacher.Locator("input[type=file]").SetInputFilesAsync(new FilePayload{Name=marker+".pdf",MimeType="application/pdf",Buffer=await File.ReadAllBytesAsync("artifacts/private/student-sample-portfolio.pdf")});await teacher.GetByRole(AriaRole.Button,new(){Name="อัปโหลด",Exact=true}).ClickAsync();await teacher.WaitForLoadStateAsync();
            var otherDepartment=await db.Set<Department>().AsNoTracking().Where(x=>x.Id!=teacherUser.DepartmentId).Select(x=>x.Id).FirstAsync();
            var source=await db.Activities.AsNoTracking().SingleAsync(x=>x.Id==activityId);
            var outside=new Activity{ActivityCode=marker+"-out",Title=marker+" other department",Slug=marker.ToLowerInvariant()+"-out",DepartmentId=otherDepartment,CategoryId=source.CategoryId,AcademicYearId=source.AcademicYearId,FiscalYear=2569,ActivityDate=DateTime.Today,Status=WorkflowStatus.Submitted};db.Add(outside);await db.SaveChangesAsync();activityIds.Add(outside.Id);
            await CheckPage(department,"/activity/"+outside.Id,404);
            if(await Post(department,"/Activities/Transition",new(){["id"]=outside.Id.ToString(),["status"]="Reviewed",["rowVersion"]=Convert.ToBase64String(outside.RowVersion)})!=403)throw new Exception("Department boundary broken.");
            await CheckPage(department,"/Workflows");await Assertions.Expect(department.Locator($"[data-work-item='Activities-{outside.Id}']")).ToHaveCountAsync(0);

            foreach(var (controller,id,details) in new[]{("Activities",activityId,"/activity/"+activityId),("Awards",awardId,"/Awards/Details/"+awardId)}){
                async Task<Record> Current()=>controller=="Activities"?await db.Activities.AsNoTracking().SingleAsync(x=>x.Id==id):await db.Awards.AsNoTracking().SingleAsync(x=>x.Id==id);
                var record=await Current();if(record.CreatedBy!=teacherUser.Id)throw new Exception("Author mismatch.");
                if(await Post(pr,"/"+controller+"/Delete",new(){["id"]=id.ToString(),["rowVersion"]=Convert.ToBase64String(record.RowVersion)})!=403)throw new Exception("PR deleted another author's draft.");
                if(await Post(teacher,"/"+controller+"/Transition",new(){["id"]=id.ToString(),["status"]="Published",["rowVersion"]=Convert.ToBase64String(record.RowVersion)})!=403)throw new Exception("Teacher bypassed workflow.");
                foreach(var (actor,next) in new[]{(teacher,WorkflowStatus.Submitted),(department,WorkflowStatus.Rejected),(teacher,WorkflowStatus.Submitted),(department,WorkflowStatus.Reviewed),(approver,WorkflowStatus.Rejected),(teacher,WorkflowStatus.Submitted),(department,WorkflowStatus.Reviewed),(approver,WorkflowStatus.Approved),(pr,WorkflowStatus.Published),(pr,WorkflowStatus.Archived),(super,WorkflowStatus.Draft)}){
                    if(next is not WorkflowStatus.Archived and not WorkflowStatus.Draft){await CheckPage(actor,"/Workflows");await Assertions.Expect(actor.Locator($"[data-work-item='{controller}-{id}']")).ToHaveCountAsync(1);}
                    await CheckPage(actor,details);var form=actor.Locator($"form[action='/{controller}/Transition']").Filter(new(){Has=actor.Locator($"input[name=status][value='{next}']")});await form.Locator("button").ClickAsync();await actor.WaitForLoadStateAsync();
                    var updated=await Current();var status=updated is Activity a?a.Status:((Award)updated).Status;if(status!=next)throw new Exception("Transition failed: "+controller+" "+next);
                }
                await CheckPage(teacher,details);await teacher.Locator($"form[action='/{controller}/Delete'] button").ClickAsync();await teacher.Locator("#confirmDelete").ClickAsync();await teacher.WaitForURLAsync(root+"/"+controller);
            }
            Console.WriteLine("PASS teacher create/upload and activity + award handoff: submit, department reject/review, approver reject/approve, PR publish/archive, admin restore, owner delete; department isolation and forbidden transitions.");

            var admin=Role("Administrator");await CheckPage(admin,"/Masters/Edit?kind=Tag");await admin.Locator("#Name").FillAsync(marker);await admin.Locator("main form button").ClickAsync();await admin.WaitForURLAsync("**/Masters?kind=Tag");tagId=await db.Set<Tag>().Where(x=>x.Name==marker).Select(x=>x.Id).SingleAsync();await CheckPage(admin,"/Masters/Edit?kind=Tag&id="+tagId);await admin.Locator("#Name").FillAsync(marker+" edited");await admin.Locator("main form button").ClickAsync();await admin.WaitForLoadStateAsync();
            await Post(admin,"/Masters/Delete",new(){["kind"]="Tag",["id"]=tagId.ToString()!});if(await db.Set<Tag>().AnyAsync(x=>x.Id==tagId))throw new Exception("Master delete failed.");
            var student=await db.Users.AsNoTracking().SingleAsync(x=>x.Email=="demo.student@school.local");await Post(admin,"/Masters/Delete",new(){["kind"]="Student",["id"]=student.StudentId.ToString()!});if(!await db.Set<Student>().AnyAsync(x=>x.Id==student.StudentId))throw new Exception("Linked student deleted.");

            var temporary=new TestCredential("Teacher",marker.ToLowerInvariant()+"@example.invalid","Aa1!"+Guid.NewGuid().ToString("N"));
            await CheckPage(super,"/Users/Edit");await super.Locator("#Email").FillAsync(temporary.Email);await super.Locator("#DisplayName").FillAsync(marker);await super.Locator("#Password").FillAsync(temporary.Password);await super.Locator("#Role").SelectOptionAsync("Teacher");await super.Locator("#DepartmentId").SelectOptionAsync(teacherUser.DepartmentId.ToString()!);await super.Locator("main form button").ClickAsync();await super.WaitForURLAsync(root+"/Users");userId=await db.Users.Where(x=>x.Email==temporary.Email).Select(x=>x.Id).SingleAsync();
            var newUser=await Login(temporary);await CheckPage(newUser,"/Activities/Edit");
            await CheckPage(super,"/Users/Edit/"+userId);await super.Locator("#Role").SelectOptionAsync("Viewer");await super.Locator("main form button").ClickAsync();await super.WaitForURLAsync(root+"/Users");
            await newUser.GotoAsync(root+"/Activities/Edit");if(!newUser.Url.Contains("/Account/Login"))throw new Exception("Old role session retained after role change.");await newUser.Context.CloseAsync();
            Console.WriteLine("PASS administrator master CRUD and linked-student protection; SuperAdmin account creation/role change; old session revoked immediately.");
        }
        finally{
            // Only this run's unique records and their independently uploaded files are removed.
            var activities=await db.Activities.IgnoreQueryFilters().AsNoTracking().Where(x=>x.ActivityCode.StartsWith(marker)).Select(x=>x.Id).ToListAsync();
            var awards=await db.Awards.IgnoreQueryFilters().AsNoTracking().Where(x=>x.AwardName.StartsWith(marker)).Select(x=>x.Id).ToListAsync();
            foreach(var f in await db.Set<ActivityImage>().IgnoreQueryFilters().Where(x=>activities.Contains(x.ActivityId)).Select(x=>x.FileId).ToListAsync())fileIds.Add(f);
            foreach(var f in await db.Set<ActivityFile>().IgnoreQueryFilters().Where(x=>activities.Contains(x.ActivityId)).Select(x=>x.FileId).ToListAsync())fileIds.Add(f);
            foreach(var f in await db.Set<AwardCertificate>().IgnoreQueryFilters().Where(x=>awards.Contains(x.AwardId)).Select(x=>x.FileId).ToListAsync())fileIds.Add(f);
            await db.Set<AwardCertificate>().IgnoreQueryFilters().Where(x=>awards.Contains(x.AwardId)).ExecuteDeleteAsync();await db.Awards.IgnoreQueryFilters().Where(x=>awards.Contains(x.Id)).ExecuteDeleteAsync();
            await db.Set<ActivityImage>().IgnoreQueryFilters().Where(x=>activities.Contains(x.ActivityId)).ExecuteDeleteAsync();await db.Set<ActivityFile>().IgnoreQueryFilters().Where(x=>activities.Contains(x.ActivityId)).ExecuteDeleteAsync();await db.Activities.IgnoreQueryFilters().Where(x=>activities.Contains(x.Id)).ExecuteDeleteAsync();
            await db.FileObjects.IgnoreQueryFilters().Where(x=>fileIds.Contains(x.FileId)).ExecuteDeleteAsync();await db.Set<Tag>().IgnoreQueryFilters().Where(x=>x.Name.StartsWith(marker)).ExecuteDeleteAsync();
            var temporaryUsers=await db.Users.Where(x=>x.Email==marker.ToLower()+"@example.invalid").Select(x=>x.Id).ToListAsync();await db.UserRoles.Where(x=>temporaryUsers.Contains(x.UserId)).ExecuteDeleteAsync();await db.Users.Where(x=>temporaryUsers.Contains(x.Id)).ExecuteDeleteAsync();
        }
        if(errors.Count!=0)throw new Exception(string.Join("\n",errors));Console.WriteLine("PASS all role readiness checks; no JavaScript errors; fixtures cleaned.");
    }
}
