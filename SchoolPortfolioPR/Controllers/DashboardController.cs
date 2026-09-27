using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
namespace SchoolPortfolioPR.Controllers;
[Authorize]
public class DashboardController(ApplicationDbContext db, AccessService access, WorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Workspace = await workspace.BuildAsync();
        var a = await access.ActivitiesAsync(); var w = await access.AwardsAsync(); var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); var end = start.AddMonths(1);
        ViewBag.Cards = new Dictionary<string, int> {
            ["กิจกรรมทั้งหมด"] = await a.CountAsync(), ["กิจกรรมเดือนนี้"] = await a.CountAsync(x => x.ActivityDate >= start && x.ActivityDate < end),
            ["ข่าวเผยแพร่"] = await a.CountAsync(x => x.Status == WorkflowStatus.Published && x.IsPublic), ["รางวัลครู"] = await w.CountAsync(x => x.Teachers.Any()),
            ["รางวัลนักเรียน"] = await w.CountAsync(x => x.Students.Any()), ["รางวัลระดับชาติ"] = await w.CountAsync(x => x.AwardLevel == AwardLevel.National),
            ["รางวัลภูมิภาค"] = await w.CountAsync(x => x.AwardLevel == AwardLevel.Regional),
            ["เกียรติบัตร"] = await (await access.CertificatesAsync()).CountAsync(),
            ["รูปภาพ"] = await db.Set<ActivityImage>().CountAsync(x => a.Select(i => i.Id).Contains(x.ActivityId) && !x.File.IsDeleted) };
        ViewBag.Month = await a.Where(x => x.ActivityDate.Year == DateTime.Today.Year).GroupBy(x => x.ActivityDate.Month).OrderBy(x => x.Key).Select(x => new ChartPoint(x.Key.ToString(), x.Count())).ToListAsync();
        ViewBag.Department = await a.GroupBy(x => x.Department.Name).Select(x => new ChartPoint(x.Key, x.Count())).ToListAsync();
        ViewBag.Category = await a.GroupBy(x => x.Category.Name).Select(x => new ChartPoint(x.Key, x.Count())).ToListAsync();
        var levels = await w.GroupBy(x => x.AwardLevel).Select(x => new { Level = x.Key, Count = x.Count() }).ToListAsync(); ViewBag.Level = levels.Select(x => new ChartPoint(ViewModels.Thai.Level(x.Level), x.Count)).ToList();
        return View(await a.OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync());
    }
}
public record ChartPoint(string Label, int Count);
