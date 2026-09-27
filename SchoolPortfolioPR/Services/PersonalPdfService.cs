using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
namespace SchoolPortfolioPR.Services;

public static class PersonalPdfService
{
    public static int PageCount(byte[] bytes)
    {
        try {
            using var stream = new MemoryStream(bytes);
            using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            if (pdf.SecuritySettings.IsEncrypted || pdf.PageCount is < 1 or > 100) throw new InvalidDataException();
            return pdf.PageCount;
        } catch (Exception ex) when (ex is not OutOfMemoryException) { throw new InvalidDataException("PDF ต้องอ่านได้ ไม่ติดรหัสผ่าน และมี 1–100 หน้า"); }
    }
    public static (byte[] Bytes, int Pages) Combine(byte[] body, IReadOnlyList<(string Title, byte[] Bytes)> evidence, int maxPages, int maxMegabytes)
    {
        using var output = new PdfDocument();
        void Add(string title, byte[] bytes)
        {
            PageCount(bytes);
            using var stream = new MemoryStream(bytes); using var input = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            var first = output.PageCount;
            foreach (var page in input.Pages) output.AddPage(page);
            output.Outlines.Add(title, output.Pages[first]);
        }
        Add("แฟ้มสะสมผลงาน", body);
        foreach (var item in evidence) Add(item.Title, item.Bytes);
        if (output.PageCount > maxPages) throw new InvalidDataException($"แฟ้มมี {output.PageCount} หน้า เกินที่กำหนด {maxPages} หน้า กรุณาลดผลงาน/หลักฐาน หรือแก้จำนวนหน้าตามประกาศรับสมัคร");
        var pages = output.PageCount;
        using var result = new MemoryStream(); output.Save(result, false); var bytes = result.ToArray();
        if (bytes.Length > maxMegabytes * 1024L * 1024L) throw new InvalidDataException($"ไฟล์มีขนาด {bytes.Length / 1048576d:N1} MB เกินที่กำหนด {maxMegabytes} MB กรุณาลดขนาดภาพหรือหลักฐาน");
        return (bytes, pages);
    }
}
