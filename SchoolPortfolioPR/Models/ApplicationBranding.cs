using System.ComponentModel.DataAnnotations;

namespace SchoolPortfolioPR.Models;

public class ApplicationBranding
{
    public int Id { get; set; } = 1;
    [Required, MaxLength(100)] public string ProgramName { get; set; } = "SchoolPortfolioPR";
    public Guid? LogoFileId { get; set; }
    public FileObject? LogoFile { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
