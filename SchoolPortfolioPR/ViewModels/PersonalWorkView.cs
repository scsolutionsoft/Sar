using System.ComponentModel.DataAnnotations;
using SchoolPortfolioPR.Models;

namespace SchoolPortfolioPR.ViewModels;

public static class WorkKinds
{
    public static readonly IReadOnlyDictionary<PersonalWorkKind,string> Labels = new Dictionary<PersonalWorkKind,string> {
        [PersonalWorkKind.Activity]="กิจกรรม",[PersonalWorkKind.Award]="รางวัล",[PersonalWorkKind.Certificate]="เกียรติบัตร",[PersonalWorkKind.Album]="อัลบั้มภาพ" };
    public static string Icon(PersonalWorkKind kind) => kind switch { PersonalWorkKind.Activity=>"activity",PersonalWorkKind.Award=>"award",PersonalWorkKind.Certificate=>"file",_=>"image" };
    public static string Tone(PersonalWorkKind kind) => kind switch { PersonalWorkKind.Activity=>"blue",PersonalWorkKind.Award=>"amber",PersonalWorkKind.Certificate=>"violet",_=>"teal" };
}
public class PersonalWorkForm : IValidatableObject
{
    public int Id { get; set; }
    public string? RowVersion { get; set; }
    public PersonalWorkKind Kind { get; set; }
    [Required(ErrorMessage="กรุณาระบุชื่อรายการ"),StringLength(250),Display(Name="ชื่อรายการ")]
    public string Title { get; set; } = "";
    [DataType(DataType.Date),Display(Name="วันที่กิจกรรม / ได้รับรางวัล")]
    public DateTime WorkDate { get; set; } = DateTime.Today;
    [StringLength(80),Display(Name="หมวดหมู่")] public string? Category { get; set; } = "ทั่วไป";
    [StringLength(200),Display(Name="หน่วยงาน / ผู้จัด / ผู้ออกเกียรติบัตร")] public string? Organizer { get; set; }
    [StringLength(200),Display(Name="สถานที่")] public string? Location { get; set; }
    [StringLength(150),Display(Name="บทบาทหรือหน้าที่ของฉัน")] public string? Role { get; set; }
    [StringLength(100),Display(Name="ระดับ / ผลรางวัล")] public string? Achievement { get; set; }
    [StringLength(4000),Display(Name="รายละเอียดและสิ่งที่ได้รับ")] public string? Description { get; set; }
    public List<IFormFile> Uploads { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(!Enum.IsDefined(Kind))yield return new("ประเภทผลงานไม่ถูกต้อง",[nameof(Kind)]);
        if(WorkDate.Year is <1900 or >2400)yield return new("กรุณาระบุวันที่ ค.ศ. ที่ถูกต้อง",[nameof(WorkDate)]);
        if(Uploads.Count>20||Uploads.Sum(x=>x.Length)>52428800)yield return new("เลือกได้สูงสุด 20 ไฟล์ รวมไม่เกิน 50 MB",[nameof(Uploads)]);
    }
}
public record PersonalWorkCard(int Id,PersonalWorkKind Kind,string Title,DateTime Date,string Category,string Description,Guid? CoverId,int MediaCount);
public record WorkMediaView(int Id,Guid? FileId,string Name,string ContentType,string Caption,string? ExternalUrl);
public record PersonalWorkDetail(PersonalWork Work,List<WorkMediaView> Media);
