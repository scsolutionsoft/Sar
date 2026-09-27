using System.Text;
using System.Text.Encodings.Web;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Playwright;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Services;
public class ExportService
{
    private static readonly SemaphoreSlim PdfGate = new(2);
    public static string Escape(string? s) => HtmlEncoder.Default.Encode(s ?? "");
    public const string Style = "body{font-family:Tahoma,Arial,sans-serif;color:#15324a;font-size:12px;line-height:1.7}h1{font-size:26px}h2{font-size:20px;color:#146d78}table{width:100%;border-collapse:collapse}td,th{border:1px solid #cfdbe2;padding:8px;text-align:left}th{background:#edf5f7}section{break-before:page}.cover{min-height:900px;display:flex;flex-direction:column;justify-content:center;background:#edf5f7;padding:35px}img{max-width:100%;max-height:480px;object-fit:contain}.gallery{display:flex;flex-wrap:wrap;gap:12px}.gallery img{width:45%;height:200px}p{white-space:pre-wrap}tr{break-inside:avoid}";
    public static string Document(string title, string body) => $"<!doctype html><html lang='th'><head><meta charset='utf-8'><title>{Escape(title)}</title><style>{Style}</style></head><body>{body}</body></html>";
    public string ReportHtml(string title, IReadOnlyList<ReportRow> rows)
    {
        var b = new StringBuilder($"<h1>{Escape(title)}</h1><p>จำนวน {rows.Count} รายการ · ออกรายงาน {Thai.Date(DateTime.Today)}</p><table><thead><tr><th>วันที่</th><th>ประเภท</th><th>รายการ</th><th>ฝ่าย</th><th>หมวดหมู่ / ระดับ</th><th>ผู้ร่วม</th></tr></thead><tbody>");
        foreach (var r in rows) b.Append($"<tr><td>{Thai.Date(r.Date)}</td><td>{Escape(r.Type)}</td><td>{Escape(r.Title)}</td><td>{Escape(r.Department)}</td><td>{Escape(r.Category)}</td><td>{Escape(r.Participants)}</td></tr>");
        return Document(title, b.Append("</tbody></table>").ToString());
    }
    public async Task<byte[]> PdfAsync(string html, bool showPageNumbers = true)
    {
        await PdfGate.WaitAsync();
        try {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();
            await page.RouteAsync("**/*", route => route.AbortAsync());
            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.Load });
            await page.EvaluateAsync("document.fonts.ready");
            return await page.PdfAsync(new() { Format = "A4", PrintBackground = true, Outline = true, Tagged = true, Margin = new() { Top = "15mm", Bottom = "18mm", Left = "12mm", Right = "12mm" }, DisplayHeaderFooter = showPageNumbers, HeaderTemplate = "<span></span>", FooterTemplate = "<div style='font-size:9px;width:100%;text-align:center'><span class='pageNumber'></span> / <span class='totalPages'></span></div>" });
        } finally { PdfGate.Release(); }
    }
    public byte[] Excel(string title, IReadOnlyList<ReportRow> rows)
    {
        using var book = new XLWorkbook(); var s = book.AddWorksheet("รายงาน");
        string[] headers = ["วันที่", "ประเภท", "รายการ", "ฝ่าย", "หมวดหมู่ / ระดับ", "ผู้ร่วม"];
        for (int c = 0; c < headers.Length; c++) s.Cell(1, c + 1).Value = headers[c];
        for (int i = 0; i < rows.Count; i++) { var r = rows[i]; string[] values = [Thai.Date(r.Date), r.Type, r.Title, r.Department, r.Category, r.Participants]; for (int c = 0; c < values.Length; c++) s.Cell(i + 2, c + 1).Value = values[c]; }
        s.Range(1, 1, rows.Count + 1, 6).CreateTable(); s.Columns().AdjustToContents(10, 60); s.SheetView.FreezeRows(1);
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
    public byte[] Word(string title, IReadOnlyList<ReportRow> rows)
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document)) {
            var part = doc.AddMainDocumentPart(); part.Document = new Document(new Body()); var body = part.Document.Body!;
            body.Append(new Paragraph(new Run(new Text(title)))); var table = new Table();
            table.Append(new TableProperties(new TableBorders(new TopBorder { Val = BorderValues.Single }, new BottomBorder { Val = BorderValues.Single }, new InsideHorizontalBorder { Val = BorderValues.Single }, new InsideVerticalBorder { Val = BorderValues.Single })));
            void AddRow(string[] values) => table.Append(new TableRow(values.Select(v => new TableCell(new Paragraph(new Run(new Text(v) { Space = SpaceProcessingModeValues.Preserve }))))));
            AddRow(["วันที่", "ประเภท", "รายการ", "ฝ่าย", "หมวดหมู่", "ผู้ร่วม"]);
            foreach (var r in rows) AddRow([Thai.Date(r.Date), r.Type, r.Title, r.Department, r.Category, r.Participants]);
            body.Append(table); part.Document.Save();
        }
        return stream.ToArray();
    }
}
