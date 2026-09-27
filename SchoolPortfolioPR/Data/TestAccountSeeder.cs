using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
namespace SchoolPortfolioPR.Data;

public record TestCredential(string Role,string Email,string Password);
public static class TestAccountSeeder
{
    public static async Task SeedAsync(IServiceProvider services,IWebHostEnvironment environment)
    {
        var db=services.GetRequiredService<ApplicationDbContext>();
        var users=services.GetRequiredService<UserManager<ApplicationUser>>();
        await RoleSeeder.SeedAsync(services.GetRequiredService<RoleManager<IdentityRole>>());
        var http=services.GetRequiredService<IHttpContextAccessor>();http.HttpContext??=new DefaultHttpContext();
        var storage=services.GetRequiredService<IFileStorage>();var export=services.GetRequiredService<ExportService>();
        var directory=Path.GetFullPath(Path.Combine(environment.ContentRootPath,"..","artifacts","private"));Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"test-accounts.json");
        var credentials=File.Exists(path)?JsonSerializer.Deserialize<List<TestCredential>>(await File.ReadAllTextAsync(path))??[]:new List<TestCredential>();
        var department=await db.Set<Department>().OrderBy(x=>x.Id).FirstAsync();
        foreach(var role in Roles.All.Concat(new[]{"Student2"}))
        {
            var actualRole=role=="Student2"?"Student":role;
            var email=$"demo.{role.ToLowerInvariant()}@school.local";
            var u=await users.FindByEmailAsync(email);
            if(u==null){
                var password="Aa1!"+Convert.ToHexString(RandomNumberGenerator.GetBytes(8))+"z!";
                u=new ApplicationUser{Email=email,UserName=email,EmailConfirmed=true,DisplayName=$"บัญชีทดสอบ {role}",DepartmentId=department.Id};
                if(actualRole=="Student"){
                    var code=role=="Student2"?"DEMO-LOGIN-S02":"DEMO-LOGIN-S01";
                    var student=await db.Set<Student>().SingleOrDefaultAsync(x=>x.Code==code);
                    if(student==null){student=new Student{Code=code,Name=role=="Student2"?"นักเรียนทดสอบ คนที่สอง":"นักเรียนทดสอบ สร้างสรรค์",Classroom="ม.6/1",DepartmentId=department.Id};db.Add(student);await db.SaveChangesAsync();}
                    u.StudentId=student.Id;
                }
                var result=await users.CreateAsync(u,password);if(!result.Succeeded)throw new InvalidOperationException(string.Join(";",result.Errors.Select(x=>x.Description)));
                result=await users.AddToRoleAsync(u,actualRole);if(!result.Succeeded)throw new InvalidOperationException("Cannot assign test role.");
                credentials.Add(new(actualRole,email,password));
                // Persist each newly created credential immediately. Existing accounts are never reset on a rerun.
                await File.WriteAllTextAsync(path,JsonSerializer.Serialize(credentials,new JsonSerializerOptions{WriteIndented=true}),Encoding.UTF8);
            }
            if(!(await users.GetRolesAsync(u)).SequenceEqual(new[]{actualRole}))throw new InvalidOperationException($"Existing {email} has unexpected roles; not changing it automatically.");
            http.HttpContext.User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,u.Id),new Claim(ClaimTypes.Role,actualRole)],"seed"));
            var title=$"[ตัวอย่าง] เอกสารส่วนตัว {role}";
            var document=await db.PersonalDocuments.SingleOrDefaultAsync(x=>x.OwnerId==u.Id&&x.Title==title);
            if(document==null){
                var content=ExportService.Document(title,$"<h1>{ExportService.Escape(title)}</h1><p>เอกสารสมมติสำหรับทดสอบคลัง PDF ส่วนตัว ไม่ใช้เป็นเอกสารรับรองจริง</p><p>เจ้าของบัญชี: {ExportService.Escape(u.DisplayName)}</p><h2>ผลการเรียน / รายงานตัวอย่าง</h2><p>GPAX ตัวอย่าง 3.65 — ข้อมูลนี้สมมติทั้งหมด</p>");
                var bytes=await export.PdfAsync(content);
                document=new PersonalDocument{OwnerId=u.Id,Title=title,Category=actualRole=="Student"?"ผลการเรียน":"รายงาน",Description="ข้อมูลสมมติสำหรับทดสอบระบบ",DocumentDate=DateTime.UtcNow.AddHours(7).Date,Pages=PersonalPdfService.PageCount(bytes),File=await storage.StoreGeneratedAsync(bytes,$"demo-private-{role.ToLowerInvariant()}.pdf",u.Id)};
                db.Add(document);await db.SaveChangesAsync();
            }
            if(role=="Student"&&!await db.AdmissionPortfolios.AnyAsync(x=>x.OwnerId==u.Id)){
                var proofBytes=await export.PdfAsync(ExportService.Document("หลักฐานกิจกรรมตัวอย่าง","<h1>หลักฐานกิจกรรม — ข้อมูลสมมติ</h1><p>ใช้ทดสอบการแนบหลักฐานท้ายแฟ้มเท่านั้น ไม่มีผลรับรองจริง</p>"));
                var proof=new PersonalDocument{OwnerId=u.Id,Title="[ตัวอย่าง] หลักฐานการร่วมกิจกรรม",Category="เกียรติบัตร",DocumentDate=DateTime.UtcNow.AddHours(7).Date,Pages=PersonalPdfService.PageCount(proofBytes),File=await storage.StoreGeneratedAsync(proofBytes,"demo-private-evidence.pdf",u.Id)};db.Add(proof);await db.SaveChangesAsync();
                var p=new AdmissionPortfolio{OwnerId=u.Id,Title="[ตัวอย่าง] แฟ้มสะสมผลงานเพื่อการศึกษาต่อ",FullName="นักเรียนทดสอบ สร้างสรรค์",School="โรงเรียนตัวอย่าง",StudyPlan="วิทยาศาสตร์–คณิตศาสตร์ (ตัวอย่าง)",GraduationYear="2570",Gpax=3.65m,ContactEmail=email,
                    TargetUniversity="มหาวิทยาลัยเป้าหมาย (แก้ไขก่อนยื่น)",TargetFaculty="คณะที่สนใจ (แก้ไขก่อนยื่น)",Bio="ข้อมูลสมมติสำหรับสาธิตการจัดแฟ้ม นักเรียนสนใจการทดลอง เทคโนโลยี และการแก้ปัญหาที่เกิดขึ้นรอบตัว",
                    Education="2567–2570 | มัธยมศึกษาตอนปลาย โรงเรียนตัวอย่าง\n2564–2566 | มัธยมศึกษาตอนต้น โรงเรียนตัวอย่าง",Motivation="ตัวอย่างข้อความ: ฉันสนใจการเรียนรู้ผ่านการลงมือทำ โดยเฉพาะการใช้เทคโนโลยีแก้ปัญหาในชีวิตประจำวัน ประสบการณ์จากโครงงานทำให้ฉันเรียนรู้การตั้งคำถาม การทดลอง และการปรับปรุงงานจากข้อเสนอแนะ\n\nกรุณาเขียนแรงบันดาลใจและประสบการณ์จริงของตนเองก่อนยื่น",Goals="ตัวอย่าง: พัฒนาทักษะการวิจัยและออกแบบ เพื่อสร้างผลงานที่เป็นประโยชน์ต่อชุมชน",Skills="การทำงานเป็นทีม | 85\nการนำเสนอ | 75\nการวิเคราะห์ข้อมูล | 70\nภาษาอังกฤษ | 65",MaxPages=10,MaxMegabytes=10};
                p.Documents.Add(new PortfolioDocument{PersonalDocumentId=document.Id,SortOrder=0});p.Documents.Add(new PortfolioDocument{PersonalDocumentId=proof.Id,SortOrder=1});
                string[] codes=["DEMO-ACT-001","DEMO-ACT-007","DEMO-ACT-010"];
                string[] titles=["[ตัวอย่าง] โครงงานทดลองวิทยาศาสตร์","[ตัวอย่าง] สื่อดิจิทัลเพื่อการเรียนรู้","[ตัวอย่าง] จิตอาสาดูแลสิ่งแวดล้อม"];
                for(var i=0;i<3;i++){
                    var e=new PortfolioEntry{Title=titles[i],Date=DateTime.UtcNow.AddDays(-10-i).Date,Section=i==2?PortfolioSection.Volunteer:i==1?PortfolioSection.Activity:PortfolioSection.Project,Role="สมาชิกทีมและผู้นำเสนอ (ตัวอย่าง)",Description="ข้อมูลสมมติ: ร่วมตั้งโจทย์ วางแผน ทดลอง และนำเสนอผลงาน พร้อมบันทึกผลและปรับปรุงตามข้อเสนอแนะ",Outcome="ตัวอย่างผลลัพธ์: ได้ชิ้นงานต้นแบบและรายงานสรุปกระบวนการทำงาน",Reflection="เรียนรู้การแบ่งหน้าที่ การรับฟังความคิดเห็น และใช้หลักฐานเพื่ออธิบายผลที่เกิดขึ้น",SortOrder=i,PersonalDocumentId=proof.Id};
                    var source=await db.Set<ActivityImage>().Where(x=>x.Activity.ActivityCode==codes[i]).OrderBy(x=>x.SortOrder).Select(x=>x.File).FirstOrDefaultAsync();
                    if(source!=null){using var stream=new MemoryStream(source.FileData);e.Image=await storage.StoreAsync(new FormFile(stream,0,stream.Length,"image",source.OriginalFileName){Headers=new HeaderDictionary(),ContentType=source.ContentType},u.Id);}
                    p.Entries.Add(e);
                }
                db.Add(p);await db.SaveChangesAsync();
                await services.GetRequiredService<AdmissionPortfolioService>().GenerateAsync(p,u.Id);await db.SaveChangesAsync();
            }
        }
        var primaryStudent=await users.FindByEmailAsync("demo.student@school.local");
        if(primaryStudent!=null){
            var unfinished=await db.AdmissionPortfolios.Include(x=>x.Entries).Include(x=>x.Documents).AsSplitQuery().FirstOrDefaultAsync(x=>x.OwnerId==primaryStudent.Id&&x.PdfFileId==null);
            if(unfinished!=null){http.HttpContext.User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,primaryStudent.Id),new Claim(ClaimTypes.Role,"Student")],"seed"));await services.GetRequiredService<AdmissionPortfolioService>().GenerateAsync(unfinished,primaryStudent.Id);await db.SaveChangesAsync();}
        }
        var markdown=new StringBuilder("# บัญชีทดสอบระบบ\n\nเข้าสู่ระบบที่ http://localhost:5093/Account/Login\n\n| บทบาท | อีเมล | รหัสผ่าน |\n|---|---|---|\n");
        foreach(var c in credentials)markdown.AppendLine($"| {c.Role} | {c.Email} | `{c.Password}` |");
        markdown.AppendLine("\nStudent คนแรกมีแฟ้มตัวอย่างและ PDF พร้อมอ่าน คนที่สองใช้ตรวจการแยกข้อมูลส่วนตัว ทุกบัญชีมีเอกสาร PDF ตัวอย่างของตนเอง บัญชีเดิมไม่ได้เปลี่ยนรหัสผ่าน ไฟล์นี้เก็บเฉพาะใน artifacts/private ซึ่งไม่รวมใน Git หรือชุด publish");
        await File.WriteAllTextAsync(Path.Combine(directory,"TEST-ACCOUNTS.md"),markdown.ToString(),Encoding.UTF8);
        Console.WriteLine($"Test users ready: {credentials.Count} accounts. Credentials: {Path.Combine(directory,"TEST-ACCOUNTS.md")}");
    }
}
