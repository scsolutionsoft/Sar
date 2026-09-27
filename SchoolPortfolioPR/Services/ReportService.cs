using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Services;
public class ReportService(AccessService access)
{
    public static readonly Dictionary<string, string> Kinds = new() { ["MonthlyActivities"] = "กิจกรรมรายเดือน", ["AnnualActivities"] = "กิจกรรมรายปี", ["TeacherPortfolio"] = "แฟ้มผลงานครู", ["StudentPortfolio"] = "แฟ้มผลงานนักเรียน", ["AwardsByYear"] = "รางวัลตามปี", ["AwardsByLevel"] = "รางวัลตามระดับ", ["AwardsByDepartment"] = "รางวัลตามฝ่าย", ["AwardsBySubjectGroup"] = "รางวัลตามกลุ่มสาระ", ["ActivitiesByDepartment"] = "กิจกรรมตามฝ่าย", ["ActivitiesByCategory"] = "กิจกรรมตามประเภท", ["TeacherAchievements"] = "ความสำเร็จของครู", ["StudentAchievements"] = "ความสำเร็จของนักเรียน" };
    public async Task<List<ReportRow>> BuildAsync(ReportFilter f)
    {
        if (access.IsStudent) { f.StudentId = await access.StudentAsync() ?? -1; f.TeacherId = null; }
        var a = await access.ActivitiesAsync(); var w = await access.AwardsAsync();
        if (f.From.HasValue) { a = a.Where(x => x.ActivityDate >= f.From); w = w.Where(x => x.AwardDate >= f.From); }
        if (f.To.HasValue) { var end = f.To.Value.Date.AddDays(1); a = a.Where(x => x.ActivityDate < end); w = w.Where(x => x.AwardDate < end); }
        if (f.AcademicYearId.HasValue) { a = a.Where(x => x.AcademicYearId == f.AcademicYearId); w = w.Where(x => x.AcademicYearId == f.AcademicYearId); }
        if (f.FiscalYear.HasValue) { a = a.Where(x => x.FiscalYear == f.FiscalYear); var start = new DateTime(f.FiscalYear.Value - 544, 10, 1); var end = start.AddYears(1); w = w.Where(x => x.AwardDate >= start && x.AwardDate < end); }
        if (f.Month.HasValue) { a = a.Where(x => x.ActivityDate.Month == f.Month); w = w.Where(x => x.AwardDate.Month == f.Month); }
        if (f.DepartmentId.HasValue) { a = a.Where(x => x.DepartmentId == f.DepartmentId); w = w.Where(x => x.DepartmentId == f.DepartmentId); }
        if (f.SubjectGroupId.HasValue) { a = a.Where(x => x.SubjectGroupId == f.SubjectGroupId); w = w.Where(x => x.SubjectGroupId == f.SubjectGroupId); }
        if (f.TeacherId.HasValue) { a = a.Where(x => x.Teachers.Any(t => t.TeacherId == f.TeacherId)); w = w.Where(x => x.Teachers.Any(t => t.TeacherId == f.TeacherId)); }
        if (f.StudentId.HasValue) { a = a.Where(x => x.Students.Any(t => t.StudentId == f.StudentId)); w = w.Where(x => x.Students.Any(t => t.StudentId == f.StudentId)); }
        if (f.CategoryId.HasValue) { a = a.Where(x => x.CategoryId == f.CategoryId); w = w.Where(x => x.Activity != null && x.Activity.CategoryId == f.CategoryId); }
        if (f.AwardLevel.HasValue) { w = w.Where(x => x.AwardLevel == f.AwardLevel); a = a.Where(x => x.Awards.Any(t => t.AwardLevel == f.AwardLevel)); }
        if (f.Kind == "TeacherAchievements") w = w.Where(x => x.Teachers.Any());
        if (f.Kind == "StudentAchievements") w = w.Where(x => x.Students.Any());
        var rows = new List<ReportRow>();
        if (!f.Kind.StartsWith("Awards") && !f.Kind.EndsWith("Achievements"))
        {
            var activities = await a.Include(x => x.Department).Include(x => x.Category).Include(x => x.Teachers).ThenInclude(x => x.Teacher).Include(x => x.Students).ThenInclude(x => x.Student).AsSplitQuery().OrderByDescending(x => x.ActivityDate).ToListAsync();
            rows.AddRange(activities.Select(x => new ReportRow(x.IsProject ? "โครงการ" : "กิจกรรม", x.Title, x.ActivityDate, x.Department.Name, x.Category.Name, string.Join(", ", x.Teachers.Select(t => t.Teacher.Name).Concat(x.Students.Select(t => t.Student.Name))))));
        }
        if (f.Kind.StartsWith("Awards") || f.Kind.EndsWith("Achievements") || f.Kind.EndsWith("Portfolio"))
        {
            var awards = await w.Include(x => x.Department).Include(x => x.SubjectGroup).Include(x => x.AcademicYear).Include(x => x.Teachers).ThenInclude(x => x.Teacher).Include(x => x.Students).ThenInclude(x => x.Student).AsSplitQuery().OrderByDescending(x => x.AwardDate).ToListAsync();
            rows.AddRange(awards.Select(x => new ReportRow("รางวัล", x.AwardName + " · " + x.AwardRank, x.AwardDate, x.Department.Name,
                f.Kind == "AwardsBySubjectGroup" ? x.SubjectGroup?.Name ?? "ไม่ระบุ" : f.Kind == "AwardsByYear" ? x.AcademicYear.Name : Thai.Level(x.AwardLevel),
                access.IsStudent ? string.Join(", ", x.Students.Where(t => t.StudentId == f.StudentId).Select(t => t.Student.Name)) : string.Join(", ", x.Teachers.Select(t => t.Teacher.Name + " (" + t.Role + ")").Concat(x.Students.Select(t => t.Student.Name))))));
        }
        return rows.OrderByDescending(x => x.Date).ToList();
    }
}
