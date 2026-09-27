using System.ComponentModel.DataAnnotations;

namespace SchoolPortfolioPR.Models;

public enum PersonalWorkKind { Activity, Award, Certificate, Album }
public class PersonalWork : Record
{
    [Required, MaxLength(450)] public string OwnerId { get; set; } = "";
    public ApplicationUser Owner { get; set; } = null!;
    public PersonalWorkKind Kind { get; set; }
    [Required, MaxLength(250)] public string Title { get; set; } = "";
    public DateTime WorkDate { get; set; } = DateTime.Today;
    [MaxLength(80)] public string Category { get; set; } = "ทั่วไป";
    [MaxLength(200)] public string Organizer { get; set; } = "";
    [MaxLength(200)] public string Location { get; set; } = "";
    [MaxLength(150)] public string Role { get; set; } = "";
    [MaxLength(100)] public string Achievement { get; set; } = "";
    [MaxLength(4000)] public string Description { get; set; } = "";
    public ICollection<PersonalWorkMedia> Media { get; set; } = new List<PersonalWorkMedia>();
}
public class PersonalWorkMedia : Record
{
    public int PersonalWorkId { get; set; }
    public PersonalWork PersonalWork { get; set; } = null!;
    public Guid? FileId { get; set; }
    public FileObject? File { get; set; }
    [MaxLength(2048)] public string? ExternalUrl { get; set; }
    [MaxLength(500)] public string Caption { get; set; } = "";
    public int SortOrder { get; set; }
}
