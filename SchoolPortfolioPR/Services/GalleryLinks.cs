namespace SchoolPortfolioPR.Services;

public static class GalleryLinks
{
    public static bool IsValid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 2048
        && !value.Any(char.IsControl) && !value.Contains('\\')
        && Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrWhiteSpace(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);
}
