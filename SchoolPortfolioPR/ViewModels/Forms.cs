using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoolPortfolioPR.Models;
namespace SchoolPortfolioPR.ViewModels;
public class LoginForm
{
    [Required, EmailAddress, Display(Name = "อีเมล")] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "รหัสผ่าน")] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class ActivityForm : IValidatableObject
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(30), Display(Name = "รหัสกิจกรรม")] public string ActivityCode { get; set; } = "";
    [Required, StringLength(250), Display(Name = "ชื่อกิจกรรม / หัวข้อข่าว")] public string Title { get; set; } = "";
    [StringLength(1000), Display(Name = "คำอธิบายย่อ")] public string? ShortDescription { get; set; }
    [Required, StringLength(100000), Display(Name = "รายละเอียด / เนื้อหาข่าว")] public string Description { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "วันที่กิจกรรม (ค.ศ.)")] public DateTime ActivityDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date), Display(Name = "วันสิ้นสุด (ค.ศ.)")] public DateTime? EndDate { get; set; }
    [StringLength(300), Display(Name = "สถานที่")] public string? Location { get; set; }
    [StringLength(300), Display(Name = "ผู้จัด")] public string? Organizer { get; set; }
    [Range(1, int.MaxValue), Display(Name = "ประเภทกิจกรรม")] public int CategoryId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "ฝ่าย / แผนก")] public int DepartmentId { get; set; }
    [Display(Name = "กลุ่มสาระ")] public int? SubjectGroupId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "ปีการศึกษา")] public int AcademicYearId { get; set; }
    [Range(2400, 3000), Display(Name = "ปีงบประมาณ (พ.ศ.)")] public int FiscalYear { get; set; } = DateTime.Today.Year + 543;
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever, Display(Name = "URL ข่าว (สร้างอัตโนมัติ)")] public string Slug { get; set; } = "";
    [Display(Name = "โครงการ")] public bool IsProject { get; set; }
    [Display(Name = "กิจกรรมแนะนำ")] public bool IsFeatured { get; set; }
    [Display(Name = "แสดงบนเว็บไซต์สาธารณะเมื่อเผยแพร่")] public bool IsPublic { get; set; }
    [Display(Name = "ครูผู้ร่วมกิจกรรม")] public int[] TeacherIds { get; set; } = [];
    [Display(Name = "นักเรียนผู้ร่วมกิจกรรม")] public int[] StudentIds { get; set; } = [];
    [Display(Name = "บทบาทครู")] public TeacherRole TeacherRole { get; set; }
    [Display(Name = "แท็ก")] public int[] TagIds { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EndDate < ActivityDate) yield return new("วันสิ้นสุดต้องไม่ก่อนวันเริ่ม", [nameof(EndDate)]);
    }
}
public class AwardForm
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(250), Display(Name = "ชื่อรางวัล")] public string AwardName { get; set; } = "";
    [Required, StringLength(250), Display(Name = "ชื่อการแข่งขัน")] public string CompetitionName { get; set; } = "";
    [EnumDataType(typeof(AwardLevel)), Display(Name = "ระดับรางวัล")] public AwardLevel AwardLevel { get; set; }
    [Required, StringLength(100), Display(Name = "อันดับ / เหรียญ")] public string AwardRank { get; set; } = "";
    [StringLength(250), Display(Name = "ผู้จัด")] public string Organizer { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "วันที่รับรางวัล (ค.ศ.)")] public DateTime AwardDate { get; set; } = DateTime.Today;
    [Range(1, int.MaxValue), Display(Name = "ปีการศึกษา")] public int AcademicYearId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "ฝ่าย / แผนก")] public int DepartmentId { get; set; }
    [Display(Name = "กลุ่มสาระ")] public int? SubjectGroupId { get; set; }
    [Display(Name = "กิจกรรมที่เกี่ยวข้อง")] public int? ActivityId { get; set; }
    [StringLength(100000), Display(Name = "รายละเอียด")] public string Description { get; set; } = "";
    [Display(Name = "เผยแพร่สาธารณะ")] public bool IsPublic { get; set; }
    [Display(Name = "ครู")] public int[] TeacherIds { get; set; } = [];
    [Display(Name = "นักเรียน")] public int[] StudentIds { get; set; } = [];
    [EnumDataType(typeof(TeacherRole)), Display(Name = "บทบาทครู")] public TeacherRole TeacherRole { get; set; }
}
public class MasterForm : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(200), Display(Name = "ชื่อ")] public string Name { get; set; } = "";
    [StringLength(30), Display(Name = "รหัส")] public string? Code { get; set; }
    [Display(Name = "ฝ่าย / แผนก")] public int? DepartmentId { get; set; }
    [Display(Name = "กลุ่มสาระ")] public int? SubjectGroupId { get; set; }
    [StringLength(50), Display(Name = "ชั้นเรียน")] public string? Classroom { get; set; }
    [DataType(DataType.Date), Display(Name = "วันเริ่ม (ค.ศ.)")] public DateTime StartDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date), Display(Name = "วันสิ้นสุด (ค.ศ.)")] public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);
    [StringLength(1000), Display(Name = "ที่อยู่")] public string? Address { get; set; }
    [StringLength(200), Display(Name = "ผู้อำนวยการ")] public string? DirectorName { get; set; }
    [StringLength(200), Display(Name = "ข้อมูลติดต่อ")] public string? Contact { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context) { if (EndDate < StartDate) yield return new("วันสิ้นสุดไม่ถูกต้อง", [nameof(EndDate)]); }
}
public class PublicationForm
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(250), Display(Name = "ชื่อวารสาร")] public string Title { get; set; } = "";
    [Range(2000, 2400), Display(Name = "ปี (ค.ศ.)")] public int Year { get; set; } = DateTime.Today.Year;
    [Range(1, 12), Display(Name = "เดือน")] public int Month { get; set; } = DateTime.Today.Month;
    [StringLength(20000), Display(Name = "สารจากผู้อำนวยการ")] public string DirectorMessage { get; set; } = "";
    [Display(Name = "เผยแพร่สาธารณะ")] public bool IsPublic { get; set; }
    public string[] SelectedItems { get; set; } = [];
}
public class UserForm
{
    [Display(Name = "นักเรียนที่เชื่อมกับบัญชี")] public int? StudentId { get; set; }
    public string? Id { get; set; }
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, StringLength(200), Display(Name = "ชื่อผู้ใช้")] public string DisplayName { get; set; } = "";
    [DataType(DataType.Password), MinLength(12), Display(Name = "รหัสผ่าน (อย่างน้อย 12 ตัว)")] public string? Password { get; set; }
    [Required, Display(Name = "บทบาท")] public string Role { get; set; } = "Viewer";
    [Display(Name = "ฝ่าย / แผนก")] public int? DepartmentId { get; set; }
}
public class ReportFilter
{
    public string Kind { get; set; } = "MonthlyActivities";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? AcademicYearId { get; set; }
    public int? FiscalYear { get; set; }
    [Range(1,12)] public int? Month { get; set; }
    public int? DepartmentId { get; set; }
    public int? SubjectGroupId { get; set; }
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }
    public int? CategoryId { get; set; }
    public AwardLevel? AwardLevel { get; set; }
}
public record ReportRow(string Type, string Title, DateTime Date, string Department, string Category, string Participants);
public record FileInfoView(Guid Id, string Name, string ContentType, int Order);
public record CertificateRecipientView(Guid FileId, string Name, int? StudentId);
public record PublicationChoice(string Key, string Title, string Section, bool Selected);
public record PageView<T>(IReadOnlyList<T> Items, int Page, int Total, int PageSize = 20) { public int Pages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize)); }
public static class Thai
{
    public static string Date(DateTime? date) => date?.ToString("d MMM yyyy", new System.Globalization.CultureInfo("th-TH")) ?? "—";
    public static string Status(WorkflowStatus status) => status switch { WorkflowStatus.Draft => "ฉบับร่าง", WorkflowStatus.Submitted => "ส่งตรวจ", WorkflowStatus.Reviewed => "ตรวจแล้ว", WorkflowStatus.Approved => "อนุมัติ", WorkflowStatus.Published => "เผยแพร่", WorkflowStatus.Rejected => "ส่งกลับแก้ไข", _ => "เก็บถาวร" };
    public static string Level(AwardLevel level) => level switch { AwardLevel.School => "โรงเรียน", AwardLevel.District => "เขตพื้นที่", AwardLevel.Provincial => "จังหวัด", AwardLevel.Regional => "ภูมิภาค", AwardLevel.National => "ชาติ", _ => "นานาชาติ" };
}
