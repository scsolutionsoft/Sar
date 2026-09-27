using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

namespace SchoolPortfolioPR.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HelpController(HelpService help) : Controller
{
    public async Task<IActionResult> Index() => View(new HelpIndexView(help.Role, help.StorageKey, await help.TopicsAsync()));
    public async Task<IActionResult> Wizard(string topic, int step = 1)
    {
        if (!ModelState.IsValid) return BadRequest();
        var item = (await help.TopicsAsync()).SingleOrDefault(x => x.Id == topic);
        if (item == null) return NotFound();
        if (step < 1 || step > item.Steps.Count) return BadRequest();
        return View(new HelpWizardView(help.Role, help.StorageKey, item, step));
    }
}
