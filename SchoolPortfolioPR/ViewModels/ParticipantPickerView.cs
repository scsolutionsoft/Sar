using System.Text.RegularExpressions;
namespace SchoolPortfolioPR.ViewModels;

public record ParticipantChoice(int Id, string Name, string Code, string GroupKey, string GroupName, string Level, string Room);
public record ParticipantPickerView(string FieldName, string Label, bool IsTeacher, IReadOnlyList<ParticipantChoice> Choices, int[] SelectedIds);

public static class StudentPlacement
{
    public static (string Level, string Room) Parse(string? classroom)
    {
        var value = (classroom ?? "").Trim().Replace('／', '/');
        for (var i=0;i<10;i++) value=value.Replace((char)('๐'+i),(char)('0'+i));
        var parts = Regex.Split(value, @"\s*(?:/|ห้อง)\s*", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        var level = Regex.Replace(parts[0].Trim(), @"\s+", "", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        var room = parts.Length>1 ? string.Join("/",parts.Skip(1)).Trim() : "";
        return (string.IsNullOrEmpty(level)?"ไม่ระบุระดับชั้น":level, string.IsNullOrEmpty(room)?"ไม่ระบุห้อง":room);
    }
}
