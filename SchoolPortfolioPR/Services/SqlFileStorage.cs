using System.IO.Compression;
using System.Security.Cryptography;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.Models;
namespace SchoolPortfolioPR.Services;
public interface IFileStorage
{
    Task<FileObject> StoreAsync(IFormFile file, string user, CancellationToken token = default);
    Task<FileObject> StoreGeneratedAsync(byte[] bytes, string name, string user);
}
public class SqlFileStorage(ApplicationDbContext db, IConfiguration config) : IFileStorage
{
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase) {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };
    public async Task<FileObject> StoreAsync(IFormFile file, string user, CancellationToken token = default)
    {
        var name = Path.GetFileName(file.FileName); var ext = Path.GetExtension(name).ToLowerInvariant();
        if (!Types.TryGetValue(ext, out var mime) || file.ContentType != mime) throw new InvalidDataException("ชนิดไฟล์หรือ MIME ไม่ถูกต้อง");
        var max = config.GetValue<long>("Uploads:MaxFileBytes", 10485760);
        if (file.Length <= 0 || file.Length > max || name.Length > 255) throw new InvalidDataException("ไฟล์ต้องไม่ว่างและขนาดไม่เกิน 10 MB");
        using var memory = new MemoryStream(); await file.CopyToAsync(memory, token); var data = memory.ToArray();
        if (!ValidSignature(data, ext)) throw new InvalidDataException("เนื้อหาไฟล์ไม่ตรงกับนามสกุล");
        return Add(data, name, ext, mime, user);
    }
    public Task<FileObject> StoreGeneratedAsync(byte[] bytes, string name, string user) => Task.FromResult(Add(bytes, name, ".pdf", "application/pdf", user));
    private FileObject Add(byte[] data, string name, string ext, string mime, string user)
    {
        var f = new FileObject { OriginalFileName = name, StoredFileName = Guid.NewGuid().ToString("N") + ext, ContentType = mime,
            FileExtension = ext, FileSize = data.LongLength, FileData = data, FileHash = Convert.ToHexString(SHA256.HashData(data)), UploadedBy = user };
        db.Add(f); return f;
    }
    public static bool ValidSignature(byte[] data, string ext)
    {
        var s = data.AsSpan();
        if (s.Length < 12) return false;
        if (ext is ".jpg" or ".jpeg") return s[0] == 255 && s[1] == 216 && s[2] == 255;
        if (ext == ".png") return s[..8].SequenceEqual(new byte[] { 137,80,78,71,13,10,26,10 });
        if (ext == ".webp") return s[..4].SequenceEqual("RIFF"u8) && s[8..12].SequenceEqual("WEBP"u8);
        if (ext == ".pdf") return s[..5].SequenceEqual("%PDF-"u8);
        if (ext is not (".docx" or ".xlsx")) return false;
        try {
            using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);
            if (zip.Entries.Count > 2000 || zip.Entries.Sum(e => e.Length) > 100 * 1024 * 1024 || zip.Entries.Any(e => e.FullName.Contains("vbaProject", StringComparison.OrdinalIgnoreCase))) return false;
            return zip.GetEntry("[Content_Types].xml") != null && zip.GetEntry(ext == ".docx" ? "word/document.xml" : "xl/workbook.xml") != null;
        } catch (InvalidDataException) { return false; }
    }
}
