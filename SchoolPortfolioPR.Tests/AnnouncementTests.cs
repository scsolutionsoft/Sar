using System.ComponentModel.DataAnnotations;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;

namespace SchoolPortfolioPR.Tests;

public class AnnouncementTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void Schedule_includes_both_boundary_days(int offset, bool expected)
    {
        var start = new DateTime(2026, 9, 26);
        var item = new Announcement { StartDate = start, EndDate = start.AddDays(2), IsEnabled = true };
        Assert.Equal(expected, AnnouncementSchedule.VisibleOn(start.AddDays(offset)).Compile()(item));
    }
    [Fact]
    public void Disabled_and_deleted_posts_are_never_public()
    {
        var today = new DateTime(2026, 9, 26);
        var item = new Announcement { StartDate = today, EndDate = today };
        var visible = AnnouncementSchedule.VisibleOn(today).Compile();
        Assert.False(visible(item)); item.IsEnabled = true; item.IsDeleted = true; Assert.False(visible(item));
    }
    [Fact]
    public void Expiration_uses_Thai_midnight_independent_of_server_timezone()
    {
        var before = new DateTimeOffset(2026, 9, 26, 16, 59, 59, TimeSpan.Zero);
        var after = before.AddSeconds(1);
        Assert.Equal(new DateTime(2026, 9, 26), AnnouncementSchedule.ThaiDate(before));
        Assert.Equal(new DateTime(2026, 9, 27), AnnouncementSchedule.ThaiDate(after));
        var item = new Announcement { StartDate = new(2026, 9, 26), EndDate = new(2026, 9, 26), IsEnabled = true };
        Assert.True(AnnouncementSchedule.VisibleOn(AnnouncementSchedule.ThaiDate(before)).Compile()(item));
        Assert.False(AnnouncementSchedule.VisibleOn(AnnouncementSchedule.ThaiDate(after)).Compile()(item));
    }
    [Fact]
    public void Form_rejects_reverse_dates_and_unknown_card_size()
    {
        var form = new AnnouncementForm { Title = "Test", Summary = "Test", StartDate = new(2026, 9, 26), EndDate = new(2026, 9, 25) };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(form, new ValidationContext(form), errors, true));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(form.EndDate)));
        form.EndDate = form.StartDate; form.Size = (AnnouncementSize)999; errors.Clear();
        Assert.False(Validator.TryValidateObject(form, new ValidationContext(form), errors, true));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(form.Size)));
    }
}
