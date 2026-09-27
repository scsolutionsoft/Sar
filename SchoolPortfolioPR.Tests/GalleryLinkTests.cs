using SchoolPortfolioPR.Services;

namespace SchoolPortfolioPR.Tests;
public class GalleryLinkTests
{
    [Theory]
    [InlineData("https://drive.google.com/drive/folders/test?usp=sharing", true)]
    [InlineData("https://1drv.ms/a/test", true)]
    [InlineData("http://example.com/photos", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,test", false)]
    [InlineData("//example.com/test", false)]
    [InlineData("file:///C:/photos/test.png", false)]
    [InlineData("https://name:password@example.com/", false)]
    [InlineData("https://example.com/\nphotos", false)]
    [InlineData("https://example.com\\photos", false)]
    [InlineData("not a URL", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Cloud_links_only_allow_explicit_web_addresses(string? url, bool expected)
        => Assert.Equal(expected, GalleryLinks.IsValid(url));
}
