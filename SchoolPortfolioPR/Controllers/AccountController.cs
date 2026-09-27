using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Controllers;
public class AccountController(SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });
    [HttpPost] public async Task<IActionResult> Login(LoginForm f)
    {
        if (!ModelState.IsValid) return View(f);
        var result = await signIn.PasswordSignInAsync(f.Email, f.Password, false, true);
        if (result.Succeeded) {
            var account = await signIn.UserManager.FindByEmailAsync(f.Email);
            var landing = account != null && await signIn.UserManager.IsInRoleAsync(account, "Student") ? "/Admissions" : "/Dashboard";
            return LocalRedirect(Url.IsLocalUrl(f.ReturnUrl) ? f.ReturnUrl! : landing);
        }
        ModelState.AddModelError("", result.IsLockedOut ? "บัญชีถูกระงับชั่วคราว โปรดลองใหม่ภายหลัง" : "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
        return View(f);
    }
    [Authorize, HttpPost] public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    public IActionResult Denied() { Response.StatusCode = 403; return View(); }
}
