using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
namespace SchoolPortfolioPR.Data;
public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role))
            {
                var result = await roles.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded) throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));
            }
    }
}
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await RoleSeeder.SeedAsync(services.GetRequiredService<RoleManager<IdentityRole>>());
        if (!await db.Set<Department>().AnyAsync()) db.Add(new Department { Name = "ส่วนกลาง" });
        if (!await db.Set<ActivityCategory>().AnyAsync()) db.AddRange(new ActivityCategory { Name = "วิชาการ" }, new ActivityCategory { Name = "กิจกรรมนักเรียน" }, new ActivityCategory { Name = "ข่าวโรงเรียน" });
        if (!await db.Set<AcademicYear>().AnyAsync()) db.Add(new AcademicYear { Name = (DateTime.Today.Year + 543).ToString(), StartDate = new(DateTime.Today.Year, 5, 1), EndDate = new(DateTime.Today.Year + 1, 4, 30) });
        if (!await db.Set<SchoolProfile>().AnyAsync()) db.Add(new SchoolProfile { Name = "SchoolPortfolioPR" });
        await db.SaveChangesAsync();
        var email = config["SeedAdmin:Email"]; var password = config["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Schema and roles ready. Set SeedAdmin:Email and SeedAdmin:Password, then run --initialize to create SuperAdmin."); return;
        }
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "ผู้ดูแลระบบ", DepartmentId = await db.Set<Department>().Select(d => d.Id).FirstAsync() };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            var roleResult = await users.AddToRoleAsync(user, "SuperAdmin");
            if (!roleResult.Succeeded) throw new InvalidOperationException("Unable to assign SuperAdmin role.");
            Console.WriteLine("SuperAdmin created from configuration.");
        }
        else Console.WriteLine("Configured account already exists; credentials and roles unchanged.");
    }
}
