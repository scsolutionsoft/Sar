using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.ViewModels;

namespace SchoolPortfolioPR.Services;

public class BrandingService(ApplicationDbContext db)
{
    private Task<BrandingView>? current;
    public Task<BrandingView> GetAsync() => current ??= LoadAsync();
    private async Task<BrandingView> LoadAsync() => await db.Branding.AsNoTracking().Where(x => x.Id == 1)
        .Select(x => new BrandingView(x.ProgramName, x.LogoFile != null ? x.LogoFileId : null)).SingleOrDefaultAsync()
        ?? new BrandingView("SchoolPortfolioPR", null);
}
