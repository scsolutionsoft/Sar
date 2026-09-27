using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Tests;
public class SecurityTests
{
    private static (ApplicationDbContext, AccessService) Access(string? role, string user = "author")
    {
        var identity = role == null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user), new Claim(ClaimTypes.Role, role)], "test");
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=localhost;Database=test;Integrated Security=true;TrustServerCertificate=true").Options, http);
        return (db, new AccessService(db, http));
    }
    [Theory]
    [InlineData("Teacher", WorkflowStatus.Draft, WorkflowStatus.Submitted, true)]
    [InlineData("Teacher", WorkflowStatus.Submitted, WorkflowStatus.Approved, false)]
    [InlineData("Teacher", WorkflowStatus.Approved, WorkflowStatus.Published, false)]
    [InlineData("Approver", WorkflowStatus.Reviewed, WorkflowStatus.Approved, true)]
    [InlineData("Approver", WorkflowStatus.Approved, WorkflowStatus.Published, false)]
    [InlineData("PRAdmin", WorkflowStatus.Approved, WorkflowStatus.Published, true)]
    [InlineData("PRAdmin", WorkflowStatus.Draft, WorkflowStatus.Published, false)]
    [InlineData("SuperAdmin", WorkflowStatus.Draft, WorkflowStatus.Published, false)]
    [InlineData("SuperAdmin", WorkflowStatus.Submitted, WorkflowStatus.Reviewed, true)]
    [InlineData("Viewer", WorkflowStatus.Draft, WorkflowStatus.Submitted, false)]
    [InlineData("Student", WorkflowStatus.Draft, WorkflowStatus.Submitted, false)]
    public async Task Workflow_enforces_role_and_sequence(string role, WorkflowStatus from, WorkflowStatus to, bool allowed)
    {
        var (db, access) = Access(role); await using var disposable = db;
        Assert.Equal(allowed, await access.CanTransitionAsync(new Activity { CreatedBy = "author" }, 1, from, to));
    }
    [Fact] public async Task Anonymous_cannot_read_private_or_draft()
    {
        var (db, access) = Access(null); await using var disposable = db; var a = new Activity();
        Assert.False(await access.CanReadAsync(a, 1, WorkflowStatus.Draft, true));
        Assert.False(await access.CanReadAsync(a, 1, WorkflowStatus.Published, false));
        Assert.True(await access.CanReadAsync(a, 1, WorkflowStatus.Published, true));
    }
    [Fact] public async Task Teacher_cannot_edit_others_or_published_records()
    {
        var (db, access) = Access("Teacher"); await using var disposable = db;
        Assert.False(await access.CanEditAsync(new Activity { CreatedBy = "other" }, 1, WorkflowStatus.Draft));
        Assert.False(await access.CanEditAsync(new Activity { CreatedBy = "author" }, 1, WorkflowStatus.Published));
    }
    [Theory] [InlineData("Viewer")] [InlineData("Student")]
    public async Task Read_only_roles_cannot_author_even_when_record_was_previously_theirs(string role)
    {
        var (db, access) = Access(role); await using var disposable = db;
        Assert.False(await access.CanAuthorAsync());
        Assert.False(await access.CanCreateAsync(1));
        Assert.False(await access.CanEditAsync(new Activity { CreatedBy = "author" }, 1, WorkflowStatus.Draft));
    }
    [Theory] [InlineData(".jpg")] [InlineData(".png")] [InlineData(".webp")] [InlineData(".pdf")] [InlineData(".docx")] [InlineData(".xlsx")]
    public void Html_disguised_as_document_is_rejected(string extension) => Assert.False(SqlFileStorage.ValidSignature(Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"), extension));
    [Fact] public async Task Mime_mismatch_is_rejected_before_storage()
    {
        var (db, _) = Access("Teacher"); await using var disposable = db;
        var service = new SqlFileStorage(db, new ConfigurationBuilder().Build());
        var file = new FormFile(new MemoryStream(new byte[12]), 0, 12, "file", "test.pdf") { Headers = new HeaderDictionary(), ContentType = "text/html" };
        await Assert.ThrowsAsync<InvalidDataException>(() => service.StoreAsync(file, "author"));
    }
    [Fact] public void Buddhist_date_is_displayed_and_html_is_encoded()
    {
        Assert.Contains("2569", Thai.Date(new DateTime(2026, 9, 26)));
        var html = new ExportService().ReportHtml("<script>alert(1)</script>", []);
        Assert.DoesNotContain("<script>", html); Assert.Contains("&lt;script&gt;", html);
    }
    [Fact] public void Office_exports_are_valid_packages_and_excel_does_not_execute_formulas()
    {
        var export = new ExportService(); List<ReportRow> rows = [new("กิจกรรม", "=HYPERLINK(\"https://evil.invalid\")", DateTime.Today, "ฝ่าย", "หมวด", "ครู")];
        using var excel = new ClosedXML.Excel.XLWorkbook(new MemoryStream(export.Excel("ทดสอบ", rows)));
        Assert.False(excel.Worksheet(1).Cell(2,3).HasFormula);
        using var word = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(new MemoryStream(export.Word("ทดสอบ", rows)), false);
        Assert.Contains("ทดสอบ", word.MainDocumentPart!.Document.InnerText);
    }
    [Fact] public void Database_model_has_binary_storage_concurrency_and_restricted_foreign_keys()
    {
        var (db, _) = Access("SuperAdmin"); using var disposable = db;
        Assert.Equal("varbinary(max)", db.Model.FindEntityType(typeof(FileObject))!.FindProperty("FileData")!.GetColumnType());
        Assert.True(db.Model.FindEntityType(typeof(Activity))!.FindProperty("RowVersion")!.IsConcurrencyToken);
        Assert.All(db.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }
    [Fact] public void Soft_delete_does_not_sever_loaded_participant_relationships()
    {
        var (db, _) = Access("SuperAdmin"); using var disposable = db;
        var activity = new Activity { Id = 10, Teachers = [new ActivityTeacher { ActivityId = 10, TeacherId = 2 }], Students = [new ActivityStudent { ActivityId = 10, StudentId = 3 }] };
        db.Attach(activity); db.Remove(activity);
        Assert.True(activity.IsDeleted);
        Assert.Equal(EntityState.Modified, db.Entry(activity).State);
        Assert.All(activity.Students, s => Assert.Equal(EntityState.Unchanged, db.Entry(s).State));
        Assert.All(activity.Teachers, t => Assert.Equal(EntityState.Unchanged, db.Entry(t).State));
    }
}
