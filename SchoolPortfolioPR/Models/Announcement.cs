using System.ComponentModel.DataAnnotations;

namespace SchoolPortfolioPR.Models;

public enum AnnouncementSize
{
    [Display(Name = "เล็ก · ข่าวสั้น")] Small,
    [Display(Name = "กลาง · แบนเนอร์แนวนอน")] Medium,
    [Display(Name = "ใหญ่ · ข่าวเด่น")] Large
}
public enum AnnouncementTone
{
    [Display(Name = "คราม")] Indigo,
    [Display(Name = "เขียวทะเล")] Teal,
    [Display(Name = "ส้มอำพัน")] Amber,
    [Display(Name = "ชมพู")] Rose
}
public class Announcement : Record
{
    [MaxLength(50)] public string? ReferenceCode { get; set; }
    [Required, MaxLength(250)] public string Title { get; set; } = "";
    [Required, MaxLength(500)] public string Summary { get; set; } = "";
    [MaxLength(30000)] public string Body { get; set; } = "";
    [Required, MaxLength(80)] public string Category { get; set; } = "ข่าวประชาสัมพันธ์";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsEnabled { get; set; }
    public AnnouncementSize Size { get; set; } = AnnouncementSize.Medium;
    public AnnouncementTone Tone { get; set; }
    public int SortOrder { get; set; }
    public Guid? ImageId { get; set; }
    public FileObject? Image { get; set; }
}
