using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
namespace SchoolPortfolioPR.Services;
public static class Roles
{
    public static readonly string[] All = ["SuperAdmin", "Administrator", "PRAdmin", "Approver", "DepartmentAdmin", "Teacher", "Viewer", "Student"];
    public static readonly string[] Admins = ["SuperAdmin", "Administrator"];
    public static readonly string[] Editors = ["SuperAdmin", "Administrator", "PRAdmin", "Approver", "DepartmentAdmin", "Teacher"];
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string> {
        ["SuperAdmin"] = "ผู้ดูแลระบบสูงสุด", ["Administrator"] = "ผู้ดูแลโรงเรียน", ["PRAdmin"] = "ประชาสัมพันธ์",
        ["Approver"] = "ผู้อนุมัติ", ["DepartmentAdmin"] = "ผู้ดูแลฝ่าย", ["Teacher"] = "ครู", ["Viewer"] = "ผู้ชม", ["Student"] = "นักเรียน" };
}
public class AccessService(ApplicationDbContext db, IHttpContextAccessor context)
{
    public ClaimsPrincipal User => context.HttpContext!.User;
    public string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    public bool IsAdmin => Roles.Admins.Any(User.IsInRole);
    public bool IsPublisher => IsAdmin || User.IsInRole("PRAdmin");
    public bool IsEditor => User.Identity?.IsAuthenticated == true && Roles.Editors.Any(User.IsInRole);
    public bool IsStudent => User.IsInRole("Student") && !IsEditor;
    public async Task<int?> StudentAsync() => await db.Users.Where(u => u.Id == UserId).Select(u => u.StudentId).SingleOrDefaultAsync();
    public async Task<bool> CanAuthorAsync() => IsEditor && (IsPublisher || await DepartmentAsync() != null);
    public async Task<int?> DepartmentAsync() => await db.Users.Where(u => u.Id == UserId).Select(u => u.DepartmentId).SingleOrDefaultAsync();
    public async Task<bool> CanReadAsync(Record item, int departmentId, WorkflowStatus status, bool isPublic)
    {
        if (IsStudent && item is Award) return await (await AwardsAsync()).AnyAsync(x => x.Id == item.Id);
        return status == WorkflowStatus.Published && isPublic || User.Identity?.IsAuthenticated == true && (IsAdmin || IsPublisher || User.IsInRole("Approver") || item.CreatedBy == UserId || User.IsInRole("DepartmentAdmin") && await DepartmentAsync() == departmentId);
    }
    public async Task<bool> CanEditAsync(Record item, int departmentId, WorkflowStatus status)
        => IsEditor && (IsAdmin || (status is WorkflowStatus.Draft or WorkflowStatus.Rejected) && (item.CreatedBy == UserId || User.IsInRole("DepartmentAdmin") && await DepartmentAsync() == departmentId));
    public async Task<bool> CanCreateAsync(int departmentId) => IsEditor && (IsPublisher || await DepartmentAsync() == departmentId);
    public async Task<bool> CanTransitionAsync(Record item, int departmentId, WorkflowStatus from, WorkflowStatus to)
    {
        if (!IsEditor) return false;
        bool reviewer = IsAdmin || User.IsInRole("DepartmentAdmin") && await DepartmentAsync() == departmentId;
        bool approver = IsAdmin || User.IsInRole("Approver");
        return (from, to) switch {
            (WorkflowStatus.Draft or WorkflowStatus.Rejected, WorkflowStatus.Submitted) => await CanEditAsync(item, departmentId, from),
            (WorkflowStatus.Submitted, WorkflowStatus.Reviewed) => reviewer,
            (WorkflowStatus.Reviewed, WorkflowStatus.Approved) => approver,
            (WorkflowStatus.Approved, WorkflowStatus.Published) => IsPublisher,
            (WorkflowStatus.Submitted, WorkflowStatus.Rejected) => reviewer,
            (WorkflowStatus.Reviewed, WorkflowStatus.Rejected) => approver,
            (WorkflowStatus.Published, WorkflowStatus.Archived) => IsPublisher,
            (WorkflowStatus.Archived, WorkflowStatus.Draft) => IsAdmin,
            _ => false
        };
    }
    public async Task<IQueryable<Activity>> ActivitiesAsync()
    {
        var q = db.Activities.AsNoTracking();
        if (IsAdmin || IsPublisher || User.IsInRole("Approver")) return q;
        var dept = User.IsInRole("DepartmentAdmin") ? await DepartmentAsync() : null;
        return q.Where(x => x.Status == WorkflowStatus.Published && x.IsPublic || x.CreatedBy == UserId || dept != null && x.DepartmentId == dept);
    }
    public async Task<IQueryable<Award>> AwardsAsync()
    {
        var q = db.Awards.AsNoTracking();
        if (IsStudent) {
            var student = await StudentAsync();
            return q.Where(x => student != null && x.Status == WorkflowStatus.Published && x.Students.Any(s => s.StudentId == student));
        }
        if (User.Identity?.IsAuthenticated != true) return q.Where(x => x.Status == WorkflowStatus.Published && x.IsPublic);
        if (IsAdmin || IsPublisher || User.IsInRole("Approver")) return q;
        var dept = User.IsInRole("DepartmentAdmin") ? await DepartmentAsync() : null;
        return q.Where(x => x.Status == WorkflowStatus.Published && x.IsPublic || x.CreatedBy == UserId || dept != null && x.DepartmentId == dept);
    }
    public async Task<IQueryable<AwardCertificate>> CertificatesAsync()
    {
        var awards = await AwardsAsync();
        var certificates = db.Set<AwardCertificate>().AsNoTracking().Where(x => awards.Select(a => a.Id).Contains(x.AwardId));
        if (IsStudent) { var student = await StudentAsync(); certificates = certificates.Where(x => student != null && x.StudentId == student); }
        return certificates;
    }
    public IQueryable<MonthlyPublication> Publications()
    {
        var q = db.Publications.AsNoTracking().Where(x => x.IsPublic || IsPublisher);
        // Compiled school PDFs cannot redact award recipients separately for each student.
        if (IsStudent) q = q.Where(x => !x.Items.Any(i => i.AwardId != null));
        return q;
    }
    public async Task<IQueryable<Activity>> PendingActivitiesAsync()
    {
        var q = await ActivitiesAsync();
        var department = User.IsInRole("DepartmentAdmin") ? await DepartmentAsync() : null;
        return q.Where(x => IsEditor && (
            (x.Status == WorkflowStatus.Draft || x.Status == WorkflowStatus.Rejected) && (IsAdmin || x.CreatedBy == UserId || department != null && x.DepartmentId == department)
            || x.Status == WorkflowStatus.Submitted && (IsAdmin || department != null && x.DepartmentId == department)
            || x.Status == WorkflowStatus.Reviewed && (IsAdmin || User.IsInRole("Approver"))
            || x.Status == WorkflowStatus.Approved && IsPublisher));
    }
    public async Task<IQueryable<Award>> PendingAwardsAsync()
    {
        var q = await AwardsAsync();
        var department = User.IsInRole("DepartmentAdmin") ? await DepartmentAsync() : null;
        return q.Where(x => IsEditor && (
            (x.Status == WorkflowStatus.Draft || x.Status == WorkflowStatus.Rejected) && (IsAdmin || x.CreatedBy == UserId || department != null && x.DepartmentId == department)
            || x.Status == WorkflowStatus.Submitted && (IsAdmin || department != null && x.DepartmentId == department)
            || x.Status == WorkflowStatus.Reviewed && (IsAdmin || User.IsInRole("Approver"))
            || x.Status == WorkflowStatus.Approved && IsPublisher));
    }
}
