using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "portfolio")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o => {
    o.Password.RequiredLength = 12; o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); o.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
// Role changes made by the administrator take effect on the next authenticated request.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(o => {
    o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied";
    o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddAuthorization(o => {
    o.AddPolicy("StudentPortfolio", p => p.RequireAuthenticatedUser().RequireAssertion(c => c.User.IsInRole("Student") && !Roles.Editors.Any(c.User.IsInRole)));
    o.AddPolicy("Editor", p => p.RequireRole(Roles.Editors));
    o.AddPolicy("Admin", p => p.RequireRole(Roles.Admins));
    o.AddPolicy("Publisher", p => p.RequireRole("SuperAdmin", "Administrator", "PRAdmin"));
});
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 55 * 1024 * 1024);
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<BrandingService>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<HelpService>();
builder.Services.AddScoped<IFileStorage, SqlFileStorage>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<PublicationService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AnnouncementSchedule>();
builder.Services.AddScoped<AdmissionPortfolioService>();
var app = builder.Build();
if (args.Contains("--seed-test-users"))
{
    using var scope = app.Services.CreateScope();
    await TestAccountSeeder.SeedAsync(scope.ServiceProvider, app.Environment);
    return;
}
if (args.Contains("--check-db"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.OpenConnectionAsync();
    await using var cmd = db.Database.GetDbConnection().CreateCommand();
    cmd.CommandText = "SELECT DB_NAME(), (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('portfolio'))";
    await using var reader = await cmd.ExecuteReaderAsync(); await reader.ReadAsync();
    Console.WriteLine($"Connected to {reader.GetString(0)}; portfolio tables: {reader.GetInt32(1)}"); return;
}
if (args.Contains("--initialize"))
{
    using var scope = app.Services.CreateScope();
    await DbInitializer.InitializeAsync(scope.ServiceProvider, app.Configuration); return;
}
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) => {
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self'; connect-src 'self'; worker-src 'self' blob:; frame-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'";
    await next();
});
// HTML date inputs submit Gregorian ISO dates even on Thai-language Windows servers.
// Keep binding/formatting stable; display Buddhist dates explicitly through Thai.Date.
app.UseRequestLocalization(new RequestLocalizationOptions {
    DefaultRequestCulture = new RequestCulture("en-US", "th-TH"),
    SupportedCultures = [new CultureInfo("en-US")], SupportedUICultures = [new CultureInfo("th-TH")],
    RequestCultureProviders = []
});
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}"); app.Run();
