using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;

[Authorize(Policy = "Editor")]
public class WorkflowsController(AccessService access) : Controller
{
    public async Task<IActionResult> Index(int page = 1)
    {
        page = Math.Max(1, page);
        var activities = await access.PendingActivitiesAsync(); var awards = await access.PendingAwardsAsync();
        var items = activities.Select(x => new { x.Id, Controller = "Activities", x.Title, Department = x.Department.Name, x.Status, x.CreatedAt })
            .Concat(awards.Select(x => new { x.Id, Controller = "Awards", Title = x.AwardName, Department = x.Department.Name, x.Status, x.CreatedAt }));
        return View(new PageView<WorkItem>(await items.OrderBy(x => x.CreatedAt).ThenBy(x => x.Controller).ThenBy(x => x.Id).Skip((page-1)*20).Take(20).Select(x=>new WorkItem(x.Id,x.Controller,x.Title,x.Department,x.Status,x.CreatedAt)).ToListAsync(), page, await items.CountAsync()));
    }
}
