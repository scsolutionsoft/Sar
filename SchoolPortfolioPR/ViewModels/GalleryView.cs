using System.ComponentModel.DataAnnotations;
using SchoolPortfolioPR.Services;

namespace SchoolPortfolioPR.ViewModels;

public enum GallerySource { Upload, External }
public class GalleryEntryForm : IValidatableObject
{
    public int ActivityId { get; set; }
    public Guid? FileId { get; set; }
    public int LinkId { get; set; }
    public GallerySource Source { get; set; }
    [StringLength(500, ErrorMessage = "คำบรรยายยาวได้ไม่เกิน 500 ตัวอักษร"), Display(Name = "คำบรรยายภาพ")]
    public string? Caption { get; set; }
    [StringLength(2048), Display(Name = "ลิงก์ภาพหรืออัลบั้มจากคลาวด์")]
    public string? ExternalUrl { get; set; }
    public List<IFormFile> Files { get; set; } = [];
    [Required] public string RowVersion { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Enum.IsDefined(Source)) yield return new("กรุณาเลือกแหล่งภาพ", [nameof(Source)]);
        if (Source == GallerySource.External && !GalleryLinks.IsValid(ExternalUrl)) yield return new("กรุณาระบุ URL แบบ https:// หรือ http:// ที่ถูกต้อง", [nameof(ExternalUrl)]);
        if (Source == GallerySource.Upload && FileId == null && Files.Count == 0) yield return new("กรุณาเลือกรูปภาพอย่างน้อยหนึ่งไฟล์", [nameof(Files)]);
        if (Files.Count > 20 || Files.Sum(x => x.Length) > 52428800) yield return new("อัปโหลดได้สูงสุด 20 ภาพ รวมไม่เกิน 50 MB", [nameof(Files)]);
        if (Files.Any(x => !new[] { ".png", ".jpg", ".jpeg", ".webp" }.Contains(Path.GetExtension(x.FileName).ToLowerInvariant()))) yield return new("ใช้รูปภาพ PNG, JPG หรือ WebP เท่านั้น", [nameof(Files)]);
        if (FileId != null && (Source != GallerySource.Upload || LinkId != 0) || LinkId != 0 && Source != GallerySource.External || Source == GallerySource.External && Files.Count > 0 || FileId != null && Files.Count > 0)
            yield return new("รูปแบบรายการภาพไม่ถูกต้อง");
    }
}
public record GalleryItemView(Guid? FileId, int LinkId, string Title, string? Caption, string? ExternalUrl);
public record ActivityGalleryView(int ActivityId, string RowVersion, bool CanEdit, List<GalleryItemView> Items);
public record GalleryAlbumView(int Id, string Title, DateTime Date, string Department, Guid? CoverId, int Images, int Links);
