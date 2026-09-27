using System.ComponentModel.DataAnnotations;
using SchoolPortfolioPR.Models;
namespace SchoolPortfolioPR.ViewModels;

public class DocumentForm
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(200), Display(Name = "ชื่อเอกสาร")] public string Title { get; set; } = "";
    [Required, StringLength(80), Display(Name = "หมวดหมู่")] public string Category { get; set; } = "เอกสารทั่วไป";
    [StringLength(2000), Display(Name = "รายละเอียด")] public string? Description { get; set; }
    [DataType(DataType.Date), Display(Name = "วันที่เอกสาร (ค.ศ.)")] public DateTime DocumentDate { get; set; } = DateTime.UtcNow.AddHours(7).Date;
    public IFormFile? Upload { get; set; }
    [StringLength(15000), Display(Name = "เนื้อหารายงาน (ใช้เมื่อสร้าง PDF ใหม่)")] public string? ReportText { get; set; }
}
public class AdmissionForm : IValidatableObject
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(200), Display(Name = "ชื่อแฟ้ม")] public string Title { get; set; } = "แฟ้มสะสมผลงานเพื่อการศึกษาต่อ";
    [Required, StringLength(200), Display(Name = "ชื่อ–นามสกุล")] public string FullName { get; set; } = "";
    [Required, StringLength(200), Display(Name = "โรงเรียน / สถานศึกษา")] public string School { get; set; } = "";
    [StringLength(200), Display(Name = "แผนการเรียน")] public string? StudyPlan { get; set; }
    [StringLength(100), Display(Name = "ปีที่สำเร็จการศึกษา (พ.ศ.)")] public string? GraduationYear { get; set; }
    [Range(0,4), Display(Name = "GPAX (ตามหลักฐานจริง)")] public decimal? Gpax { get; set; }
    [EmailAddress, StringLength(200), Display(Name = "อีเมลติดต่อ")] public string? ContactEmail { get; set; }
    [StringLength(50), Display(Name = "โทรศัพท์")] public string? Phone { get; set; }
    [StringLength(200), Display(Name = "มหาวิทยาลัยที่ต้องการยื่น")] public string? TargetUniversity { get; set; }
    [StringLength(200), Display(Name = "คณะ / สาขา")] public string? TargetFaculty { get; set; }
    [StringLength(100), Display(Name = "รอบรับสมัคร")] public string? AdmissionRound { get; set; } = "รอบ Portfolio";
    [StringLength(1000), Display(Name = "ลิงก์ประกาศรับสมัคร")] public string? RequirementsUrl { get; set; }
    [StringLength(1200), Display(Name = "แนะนำตัวและความสนใจ")] public string? Bio { get; set; }
    [StringLength(2200), Display(Name = "เหตุผลที่เลือกเรียน / Statement of Purpose")] public string? Motivation { get; set; }
    [StringLength(1200), Display(Name = "ประวัติการศึกษา (หนึ่งช่วงเวลาต่อบรรทัด)")] public string? Education { get; set; }
    [StringLength(1200), Display(Name = "ทักษะ (หนึ่งทักษะต่อบรรทัด เช่น ภาษาอังกฤษ | 70)")] public string? Skills { get; set; }
    [StringLength(1000), Display(Name = "เป้าหมายการศึกษาและอาชีพ")] public string? Goals { get; set; }
    [EnumDataType(typeof(PortfolioTheme)), Display(Name = "แม่แบบอินโฟกราฟิก")] public PortfolioTheme Theme { get; set; }
    [Display(Name = "มีสารบัญ")] public bool IncludeContents { get; set; } = true;
    [Display(Name = "มีหน้าปิดท้าย")] public bool IncludeClosing { get; set; }
    [Display(Name = "รวม PDF หลักฐานต่อท้ายเล่ม")] public bool IncludeEvidence { get; set; } = true;
    [Range(1,100), Display(Name = "จำนวนหน้าสูงสุด (รวมทุกหน้า)")] public int MaxPages { get; set; } = 10;
    [Range(1,50), Display(Name = "ขนาดไฟล์สูงสุด (MB)")] public int MaxMegabytes { get; set; } = 10;
    public IFormFile? PortraitUpload { get; set; }
    public Guid? CurrentPortraitId { get; set; }
    [Display(Name = "ลบภาพปกเดิม")] public bool RemovePortrait { get; set; }
    public int[] DocumentIds { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!PersonalInput.SafeLink(RequirementsUrl)) yield return new("ลิงก์ต้องเริ่มด้วย https:// หรือ http://", [nameof(RequirementsUrl)]);
        if (DocumentIds.Length > 30 || DocumentIds.Distinct().Count() != DocumentIds.Length) yield return new("เลือกเอกสารไม่เกิน 30 รายการและไม่ซ้ำกัน", [nameof(DocumentIds)]);
        if (RemovePortrait && PortraitUpload != null) yield return new("ยกเลิกการลบภาพก่อนอัปโหลดภาพใหม่", [nameof(RemovePortrait)]);
    }
}
public class PortfolioEntryForm : IValidatableObject
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public string? RowVersion { get; set; }
    public string? PortfolioVersion { get; set; }
    [EnumDataType(typeof(PortfolioSection)), Display(Name = "ประเภทผลงาน")] public PortfolioSection Section { get; set; }
    [Required, StringLength(200), Display(Name = "ชื่อผลงาน / กิจกรรม")] public string Title { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "วันที่ (ค.ศ.)")] public DateTime Date { get; set; } = DateTime.UtcNow.AddHours(7).Date;
    [StringLength(150), Display(Name = "บทบาทและหน้าที่ของฉัน")] public string? Role { get; set; }
    [StringLength(1200), Display(Name = "แนวคิดและขั้นตอนการทำงาน")] public string? Description { get; set; }
    [StringLength(800), Display(Name = "ผลลัพธ์ / รางวัลที่ได้รับ")] public string? Outcome { get; set; }
    [StringLength(800), Display(Name = "สิ่งที่ได้เรียนรู้และการต่อยอด")] public string? Reflection { get; set; }
    [StringLength(1000), Display(Name = "ลิงก์ผลงาน / วิดีโอ")] public string? Link { get; set; }
    [Range(0,9999), Display(Name = "ลำดับแสดง")] public int SortOrder { get; set; }
    [Display(Name = "หลักฐาน PDF จากคลังส่วนตัว")] public int? PersonalDocumentId { get; set; }
    public IFormFile? ImageUpload { get; set; }
    public Guid? CurrentImageId { get; set; }
    [Display(Name = "ลบภาพเดิม")] public bool RemoveImage { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!PersonalInput.SafeLink(Link)) yield return new("ลิงก์ต้องเริ่มด้วย https:// หรือ http://", [nameof(Link)]);
        if (Date.Year is < 1900 or > 2400) yield return new("กรุณาระบุวันที่ ค.ศ.", [nameof(Date)]);
        if (RemoveImage && ImageUpload != null) yield return new("ยกเลิกการลบภาพก่อนอัปโหลดใหม่", [nameof(RemoveImage)]);
    }
}
public static class PersonalInput
{
    public static bool SafeLink(string? value) => string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http";
    public static string Section(PortfolioSection value) => value switch { PortfolioSection.Project => "โครงงาน", PortfolioSection.Activity => "กิจกรรม", PortfolioSection.Award => "รางวัล", PortfolioSection.Volunteer => "จิตอาสา", _ => "เกียรติบัตร" };
}
