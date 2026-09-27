using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SchoolPortfolioPR.Models;

public enum WorkflowStatus { Draft, Submitted, Reviewed, Approved, Published, Rejected, Archived }
public enum TeacherRole { Coach, Advisor, Supervisor, ProjectOwner }
public enum AwardLevel { School, District, Provincial, Regional, National, International }
public abstract class Record
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string? CreatedBy { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
public abstract class NamedRecord : Record
{
    [Required, MaxLength(200)] public string Name { get; set; } = "";
}
public class ApplicationUser : IdentityUser
{
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    [MaxLength(200)] public string DisplayName { get; set; } = "";
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
public class SchoolProfile : NamedRecord
{
    [MaxLength(1000)] public string? Address { get; set; }
    [MaxLength(200)] public string? DirectorName { get; set; }
    [MaxLength(200)] public string? Contact { get; set; }
}
public class Department : NamedRecord { }
public class SubjectGroup : NamedRecord { }
public class ActivityCategory : NamedRecord { }
public class Tag : NamedRecord { }
public class AcademicYear : NamedRecord
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
public class Teacher : NamedRecord
{
    [MaxLength(30)] public string Code { get; set; } = "";
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? SubjectGroupId { get; set; }
    public SubjectGroup? SubjectGroup { get; set; }
    [MaxLength(450)] public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
public class Student : NamedRecord
{
    [MaxLength(30)] public string Code { get; set; } = "";
    [MaxLength(50)] public string? Classroom { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
public class Activity : Record
{
    [Required, MaxLength(30)] public string ActivityCode { get; set; } = "";
    [Required, MaxLength(250)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? ShortDescription { get; set; }
    public string Description { get; set; } = "";
    public DateTime ActivityDate { get; set; }
    public DateTime? EndDate { get; set; }
    [MaxLength(300)] public string? Location { get; set; }
    [MaxLength(300)] public string? Organizer { get; set; }
    public int CategoryId { get; set; }
    public ActivityCategory Category { get; set; } = null!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int? SubjectGroupId { get; set; }
    public SubjectGroup? SubjectGroup { get; set; }
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public int FiscalYear { get; set; }
    public Guid? CoverImageId { get; set; }
    public FileObject? CoverImage { get; set; }
    public bool IsProject { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublic { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTime? PublishedDate { get; set; }
    [Required, MaxLength(250)] public string Slug { get; set; } = "";
    public ICollection<ActivityImage> Images { get; set; } = new List<ActivityImage>();
    public ICollection<ActivityFile> Files { get; set; } = new List<ActivityFile>();
    public ICollection<ActivityTeacher> Teachers { get; set; } = new List<ActivityTeacher>();
    public ICollection<ActivityStudent> Students { get; set; } = new List<ActivityStudent>();
    public ICollection<ActivityTag> Tags { get; set; } = new List<ActivityTag>();
    public ICollection<Award> Awards { get; set; } = new List<Award>();
}
public class FileObject
{
    [Key] public Guid FileId { get; set; } = Guid.NewGuid();
    [MaxLength(255)] public string OriginalFileName { get; set; } = "";
    [MaxLength(100)] public string StoredFileName { get; set; } = "";
    [MaxLength(150)] public string ContentType { get; set; } = "";
    [MaxLength(10)] public string FileExtension { get; set; } = "";
    public long FileSize { get; set; }
    public byte[] FileData { get; set; } = [];
    [MaxLength(64)] public string FileHash { get; set; } = "";
    [MaxLength(450)] public string UploadedBy { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
public class ActivityImage
{
    [MaxLength(500)] public string? Caption { get; set; }
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public Guid FileId { get; set; }
    public FileObject File { get; set; } = null!;
    public int SortOrder { get; set; }
}
public class ActivityImageLink : Record
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    [Required, MaxLength(2048)] public string Url { get; set; } = "";
    [MaxLength(500)] public string? Caption { get; set; }
}
public class ActivityFile
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public Guid FileId { get; set; }
    public FileObject File { get; set; } = null!;
}
public class ActivityTeacher
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public TeacherRole Role { get; set; }
}
public class ActivityStudent
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
}
public class ActivityTag
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
public class Award : Record
{
    [Required, MaxLength(250)] public string AwardName { get; set; } = "";
    [MaxLength(250)] public string CompetitionName { get; set; } = "";
    public AwardLevel AwardLevel { get; set; }
    [MaxLength(100)] public string AwardRank { get; set; } = "";
    [MaxLength(250)] public string Organizer { get; set; } = "";
    public DateTime AwardDate { get; set; }
    public int AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int? SubjectGroupId { get; set; }
    public SubjectGroup? SubjectGroup { get; set; }
    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public string Description { get; set; } = "";
    public WorkflowStatus Status { get; set; }
    public bool IsPublic { get; set; }
    public DateTime? PublishedDate { get; set; }
    public ICollection<AwardTeacher> Teachers { get; set; } = new List<AwardTeacher>();
    public ICollection<AwardStudent> Students { get; set; } = new List<AwardStudent>();
    public ICollection<AwardCertificate> Certificates { get; set; } = new List<AwardCertificate>();
}
public class AwardTeacher
{
    public int AwardId { get; set; }
    public Award Award { get; set; } = null!;
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public TeacherRole Role { get; set; }
}
public class AwardStudent
{
    public int AwardId { get; set; }
    public Award Award { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
}
public class AwardCertificate
{
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    public int AwardId { get; set; }
    public Award Award { get; set; } = null!;
    public Guid FileId { get; set; }
    public FileObject File { get; set; } = null!;
}
public class MonthlyPublication : Record
{
    [MaxLength(250)] public string Title { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public string DirectorMessage { get; set; } = "";
    public bool IsPublic { get; set; }
    public ICollection<PublicationItem> Items { get; set; } = new List<PublicationItem>();
    public ICollection<PublicationFile> Files { get; set; } = new List<PublicationFile>();
}
public class PublicationItem
{
    public int Id { get; set; }
    public int MonthlyPublicationId { get; set; }
    public MonthlyPublication MonthlyPublication { get; set; } = null!;
    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public int? AwardId { get; set; }
    public Award? Award { get; set; }
    public int SortOrder { get; set; }
    [MaxLength(100)] public string Section { get; set; } = "";
}
public class PublicationFile
{
    public int MonthlyPublicationId { get; set; }
    public MonthlyPublication MonthlyPublication { get; set; } = null!;
    public Guid FileId { get; set; }
    public FileObject File { get; set; } = null!;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
public class AuditLog
{
    public long Id { get; set; }
    [MaxLength(450)] public string User { get; set; } = "";
    [MaxLength(50)] public string Action { get; set; } = "";
    [MaxLength(100)] public string Entity { get; set; } = "";
    [MaxLength(100)] public string EntityId { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    [MaxLength(64)] public string? IPAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
