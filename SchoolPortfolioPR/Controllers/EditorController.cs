using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
public abstract class EditorController(ApplicationDbContext db, AccessService access) : Controller
{
    protected ApplicationDbContext Db => db;
    protected AccessService Access => access;
    protected async Task LookupsAsync(bool forAuthoring = false)
    {
        var departments = db.Set<Department>().AsNoTracking();
        if (forAuthoring && !access.IsPublisher) { var own = await access.DepartmentAsync(); departments = departments.Where(x => x.Id == own); }
        ViewBag.Departments = new SelectList(await departments.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.SubjectGroups = new SelectList(await db.Set<SubjectGroup>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Categories = new SelectList(await db.Set<ActivityCategory>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.AcademicYears = new SelectList(await db.Set<AcademicYear>().AsNoTracking().OrderByDescending(x => x.StartDate).ToListAsync(), "Id", "Name");
        var teachers = db.Set<Teacher>().AsNoTracking(); var students = db.Set<Student>().AsNoTracking();
        if (!access.IsEditor) {
            var activities = await access.ActivitiesAsync(); var awards = await access.AwardsAsync();
            teachers = teachers.Where(t => activities.Any(a => a.Teachers.Any(p => p.TeacherId == t.Id)) || awards.Any(a => a.Teachers.Any(p => p.TeacherId == t.Id)));
            if (access.IsStudent) { var own = await access.StudentAsync(); students = students.Where(x => x.Id == own); teachers = teachers.Where(x => false); }
            else students = students.Where(t => activities.Any(a => a.Students.Any(p => p.StudentId == t.Id)) || awards.Any(a => a.Students.Any(p => p.StudentId == t.Id)));
        }
        var teacherChoices = await teachers.OrderBy(x => x.Name).Select(x=>new { x.Id, x.Name, x.Code, x.SubjectGroupId, GroupName = x.SubjectGroup == null ? null : x.SubjectGroup.Name }).ToListAsync();
        var studentChoices = await students.OrderBy(x => x.Name).Select(x=>new { x.Id, x.Name, x.Code, x.Classroom }).ToListAsync();
        ViewBag.Teachers = new SelectList(teacherChoices, "Id", "Name");
        ViewBag.Students = new SelectList(studentChoices, "Id", "Name");
        if (forAuthoring) {
            ViewBag.TeacherChoices = teacherChoices.Select(x=>new ParticipantChoice(x.Id,x.Name,x.Code,x.SubjectGroupId?.ToString()??"none",x.GroupName??"ไม่ระบุกลุ่มสาระ","","")).ToList();
            ViewBag.StudentChoices = studentChoices.Select(x=>{ var placement=StudentPlacement.Parse(x.Classroom); return new ParticipantChoice(x.Id,x.Name,x.Code,"","",placement.Level,placement.Room); }).ToList();
        }
        ViewBag.Tags = new SelectList(await db.Set<Tag>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Activities = new SelectList(await (await access.ActivitiesAsync()).OrderByDescending(x => x.ActivityDate).Select(x => new { x.Id, x.Title }).ToListAsync(), "Id", "Title");
    }
    protected async Task ValidateLinksAsync(int department, int year, int? subject, int[] teachers, int[] students)
    {
        if (!await db.Set<Department>().AnyAsync(x => x.Id == department)) ModelState.AddModelError("DepartmentId", "ไม่พบฝ่าย");
        if (!await db.Set<AcademicYear>().AnyAsync(x => x.Id == year)) ModelState.AddModelError("AcademicYearId", "ไม่พบปีการศึกษา");
        if (subject != null && !await db.Set<SubjectGroup>().AnyAsync(x => x.Id == subject)) ModelState.AddModelError("SubjectGroupId", "ไม่พบกลุ่มสาระ");
        if (await db.Set<Teacher>().CountAsync(x => teachers.Contains(x.Id)) != teachers.Distinct().Count()) ModelState.AddModelError("TeacherIds", "ไม่พบครูบางรายการ");
        if (await db.Set<Student>().CountAsync(x => students.Contains(x.Id)) != students.Distinct().Count()) ModelState.AddModelError("StudentIds", "ไม่พบนักเรียนบางรายการ");
    }
    protected bool Version(Record record, string? value)
    {
        if (record.Id == 0) return true;
        if (string.IsNullOrEmpty(value) || value != Convert.ToBase64String(record.RowVersion)) { ModelState.AddModelError("", "ข้อมูลถูกแก้ไขโดยผู้อื่น กรุณาเปิดหน้าใหม่"); return false; }
        return true;
    }
}
