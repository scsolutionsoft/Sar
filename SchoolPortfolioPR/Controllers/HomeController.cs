
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
namespace SchoolPortfolioPR.Controllers;
public class HomeController(ApplicationDbContext db, AnnouncementSchedule schedule, AccessService access) : Controller
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var news = db.Activities.AsNoTracking().Where(x => x.Status == WorkflowStatus.Published && x.IsPublic);
        if (!string.IsNullOrWhiteSpace(q)) news = news.Where(x => x.Title.Contains(q) || (x.ShortDescription != null && x.ShortDescription.Contains(q)));
        page = Math.Max(1, page); ViewBag.Search = q;
        ViewBag.School = await db.Set<SchoolProfile>().AsNoTracking().FirstOrDefaultAsync();
        ViewBag.Announcements = await db.Announcements.AsNoTracking().Where(AnnouncementSchedule.VisibleOn(schedule.Today))
            .OrderBy(x => x.SortOrder).ThenByDescending(x => x.Id).ToListAsync();
        ViewBag.Featured = await news.Where(x => x.IsFeatured).OrderByDescending(x => x.PublishedDate).Take(3).ToListAsync();
        var awards = access.IsStudent ? await access.AwardsAsync() : db.Awards.AsNoTracking().Where(x => x.Status == WorkflowStatus.Published && x.IsPublic);
        ViewBag.Awards = await awards.OrderByDescending(x => x.AwardDate).Take(6).ToListAsync();
        ViewBag.Publications = await access.Publications().Where(x => x.IsPublic).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).Take(4).ToListAsync();
        return View(new SchoolPortfolioPR.ViewModels.PageView<Activity>(await news.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * 12).Take(12).ToListAsync(), page, await news.CountAsync(), 12));
    }
    [HttpGet("news/{year:int}/{slug}")]
    public async Task<IActionResult> News(int year, string slug)
    {
        var a = await db.Activities.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.Status == WorkflowStatus.Published && x.IsPublic && x.ActivityDate.Year + 543 == year);
        return a == null ? NotFound() : RedirectToAction("Details", "Activities", new { id = a.Id });
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

