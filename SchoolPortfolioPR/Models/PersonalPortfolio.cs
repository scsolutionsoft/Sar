using System.ComponentModel.DataAnnotations;
namespace SchoolPortfolioPR.Models;

public class PersonalDocument : Record
{
    [Required, MaxLength(450)] public string OwnerId { get; set; } = "";
    public ApplicationUser Owner { get; set; } = null!;
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [Required, MaxLength(80)] public string Category { get; set; } = "เอกสารทั่วไป";
    [MaxLength(2000)] public string Description { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public int Pages { get; set; }
    public Guid FileId { get; set; }
    public FileObject File { get; set; } = null!;
}
public enum PortfolioTheme { Academic, Botanical, Creative }
public enum PortfolioSection { Project, Activity, Award, Volunteer, Certificate }
public class AdmissionPortfolio : Record
{
    [Required, MaxLength(450)] public string OwnerId { get; set; } = "";
    public ApplicationUser Owner { get; set; } = null!;
    [Required, MaxLength(200)] public string Title { get; set; } = "แฟ้มสะสมผลงานเพื่อการศึกษาต่อ";
    [Required, MaxLength(200)] public string FullName { get; set; } = "";
    [Required, MaxLength(200)] public string School { get; set; } = "";
    [MaxLength(200)] public string StudyPlan { get; set; } = "";
    [MaxLength(100)] public string GraduationYear { get; set; } = "";
    public decimal? Gpax { get; set; }
    [MaxLength(200)] public string ContactEmail { get; set; } = "";
    [MaxLength(50)] public string Phone { get; set; } = "";
    [MaxLength(200)] public string TargetUniversity { get; set; } = "";
    [MaxLength(200)] public string TargetFaculty { get; set; } = "";
    [MaxLength(100)] public string AdmissionRound { get; set; } = "รอบ Portfolio";
    [MaxLength(1000)] public string RequirementsUrl { get; set; } = "";
    [MaxLength(1200)] public string Bio { get; set; } = "";
    [MaxLength(2200)] public string Motivation { get; set; } = "";
    [MaxLength(1200)] public string Education { get; set; } = "";
    [MaxLength(1200)] public string Skills { get; set; } = "";
    [MaxLength(1000)] public string Goals { get; set; } = "";
    public PortfolioTheme Theme { get; set; }
    public bool IncludeContents { get; set; } = true;
    public bool IncludeClosing { get; set; }
    public bool IncludeEvidence { get; set; } = true;
    public int MaxPages { get; set; } = 10;
    public int MaxMegabytes { get; set; } = 10;
    public Guid? PortraitId { get; set; }
    public FileObject? Portrait { get; set; }
    public Guid? PdfFileId { get; set; }
    public FileObject? PdfFile { get; set; }
    public int? ExportedPages { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PortfolioEntry> Entries { get; set; } = new List<PortfolioEntry>();
    public ICollection<PortfolioDocument> Documents { get; set; } = new List<PortfolioDocument>();
}
public class PortfolioEntry : Record
{
    public int AdmissionPortfolioId { get; set; }
    public AdmissionPortfolio AdmissionPortfolio { get; set; } = null!;
    public PortfolioSection Section { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    [MaxLength(150)] public string Role { get; set; } = "";
    [MaxLength(1200)] public string Description { get; set; } = "";
    [MaxLength(800)] public string Outcome { get; set; } = "";
    [MaxLength(800)] public string Reflection { get; set; } = "";
    [MaxLength(1000)] public string Link { get; set; } = "";
    public int SortOrder { get; set; }
    public Guid? ImageId { get; set; }
    public FileObject? Image { get; set; }
    public int? PersonalDocumentId { get; set; }
    public PersonalDocument? PersonalDocument { get; set; }
}
public class PortfolioDocument
{
    public int AdmissionPortfolioId { get; set; }
    public AdmissionPortfolio AdmissionPortfolio { get; set; } = null!;
    public int PersonalDocumentId { get; set; }
    public PersonalDocument PersonalDocument { get; set; } = null!;
    public int SortOrder { get; set; }
}
