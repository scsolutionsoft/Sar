using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Tests;
public class ParticipantPickerTests
{
    [Theory]
    [InlineData("ม.6/1","ม.6","1")]
    [InlineData(" ป. ๓ / ๒ ","ป.3","2")]
    [InlineData("ม.4 ห้อง 3","ม.4","3")]
    [InlineData("อ.2／1","อ.2","1")]
    [InlineData("ม.5","ม.5","ไม่ระบุห้อง")]
    [InlineData(null,"ไม่ระบุระดับชั้น","ไม่ระบุห้อง")]
    [InlineData(" / 1","ไม่ระบุระดับชั้น","1")]
    public void Existing_classroom_formats_remain_filterable(string? input,string level,string room)
    {
        Assert.Equal((level,room),StudentPlacement.Parse(input));
    }
}
