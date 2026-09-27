using System.ComponentModel.DataAnnotations;

namespace SchoolPortfolioPR.ViewModels;

public class BrandingForm
{
    [Required(ErrorMessage = "กรุณาระบุชื่อโปรแกรม"), StringLength(100, ErrorMessage = "ชื่อโปรแกรมยาวได้ไม่เกิน 100 ตัวอักษร"), Display(Name = "ชื่อโปรแกรม")]
    public string ProgramName { get; set; } = "";
    public IFormFile? Logo { get; set; }
    public bool RemoveLogo { get; set; }
    [Required] public string RowVersion { get; set; } = "";
    public Guid? CurrentLogoId { get; set; }
}
public record BrandingView(string ProgramName, Guid? LogoFileId);
