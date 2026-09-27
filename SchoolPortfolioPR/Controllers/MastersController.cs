using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
[Authorize(Policy = "Admin")]
public class MastersController(ApplicationDbContext db, AccessService access) : EditorController(db, access)
{
    public static readonly Dictionary<string, string> Names = new() { ["Department"] = "ฝ่าย / แผนก", ["SubjectGroup"] = "กลุ่มสาระ", ["Teacher"] = "ครู", ["Student"] = "นักเรียน", ["AcademicYear"] = "ปีการศึกษา", ["ActivityCategory"] = "ประเภทกิจกรรม", ["Tag"] = "แท็ก", ["SchoolProfile"] = "ข้อมูลโรงเรียน" };
    private IQueryable<NamedRecord> Query(string kind) => kind switch {
        "Department" => Db.Set<Department>(), "SubjectGroup" => Db.Set<SubjectGroup>(), "Teacher" => Db.Set<Teacher>(), "Student" => Db.Set<Student>(),
        "AcademicYear" => Db.Set<AcademicYear>(), "ActivityCategory" => Db.Set<ActivityCategory>(), "Tag" => Db.Set<Tag>(), "SchoolProfile" => Db.Set<SchoolProfile>(), _ => throw new ArgumentException("Invalid kind") };
    public async Task<IActionResult> Index(string kind = "Department", string? q = null, int page = 1)
    {
        if (!Names.ContainsKey(kind)) return NotFound(); ViewBag.Kind = kind; ViewBag.Title = Names[kind]; ViewBag.Search = q;
        var query = Query(kind).AsNoTracking(); if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Name.Contains(q));
        page = Math.Max(1, page); return View(new PageView<NamedRecord>(await query.OrderBy(x => x.Name).Skip((page - 1) * 20).Take(20).ToListAsync(), page, await query.CountAsync()));
    }
    public async Task<IActionResult> Edit(string kind, int? id)
    {
        if (!Names.ContainsKey(kind)) return NotFound(); ViewBag.Kind = kind; ViewBag.Title = Names[kind]; await LookupsAsync();
        if (id == null) return View(new MasterForm());
        var r = await Query(kind).SingleOrDefaultAsync(x => x.Id == id); if (r == null) return NotFound();
        var f = new MasterForm { Id = r.Id, Name = r.Name };
        if (r is Teacher t) { f.Code = t.Code; f.DepartmentId = t.DepartmentId; f.SubjectGroupId = t.SubjectGroupId; }
        if (r is Student s) { f.Code = s.Code; f.DepartmentId = s.DepartmentId; f.Classroom = s.Classroom; }
        if (r is AcademicYear y) { f.StartDate = y.StartDate; f.EndDate = y.EndDate; }
        if (r is SchoolProfile p) { f.Address = p.Address; f.DirectorName = p.DirectorName; f.Contact = p.Contact; }
        return View(f);
    }
    [HttpPost] public async Task<IActionResult> Edit(string kind, MasterForm f)
    {
        if (!Names.ContainsKey(kind)) return NotFound(); ViewBag.Kind = kind; ViewBag.Title = Names[kind];
        if (kind is "Teacher" or "Student" && string.IsNullOrWhiteSpace(f.Code)) ModelState.AddModelError("Code", "กรุณาระบุรหัส");
        if (f.DepartmentId != null && !await Db.Set<Department>().AnyAsync(x => x.Id == f.DepartmentId)) ModelState.AddModelError("DepartmentId", "ไม่พบฝ่าย");
        if (f.SubjectGroupId != null && !await Db.Set<SubjectGroup>().AnyAsync(x => x.Id == f.SubjectGroupId)) ModelState.AddModelError("SubjectGroupId", "ไม่พบกลุ่มสาระ");
        if (kind == "Teacher" && await Db.Set<Teacher>().AnyAsync(x => x.Id != f.Id && x.Code == f.Code) || kind == "Student" && await Db.Set<Student>().AnyAsync(x => x.Id != f.Id && x.Code == f.Code)) ModelState.AddModelError("Code", "รหัสซ้ำ");
        if (!ModelState.IsValid) { await LookupsAsync(); return View(f); }
        var r = f.Id == 0 ? (NamedRecord)Activator.CreateInstance(typeof(Department).Assembly.GetType("SchoolPortfolioPR.Models." + kind)!)! : await Query(kind).SingleOrDefaultAsync(x => x.Id == f.Id);
        if (r == null) return NotFound(); r.Name = f.Name;
        if (r is Teacher t) { t.Code = f.Code!; t.DepartmentId = f.DepartmentId; t.SubjectGroupId = f.SubjectGroupId; }
        if (r is Student s) { s.Code = f.Code!; s.DepartmentId = f.DepartmentId; s.Classroom = f.Classroom; }
        if (r is AcademicYear y) { y.StartDate = f.StartDate; y.EndDate = f.EndDate; }
        if (r is SchoolProfile p) { p.Address = f.Address; p.DirectorName = f.DirectorName; p.Contact = f.Contact; }
        if (f.Id == 0) Db.Add(r); await Db.SaveChangesAsync(); TempData["Success"] = "บันทึกข้อมูลแล้ว"; return RedirectToAction(nameof(Index), new { kind });
    }
    [HttpPost] public async Task<IActionResult> Delete(string kind, int id)
    {
        if (!Names.ContainsKey(kind)) return NotFound(); var r = await Query(kind).SingleOrDefaultAsync(x => x.Id == id); if (r == null) return NotFound();
        bool inUse = kind switch {
            "Department" => await Db.Activities.AnyAsync(x => x.DepartmentId == id) || await Db.Awards.AnyAsync(x => x.DepartmentId == id) || await Db.Users.AnyAsync(x => x.DepartmentId == id) || await Db.Set<Teacher>().AnyAsync(x => x.DepartmentId == id) || await Db.Set<Student>().AnyAsync(x => x.DepartmentId == id),
            "SubjectGroup" => await Db.Activities.AnyAsync(x => x.SubjectGroupId == id) || await Db.Awards.AnyAsync(x => x.SubjectGroupId == id) || await Db.Set<Teacher>().AnyAsync(x => x.SubjectGroupId == id),
            "AcademicYear" => await Db.Activities.AnyAsync(x => x.AcademicYearId == id) || await Db.Awards.AnyAsync(x => x.AcademicYearId == id),
            "ActivityCategory" => await Db.Activities.AnyAsync(x => x.CategoryId == id),
            "Teacher" => await Db.Set<ActivityTeacher>().AnyAsync(x => x.TeacherId == id) || await Db.Set<AwardTeacher>().AnyAsync(x => x.TeacherId == id),
            "Student" => await Db.Users.AnyAsync(x => x.StudentId == id) || await Db.Set<ActivityStudent>().AnyAsync(x => x.StudentId == id) || await Db.Set<AwardStudent>().AnyAsync(x => x.StudentId == id),
            "Tag" => await Db.Set<ActivityTag>().AnyAsync(x => x.TagId == id), "SchoolProfile" => true, _ => false };
        if (inUse) { TempData["Error"] = "ข้อมูลนี้มีการใช้งานอยู่ จึงไม่สามารถลบได้"; }
        else { Db.Remove(r); await Db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index), new { kind });
    }
}
