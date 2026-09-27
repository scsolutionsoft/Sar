using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
[Authorize(Policy = "Admin")]
public class AuditController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int page = 1) { page = Math.Max(1, page); return View(new PageView<AuditLog>(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.Id).Skip((page - 1) * 20).Take(20).ToListAsync(), page, await db.AuditLogs.CountAsync())); }
}
