using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
[Authorize(Roles = "SuperAdmin")]
public class UsersController(ApplicationDbContext db, AccessService access, UserManager<ApplicationUser> users) : EditorController(db, access)
{
    public async Task<IActionResult> Index()
    {
        var roles = await (from link in Db.UserRoles join role in Db.Roles on link.RoleId equals role.Id select new { link.UserId, role.Name }).ToListAsync();
        ViewBag.UserRoles = roles.GroupBy(x=>x.UserId).ToDictionary(x=>x.Key,x=>string.Join(", ",x.Select(r=>Roles.Labels.GetValueOrDefault(r.Name!,r.Name!))));
        return View(await users.Users.AsNoTracking().Include(x=>x.Department).Include(x=>x.Student).OrderBy(x=>x.DisplayName).ToListAsync());
    }
    public async Task<IActionResult> Edit(string? id)
    {
        await LookupsAsync(); if (id == null) return View(new UserForm());
        var u = await users.FindByIdAsync(id); if (u == null) return NotFound();
        return View(new UserForm { Id = u.Id, Email = u.Email!, DisplayName = u.DisplayName, DepartmentId = u.DepartmentId, StudentId = u.StudentId, Role = (await users.GetRolesAsync(u)).FirstOrDefault() ?? "Viewer" });
    }
    [HttpPost] public async Task<IActionResult> Edit(UserForm f)
    {
        if (!Roles.All.Contains(f.Role)) ModelState.AddModelError("Role", "บทบาทไม่ถูกต้อง");
        if (f.Role == "Student" && (f.StudentId == null || !await Db.Set<Student>().AnyAsync(x => x.Id == f.StudentId))) ModelState.AddModelError("StudentId", "กรุณาเลือกนักเรียนที่มีในระบบ");
        if (f.Role == "Student" && await Db.Users.AnyAsync(x => x.Id != f.Id && x.StudentId == f.StudentId)) ModelState.AddModelError("StudentId", "นักเรียนนี้มีบัญชีแล้ว");
        if (f.Id == null && string.IsNullOrEmpty(f.Password)) ModelState.AddModelError("Password", "กรุณาระบุรหัสผ่าน");
        if (f.Id == Access.UserId && f.Role != "SuperAdmin") ModelState.AddModelError("Role", "ไม่สามารถลดสิทธิ์บัญชีของตนเอง");
        if (f.DepartmentId != null && !await Db.Set<Department>().AnyAsync(x => x.Id == f.DepartmentId)) ModelState.AddModelError("DepartmentId", "ไม่พบฝ่าย");
        if (f.Role is "Teacher" or "DepartmentAdmin" && f.DepartmentId == null) ModelState.AddModelError("DepartmentId", "กรุณาเลือกฝ่าย");
        if (!ModelState.IsValid) { await LookupsAsync(); return View(f); }
        await using var tx = await Db.Database.BeginTransactionAsync();
        var u = f.Id == null ? new ApplicationUser { UserName = f.Email, Email = f.Email, EmailConfirmed = true } : await users.FindByIdAsync(f.Id);
        if (u == null) return NotFound(); u.DisplayName = f.DisplayName; u.DepartmentId = f.DepartmentId; u.StudentId = f.Role == "Student" ? f.StudentId : null;
        var result = f.Id == null ? await users.CreateAsync(u, f.Password!) : await users.UpdateAsync(u);
        if (result.Succeeded && f.Id != null && !string.IsNullOrWhiteSpace(f.Password)) result = await users.ResetPasswordAsync(u, await users.GeneratePasswordResetTokenAsync(u), f.Password);
        if (result.Succeeded) result = await users.SetEmailAsync(u, f.Email);
        if (result.Succeeded) result = await users.SetUserNameAsync(u, f.Email);
        if (result.Succeeded) result = await users.RemoveFromRolesAsync(u, await users.GetRolesAsync(u));
        if (result.Succeeded) result = await users.AddToRoleAsync(u, f.Role);
        if (result.Succeeded) result = await users.UpdateSecurityStampAsync(u);
        if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); await tx.RollbackAsync(); await LookupsAsync(); return View(f); }
        Db.AuditLogs.Add(new AuditLog { User = Access.UserId, Action = "ManageUser", Entity = "ApplicationUser", EntityId = u.Id, NewValue = System.Text.Json.JsonSerializer.Serialize(new { f.Email, f.Role, f.DepartmentId }), IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
        await Db.SaveChangesAsync(); await tx.CommitAsync(); return RedirectToAction(nameof(Index));
    }
}
