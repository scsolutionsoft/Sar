using System.ComponentModel.DataAnnotations;
using SchoolPortfolioPR.Models;

namespace SchoolPortfolioPR.ViewModels;

public class AnnouncementForm : IValidatableObject
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    [Required, StringLength(250), Display(Name = "หัวข้อข่าว")] public string Title { get; set; } = "";
    [Required, StringLength(500), Display(Name = "ข้อความบนกรอบข่าว")] public string Summary { get; set; } = "";
    [StringLength(30000), Display(Name = "รายละเอียดข่าว")] public string? Body { get; set; }
    [Required, StringLength(80), Display(Name = "หมวดหมู่")] public string Category { get; set; } = "ข่าวประชาสัมพันธ์";
    [DataType(DataType.Date), Display(Name = "เริ่มแสดงวันที่ (ค.ศ.)")] public DateTime StartDate { get; set; }
    [DataType(DataType.Date), Display(Name = "แสดงถึงวันที่ (ค.ศ.)")] public DateTime EndDate { get; set; }
    [Display(Name = "เปิดแสดงบนหน้าแรกตามกำหนดเวลา")] public bool IsEnabled { get; set; }
    [EnumDataType(typeof(AnnouncementSize)), Display(Name = "ขนาดกรอบข่าว")] public AnnouncementSize Size { get; set; } = AnnouncementSize.Medium;
    [EnumDataType(typeof(AnnouncementTone)), Display(Name = "โทนสี")] public AnnouncementTone Tone { get; set; }
    [Range(0, 9999), Display(Name = "ลำดับแสดง (เลขน้อยขึ้นก่อน)")] public int SortOrder { get; set; }
    [Display(Name = "อัปโหลดภาพใหม่")] public IFormFile? Upload { get; set; }
    public Guid? SourceImageId { get; set; }
    public Guid? CurrentImageId { get; set; }
    [Display(Name = "ลบภาพเดิม")] public bool RemoveImage { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartDate.Year < 2000 || EndDate.Year > 2400) yield return new("กรุณาระบุวันที่ ค.ศ. ระหว่างปี 2000–2400", [nameof(StartDate), nameof(EndDate)]);
        if (EndDate.Date < StartDate.Date) yield return new("วันสิ้นสุดต้องไม่ก่อนวันเริ่มแสดง", [nameof(EndDate)]);
        if (Upload != null && SourceImageId.HasValue) yield return new("เลือกภาพจากคลังหรืออัปโหลดใหม่อย่างใดอย่างหนึ่ง", [nameof(Upload)]);
        if (RemoveImage && (Upload != null || SourceImageId.HasValue)) yield return new("ยกเลิกการลบภาพเดิมก่อนเลือกภาพใหม่", [nameof(RemoveImage)]);
    }
}
public record AnnouncementImageChoice(Guid Id, string Name, string ActivityTitle);
