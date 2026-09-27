using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;

internal static class ParticipantPickerChecks
{
    public static async Task RunAsync()
    {
        const string root="http://localhost:5093";
        var account=JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync("artifacts/private/test-accounts.json"))!.Single(x=>x.Role=="Teacher");
        var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","SchoolPortfolioPR-Sar-2026","secrets.json")).Build();
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(config.GetConnectionString("DefaultConnection")).Options,new HttpContextAccessor());
        var user=await db.Users.AsNoTracking().SingleAsync(x=>x.Email==account.Email);var marker="PICK-"+Guid.NewGuid().ToString("N")[..10];
        var groups=new[]{new SubjectGroup{Name=marker+" วิทยาศาสตร์"},new SubjectGroup{Name=marker+" ภาษาไทย"}};
        var teachers=new[]{new Teacher{Code=marker+"-T1",Name="ครูทดสอบ "+marker,DepartmentId=user.DepartmentId,SubjectGroup=groups[0]},new Teacher{Code=marker+"-T2",Name="ครูทดสอบ "+marker,DepartmentId=user.DepartmentId,SubjectGroup=groups[1]}};
        var students=new[]{new Student{Code=marker+"-S1",Name="นักเรียนทดสอบ "+marker+" หนึ่ง",Classroom="ม.4/1",DepartmentId=user.DepartmentId},new Student{Code=marker+"-S2",Name="นักเรียนทดสอบ "+marker+" สอง",Classroom="ม.4/2",DepartmentId=user.DepartmentId},new Student{Code=marker+"-S3",Name="นักเรียนทดสอบ "+marker+" <b>สาม</b>",Classroom="ม.5/1",DepartmentId=user.DepartmentId}};
        db.AddRange(teachers);db.AddRange(students);await db.SaveChangesAsync();
        using var playwright=await Playwright.CreateAsync();await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});await using var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1440,Height=1050}});var page=await context.NewPageAsync();var errors=new List<string>();page.PageError+=(_,e)=>errors.Add(e);Directory.CreateDirectory("artifacts/ui/participants");
        ILocator Picker(string kind)=>page.Locator($"[data-participant-picker][data-kind='{kind}']");
        ILocator Row(string kind,int id)=>Picker(kind).Locator($"[data-participant-results] input[value='{id}']");
        async Task Selected(string field,params int[] ids){var actual=await page.Locator("#"+field).EvaluateAsync<int[]>("el=>Array.from(el.selectedOptions,o=>Number(o.value))");if(!actual.Order().SequenceEqual(ids.Order()))throw new Exception("Selected values lost for "+field);}
        async Task Ready(){await Assertions.Expect(Picker("teacher")).ToHaveAttributeAsync("data-ready","true");await Assertions.Expect(Picker("student")).ToHaveAttributeAsync("data-ready","true");}
        try{
            await page.GotoAsync(root+"/Account/Login");await page.Locator("#Email").FillAsync(account.Email);await page.Locator("#Password").FillAsync(account.Password);await page.Locator("form button").ClickAsync();await page.WaitForURLAsync(root+"/Dashboard");
            await page.GotoAsync(root+"/Activities/Edit");await Ready();
            await Picker("teacher").Locator("[data-filter-group]").SelectOptionAsync(groups[0].Id.ToString());await Picker("teacher").Locator("[data-participant-search]").FillAsync("ครูทดสอบ "+marker);await Row("teacher",teachers[0].Id).CheckAsync();
            await Assertions.Expect(Row("teacher",teachers[1].Id)).ToHaveCountAsync(0);
            await Picker("teacher").Locator("[data-filter-group]").SelectOptionAsync(groups[1].Id.ToString());await Row("teacher",teachers[1].Id).CheckAsync();await Selected("TeacherIds",teachers.Select(x=>x.Id).ToArray());
            await Picker("teacher").Locator("[data-participant-search]").FillAsync("no-match-"+marker);await Assertions.Expect(Picker("teacher").Locator("[data-participant-status]")).ToContainTextAsync("พบ 0 คน");await Selected("TeacherIds",teachers.Select(x=>x.Id).ToArray());
            await Picker("teacher").Locator("[data-reset-filters]").ClickAsync();await Selected("TeacherIds",teachers.Select(x=>x.Id).ToArray());
            await Picker("student").Locator("[data-participant-search]").FillAsync("นักเรียนทดสอบ "+marker);await Picker("student").Locator("[data-filter-level]").SelectOptionAsync("ม.4");await Picker("student").Locator("[data-filter-room]").SelectOptionAsync("1");
            await Row("student",students[0].Id).CheckAsync();await Assertions.Expect(Row("student",students[1].Id)).ToHaveCountAsync(0);
            await Picker("student").Locator("[data-filter-level]").SelectOptionAsync("ม.5");await Assertions.Expect(Picker("student").Locator("[data-filter-room]")).ToHaveValueAsync("");await Row("student",students[2].Id).CheckAsync();await Selected("StudentIds",students[0].Id,students[2].Id);
            await Assertions.Expect(Picker("student").Locator("[data-participant-results] b")).ToHaveCountAsync(0);await Assertions.Expect(Picker("student").Locator("[data-participant-results]")).ToContainTextAsync("<b>สาม</b>");
            await Picker("student").Locator("[data-clear-picked]").ClickAsync();await Selected("StudentIds");await Picker("student").Locator("[data-filter-level]").SelectOptionAsync("ม.4");await Picker("student").Locator("[data-pick-visible]").ClickAsync();await Selected("StudentIds",students[0].Id,students[1].Id);
            await Picker("student").Locator(".participant-chip").Filter(new(){HasText=students[1].Code}).Locator("button").ClickAsync();await Selected("StudentIds",students[0].Id);await Picker("student").Locator("[data-filter-level]").SelectOptionAsync("ม.5");await Row("student",students[2].Id).CheckAsync();
            await Picker("student").Locator("[data-participant-search]").PressAsync("Enter");if(!page.Url.EndsWith("/Activities/Edit"))throw new Exception("Search Enter submitted form.");
            await page.Locator("#ActivityCode").FillAsync(marker);await page.Locator("#Description").FillAsync("ทดสอบชุดเลือกรายชื่อ");await page.Locator("#CategoryId").SelectOptionAsync(new SelectOptionValue{Index=1});await page.Locator("#AcademicYearId").SelectOptionAsync(new SelectOptionValue{Index=1});await page.Locator("#TeacherRole").SelectOptionAsync("0");
            await page.EvaluateAsync("document.querySelector('main form').noValidate=true");await page.Locator(".form-actions button").ClickAsync();await Ready();await Assertions.Expect(page.Locator(".validation-summary-errors")).ToBeVisibleAsync();await Selected("TeacherIds",teachers.Select(x=>x.Id).ToArray());await Selected("StudentIds",students[0].Id,students[2].Id);
            await page.Locator("#Title").FillAsync(marker+" activity");await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/activity/*");var activityId=int.Parse(new Uri(page.Url).Segments.Last());
            var activity=await db.Activities.AsNoTracking().Include(x=>x.Teachers).Include(x=>x.Students).AsSplitQuery().SingleAsync(x=>x.Id==activityId);if(activity.Teachers.Count!=2||!activity.Students.Select(x=>x.StudentId).Order().SequenceEqual(new[]{students[0].Id,students[2].Id}.Order()))throw new Exception("Activity links not persisted.");
            await page.GotoAsync(root+"/Activities/Edit/"+activityId);await Ready();await Selected("TeacherIds",teachers.Select(x=>x.Id).ToArray());await Selected("StudentIds",students[0].Id,students[2].Id);
            await page.Locator("fieldset").Filter(new(){Has=page.Locator("[data-kind=teacher]")}).ScreenshotAsync(new(){Path="artifacts/ui/participants/activity-picker-desktop.png"});
            await page.SetViewportSizeAsync(390,844);await Picker("student").ScrollIntoViewIfNeededAsync();if(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth>innerWidth+1"))throw new Exception("Mobile form overflow.");await Picker("student").ScreenshotAsync(new(){Path="artifacts/ui/participants/student-picker-mobile.png"});await page.SetViewportSizeAsync(1440,1050);
            await using(var plain=await browser.NewContextAsync(new(){JavaScriptEnabled=false,StorageState=await context.StorageStateAsync()})){var fallback=await plain.NewPageAsync();await fallback.GotoAsync(root+"/Activities/Edit/"+activityId);await Assertions.Expect(fallback.Locator("#TeacherIds")).ToBeVisibleAsync();await Assertions.Expect(fallback.Locator("[data-participant-controls]").First).ToBeHiddenAsync();}
            Console.WriteLine("PASS activity: subject-group/name search, dependent grade/room, persistent multi-selection, bulk/clear/remove, escaped names, validation redisplay, SQL links, edit restore, mobile and no-JS fallback.");

            await page.GotoAsync(root+"/Awards/Edit");await Ready();await Picker("teacher").Locator("[data-participant-search]").FillAsync(teachers[0].Code);await Row("teacher",teachers[0].Id).CheckAsync();await Picker("student").Locator("[data-participant-search]").FillAsync(students[1].Code);await Row("student",students[1].Id).CheckAsync();
            await page.Locator("#AwardName").FillAsync(marker+" award");await page.Locator("#CompetitionName").FillAsync("ทดสอบรางวัล");await page.Locator("#AwardRank").FillAsync("ทดสอบ");await page.Locator("#Organizer").FillAsync("ผู้จัดทดสอบ");await page.Locator("#Description").FillAsync("รายละเอียดทดสอบ");await page.Locator("#AwardLevel").SelectOptionAsync("0");await page.Locator("#TeacherRole").SelectOptionAsync("0");await page.Locator("#AcademicYearId").SelectOptionAsync(new SelectOptionValue{Index=1});await page.Locator("#ActivityId").SelectOptionAsync(activityId.ToString());await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/Awards/Details/*");var awardId=int.Parse(new Uri(page.Url).Segments.Last());
            await page.GotoAsync(root+"/Awards/Edit/"+awardId);await Ready();await Selected("TeacherIds",teachers[0].Id);await Selected("StudentIds",students[1].Id);await Picker("teacher").Locator("[data-participant-chips] button").ClickAsync();await Picker("teacher").Locator("[data-participant-search]").FillAsync(teachers[1].Code);await Row("teacher",teachers[1].Id).CheckAsync();await page.Locator(".form-actions button").ClickAsync();await page.WaitForURLAsync("**/Awards/Details/*");
            var award=await db.Awards.AsNoTracking().Include(x=>x.Teachers).Include(x=>x.Students).AsSplitQuery().SingleAsync(x=>x.Id==awardId);if(award.Teachers.Single().TeacherId!=teachers[1].Id||award.Students.Single().StudentId!=students[1].Id)throw new Exception("Award edit did not update selected IDs.");
            Console.WriteLine("PASS award: same picker, code search, create, edit, selection removal/replacement and SQL relationships.");
            if(errors.Count>0)throw new Exception(string.Join("\n",errors));
        }finally{
            var activities=await db.Activities.IgnoreQueryFilters().Where(x=>x.ActivityCode==marker).Select(x=>x.Id).ToListAsync();var awards=await db.Awards.IgnoreQueryFilters().Where(x=>x.AwardName==marker+" award").Select(x=>x.Id).ToListAsync();
            await db.Set<AwardTeacher>().IgnoreQueryFilters().Where(x=>awards.Contains(x.AwardId)).ExecuteDeleteAsync();await db.Set<AwardStudent>().IgnoreQueryFilters().Where(x=>awards.Contains(x.AwardId)).ExecuteDeleteAsync();await db.Awards.IgnoreQueryFilters().Where(x=>awards.Contains(x.Id)).ExecuteDeleteAsync();
            await db.Set<ActivityTeacher>().IgnoreQueryFilters().Where(x=>activities.Contains(x.ActivityId)).ExecuteDeleteAsync();await db.Set<ActivityStudent>().IgnoreQueryFilters().Where(x=>activities.Contains(x.ActivityId)).ExecuteDeleteAsync();await db.Activities.IgnoreQueryFilters().Where(x=>activities.Contains(x.Id)).ExecuteDeleteAsync();
            var teacherIds=teachers.Select(x=>x.Id).ToArray();var studentIds=students.Select(x=>x.Id).ToArray();var groupIds=groups.Select(x=>x.Id).ToArray();await db.Set<Teacher>().IgnoreQueryFilters().Where(x=>teacherIds.Contains(x.Id)).ExecuteDeleteAsync();await db.Set<Student>().IgnoreQueryFilters().Where(x=>studentIds.Contains(x.Id)).ExecuteDeleteAsync();await db.Set<SubjectGroup>().IgnoreQueryFilters().Where(x=>groupIds.Contains(x.Id)).ExecuteDeleteAsync();
        }
        Console.WriteLine("PASS shared participant pickers; no JavaScript errors; fixtures cleaned.");
    }
}
