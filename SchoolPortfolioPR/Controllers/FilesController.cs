using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
namespace SchoolPortfolioPR.Controllers;
public class FilesController(ApplicationDbContext db, AccessService access, AnnouncementSchedule schedule) : Controller
{
    [HttpGet("files/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, bool download = false)
    {
        bool allowed = false;
        if (access.IsStudent && await db.Set<AwardCertificate>().AnyAsync(x => x.FileId == id)) {
            if (!await (await access.CertificatesAsync()).AnyAsync(x => x.FileId == id)) return NotFound();
            allowed = true;
        }
        var activity = await db.Activities.AsNoTracking().FirstOrDefaultAsync(x => x.Images.Any(i => i.FileId == id) || x.Files.Any(i => i.FileId == id));
        if (activity != null) allowed |= await access.CanReadAsync(activity, activity.DepartmentId, activity.Status, activity.IsPublic);
        var award = await db.Awards.AsNoTracking().FirstOrDefaultAsync(x => x.Certificates.Any(i => i.FileId == id));
        if (award != null && !access.IsStudent) allowed |= await access.CanReadAsync(award, award.DepartmentId, award.Status, award.IsPublic);
        var publication = await db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Files.Any(i => i.FileId == id));
        if (publication != null) {
            var canRead = await access.Publications().AnyAsync(x => x.Id == publication.Id);
            if (access.IsStudent && !canRead) return NotFound();
            allowed |= canRead;
        }
        var board = db.Announcements.AsNoTracking().Where(x => x.ImageId == id);
        if (!access.IsPublisher) board = board.Where(AnnouncementSchedule.VisibleOn(schedule.Today));
        allowed |= await board.AnyAsync();
        if (access.User.Identity?.IsAuthenticated == true)
        {
            var owner = access.UserId;
            allowed |= await db.PersonalWorkMedia.AnyAsync(x => x.FileId == id && x.PersonalWork.OwnerId == owner);
            allowed |= await db.PersonalDocuments.AnyAsync(x => x.OwnerId == owner && x.FileId == id);
            if (access.IsStudent) {
                allowed |= await db.AdmissionPortfolios.AnyAsync(x => x.OwnerId == owner && (x.PortraitId == id || x.PdfFileId == id));
                allowed |= await db.Set<PortfolioEntry>().AnyAsync(x => x.ImageId == id && x.AdmissionPortfolio.OwnerId == owner && !x.AdmissionPortfolio.IsDeleted);
            }
        }
        if (!allowed) return NotFound();
        var file = await db.FileObjects.AsNoTracking().SingleOrDefaultAsync(x => x.FileId == id); if (file == null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return download || !(file.ContentType.StartsWith("image/") || file.ContentType == "application/pdf")
            ? File(file.FileData, file.ContentType, file.OriginalFileName, enableRangeProcessing: true)
            : File(file.FileData, file.ContentType, enableRangeProcessing: true);
    }
    [HttpPost, Authorize(Policy = "Editor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var activity = await db.Activities.Include(x => x.Images).Include(x => x.Files).AsSplitQuery().FirstOrDefaultAsync(x => x.Images.Any(i => i.FileId == id) || x.Files.Any(i => i.FileId == id));
        var award = await db.Awards.Include(x => x.Certificates).FirstOrDefaultAsync(x => x.Certificates.Any(i => i.FileId == id));
        if (activity == null && award == null) return NotFound();
        if (activity != null && !await access.CanEditAsync(activity, activity.DepartmentId, activity.Status) || award != null && !await access.CanEditAsync(award, award.DepartmentId, award.Status)) return Forbid();
        var file = await db.FileObjects.SingleOrDefaultAsync(x => x.FileId == id); if (file == null) return NotFound();
        file.IsDeleted = true;
        if (activity != null) {
            if (activity.CoverImageId == id) activity.CoverImageId = null;
            db.RemoveRange(activity.Images.Where(x => x.FileId == id)); db.RemoveRange(activity.Files.Where(x => x.FileId == id));
        }
        if (award != null) db.RemoveRange(award.Certificates.Where(x => x.FileId == id));
        await db.SaveChangesAsync();
        return activity != null ? RedirectToAction("Details", "Activities", new { id = activity.Id }) : RedirectToAction("Details", "Awards", new { id = award!.Id });
    }
}
