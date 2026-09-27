using System.Text.RegularExpressions;

namespace SchoolPortfolioPR.Services;

public static class ActivitySlug
{
    public static string Create(string activityCode)
    {
        var prefix = Regex.Replace(activityCode.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (prefix.Length == 0) prefix = "news";
        if (prefix.Length > 60) prefix = prefix[..60].TrimEnd('-');
        return prefix + "-" + Guid.NewGuid().ToString("N");
    }
}
