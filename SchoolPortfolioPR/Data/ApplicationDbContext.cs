using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Models;

namespace SchoolPortfolioPR.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor http) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityImageLink> ActivityImageLinks => Set<ActivityImageLink>();
    public DbSet<ApplicationBranding> Branding => Set<ApplicationBranding>();
    public DbSet<Award> Awards => Set<Award>();
    public DbSet<FileObject> FileObjects => Set<FileObject>();
    public DbSet<MonthlyPublication> Publications => Set<MonthlyPublication>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<PersonalDocument> PersonalDocuments => Set<PersonalDocument>();
    public DbSet<PersonalWork> PersonalWorks => Set<PersonalWork>();
    public DbSet<PersonalWorkMedia> PersonalWorkMedia => Set<PersonalWorkMedia>();
    public DbSet<AdmissionPortfolio> AdmissionPortfolios => Set<AdmissionPortfolio>();
    public override Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> Remove<TEntity>(TEntity entity)
    {
        if (entity is Record record)
        {
            record.IsDeleted = true;
            var entry = Entry(entity);
            entry.Property(nameof(Record.IsDeleted)).IsModified = true;
            return entry;
        }
        return base.Remove(entity);
    }
    public override Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Remove(object entity)
    {
        if (entity is Record record)
        {
            record.IsDeleted = true;
            var entry = Entry(entity);
            entry.Property(nameof(Record.IsDeleted)).IsModified = true;
            return entry;
        }
        return base.Remove(entity);
    }
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.HasDefaultSchema("portfolio");
        b.Entity<ApplicationBranding>().Property(x => x.Id).ValueGeneratedNever();
        b.Entity<ApplicationBranding>().ToTable(t => t.HasCheckConstraint("CK_Branding_Singleton", "[Id] = 1"));
        b.Entity<ApplicationBranding>().HasData(new { Id = 1, ProgramName = "SchoolPortfolioPR" });
        b.Entity<SchoolProfile>(); b.Entity<Department>(); b.Entity<SubjectGroup>();
        b.Entity<Teacher>(); b.Entity<Student>(); b.Entity<AcademicYear>(); b.Entity<ActivityCategory>(); b.Entity<Tag>();
        b.Entity<ActivityImage>().HasKey(x => new { x.ActivityId, x.FileId });
        b.Entity<ApplicationUser>().HasIndex(x => x.StudentId).IsUnique().HasFilter("[StudentId] IS NOT NULL");
        b.Entity<PersonalDocument>().HasIndex(x => new { x.OwnerId, x.DocumentDate });
        b.Entity<PersonalWork>().HasIndex(x => new { x.OwnerId, x.Kind, x.WorkDate });
        b.Entity<PersonalWork>().ToTable(t => t.HasCheckConstraint("CK_PersonalWork_Kind", "[Kind] BETWEEN 0 AND 3"));
        b.Entity<PersonalWorkMedia>().ToTable(t => t.HasCheckConstraint("CK_PersonalWorkMedia_Source", "([FileId] IS NOT NULL AND [ExternalUrl] IS NULL) OR ([FileId] IS NULL AND [ExternalUrl] IS NOT NULL)"));
        b.Entity<AdmissionPortfolio>().HasIndex(x => x.OwnerId);
        b.Entity<AdmissionPortfolio>().Property(x => x.Gpax).HasPrecision(3, 2);
        b.Entity<PortfolioDocument>().HasKey(x => new { x.AdmissionPortfolioId, x.PersonalDocumentId });
        b.Entity<PortfolioDocument>().HasQueryFilter(x => !x.AdmissionPortfolio.IsDeleted && !x.PersonalDocument.IsDeleted);
        b.Entity<ActivityFile>().HasKey(x => new { x.ActivityId, x.FileId });
        b.Entity<ActivityTeacher>().HasKey(x => new { x.ActivityId, x.TeacherId });
        b.Entity<ActivityStudent>().HasKey(x => new { x.ActivityId, x.StudentId });
        b.Entity<ActivityTag>().HasKey(x => new { x.ActivityId, x.TagId });
        b.Entity<AwardTeacher>().HasKey(x => new { x.AwardId, x.TeacherId });
        b.Entity<AwardStudent>().HasKey(x => new { x.AwardId, x.StudentId });
        b.Entity<AwardCertificate>().HasKey(x => new { x.AwardId, x.FileId });
        b.Entity<PublicationFile>().HasKey(x => new { x.MonthlyPublicationId, x.FileId });
        b.Entity<PublicationItem>().ToTable(t => t.HasCheckConstraint("CK_PublicationItem_Source", "([ActivityId] IS NOT NULL AND [AwardId] IS NULL) OR ([ActivityId] IS NULL AND [AwardId] IS NOT NULL)"));
        b.Entity<MonthlyPublication>().ToTable(t => t.HasCheckConstraint("CK_Publication_Month", "[Month] BETWEEN 1 AND 12"));
        b.Entity<MonthlyPublication>().HasIndex(x => new { x.Year, x.Month }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Entity<Activity>().HasIndex(x => x.ActivityCode).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Entity<Activity>().HasIndex(x => x.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Entity<Activity>().HasIndex(x => x.ActivityDate);
        b.Entity<Activity>().HasIndex(x => x.PublishedDate);
        b.Entity<Activity>().HasIndex(x => new { x.Status, x.IsPublic, x.ActivityDate });
        b.Entity<Award>().HasIndex(x => x.AwardDate);
        b.Entity<Award>().HasIndex(x => x.AwardLevel);
        b.Entity<Award>().HasIndex(x => x.PublishedDate);
        b.Entity<FileObject>().HasIndex(x => x.FileHash);
        b.Entity<Announcement>().Property(x => x.StartDate).HasColumnType("date");
        b.Entity<Announcement>().Property(x => x.EndDate).HasColumnType("date");
        b.Entity<Announcement>().HasIndex(x => new { x.IsEnabled, x.StartDate, x.EndDate });
        b.Entity<Announcement>().HasIndex(x => x.ReferenceCode).IsUnique().HasFilter("[ReferenceCode] IS NOT NULL AND [IsDeleted] = 0");
        b.Entity<Announcement>().ToTable(t => {
            t.HasCheckConstraint("CK_Announcement_Dates", "[EndDate] >= [StartDate]");
            t.HasCheckConstraint("CK_Announcement_Size", "[Size] BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_Announcement_Tone", "[Tone] BETWEEN 0 AND 3");
        });
        b.Entity<FileObject>().Property(x => x.FileData).HasColumnType("varbinary(max)");
        b.Entity<FileObject>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<AuditLog>().HasIndex(x => x.Timestamp);
        b.Entity<Teacher>().HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Entity<Student>().HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Entity<ActivityImage>().HasQueryFilter(x => !x.Activity.IsDeleted && !x.File.IsDeleted);
        b.Entity<ActivityFile>().HasQueryFilter(x => !x.Activity.IsDeleted && !x.File.IsDeleted);
        b.Entity<ActivityTeacher>().HasQueryFilter(x => !x.Activity.IsDeleted && !x.Teacher.IsDeleted);
        b.Entity<ActivityStudent>().HasQueryFilter(x => !x.Activity.IsDeleted && !x.Student.IsDeleted);
        b.Entity<ActivityTag>().HasQueryFilter(x => !x.Activity.IsDeleted && !x.Tag.IsDeleted);
        b.Entity<AwardTeacher>().HasQueryFilter(x => !x.Award.IsDeleted && !x.Teacher.IsDeleted);
        b.Entity<AwardStudent>().HasQueryFilter(x => !x.Award.IsDeleted && !x.Student.IsDeleted);
        b.Entity<AwardCertificate>().HasQueryFilter(x => !x.Award.IsDeleted && !x.File.IsDeleted);
        b.Entity<PublicationFile>().HasQueryFilter(x => !x.MonthlyPublication.IsDeleted && !x.File.IsDeleted);
        b.Entity<PublicationItem>().HasQueryFilter(x => !x.MonthlyPublication.IsDeleted);
        foreach (var type in b.Model.GetEntityTypes().ToList())
        {
            foreach (var fk in type.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
            if (!typeof(Record).IsAssignableFrom(type.ClrType)) continue;
            var p = Expression.Parameter(type.ClrType, "x");
            b.Entity(type.ClrType).HasQueryFilter(Expression.Lambda(Expression.Not(Expression.Property(p, "IsDeleted")), p));
        }
        b.Entity<PersonalWorkMedia>().HasQueryFilter(x => !x.IsDeleted && !x.PersonalWork.IsDeleted && (x.FileId == null || !x.File!.IsDeleted));
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var user = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var changes = ChangeTracker.Entries().Where(e => e.Entity is not AuditLog && e.Entity is not ApplicationUser && !e.Metadata.ClrType.Namespace!.StartsWith("Microsoft.AspNetCore.Identity") && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)).ToList();
        var audits = new List<(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry, AuditLog Log)>();
        foreach (var e in changes)
        {
            var action = e.State.ToString();
            if (e.Entity is Record deleted && deleted.IsDeleted && e.Property(nameof(Record.IsDeleted)).IsModified) action = "Deleted";
            if (e.Entity is Record r)
            {
                if (e.State == EntityState.Added) r.CreatedBy = user;
                if (e.State == EntityState.Deleted) { e.State = EntityState.Modified; r.IsDeleted = true; }
            }
            var personal = e.Entity is PersonalDocument or AdmissionPortfolio or PortfolioEntry or PortfolioDocument or PersonalWork or Models.PersonalWorkMedia;
            object Values(bool old) => e.Properties.Where(p => p.Metadata.Name != "FileData" && p.Metadata.Name != "RowVersion"
                    && (!personal || p.Metadata.IsPrimaryKey() || p.Metadata.Name is "OwnerId" or "AdmissionPortfolioId" or "IsDeleted"))
                .ToDictionary(p => p.Metadata.Name, p => old ? p.OriginalValue : p.CurrentValue);
            audits.Add((e, new AuditLog { User = user, Action = action, Entity = e.Metadata.ClrType.Name,
                OldValue = action == "Added" ? null : JsonSerializer.Serialize(Values(true)),
                NewValue = JsonSerializer.Serialize(Values(false)), IPAddress = http.HttpContext?.Connection.RemoteIpAddress?.ToString() }));
        }
        var ownTransaction = Database.CurrentTransaction == null;
        await using var tx = ownTransaction ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var result = await base.SaveChangesAsync(cancellationToken);
        foreach (var (entry, log) in audits)
        {
            log.EntityId = string.Join(",", entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue));
            AuditLogs.Add(log);
        }
        if (audits.Count > 0) await base.SaveChangesAsync(cancellationToken);
        if (tx != null) await tx.CommitAsync(cancellationToken);
        return result;
    }
}
