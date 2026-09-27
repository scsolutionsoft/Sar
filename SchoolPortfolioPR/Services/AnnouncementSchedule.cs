using System.Linq.Expressions;
using SchoolPortfolioPR.Models;

namespace SchoolPortfolioPR.Services;

public class AnnouncementSchedule(TimeProvider clock)
{
    public static DateTime ThaiDate(DateTimeOffset instant) => instant.ToOffset(TimeSpan.FromHours(7)).Date;
    public DateTime Today => ThaiDate(clock.GetUtcNow());
    // Inclusive calendar dates: a post remains visible until midnight after its end date in Thailand.
    public static Expression<Func<Announcement, bool>> VisibleOn(DateTime date) =>
        x => !x.IsDeleted && x.IsEnabled && x.StartDate <= date && x.EndDate >= date;
    public static string Status(Announcement item, DateTime today) => !item.IsEnabled ? "ปิดแสดง" :
        item.StartDate > today ? "รอแสดง" : item.EndDate < today ? "หมดอายุ" : "กำลังแสดง";
}
