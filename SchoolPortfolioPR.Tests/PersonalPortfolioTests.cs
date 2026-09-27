using System.ComponentModel.DataAnnotations;
using PdfSharp.Pdf;
using SchoolPortfolioPR.Models;
using SchoolPortfolioPR.Services;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Tests;

public class PersonalPortfolioTests
{
    private static byte[] Pdf(int pages,bool encrypted=false)
    {
        using var p=new PdfDocument();for(var i=0;i<pages;i++)p.AddPage();
        if(encrypted){p.SecuritySettings.UserPassword="TestUser!12345";p.SecuritySettings.OwnerPassword="TestOwner!12345";}
        using var stream=new MemoryStream();p.Save(stream,false);return stream.ToArray();
    }
    [Fact] public void Pdf_upload_rejects_fake_or_encrypted_documents()
    {
        Assert.Throws<InvalidDataException>(()=>PersonalPdfService.PageCount("%PDF-not-a-document"u8.ToArray()));
        Assert.Throws<InvalidDataException>(()=>PersonalPdfService.PageCount(Pdf(1,true)));
        Assert.Equal(2,PersonalPdfService.PageCount(Pdf(2)));
    }
    [Fact] public void Export_appends_all_evidence_pages_and_enforces_actual_page_limit()
    {
        var evidence=new List<(string,byte[])>{("Transcript",Pdf(2)),("Certificate",Pdf(1))};
        var result=PersonalPdfService.Combine(Pdf(3),evidence,6,5);
        Assert.Equal(6,result.Pages);Assert.Equal(6,PersonalPdfService.PageCount(result.Bytes));
        Assert.Throws<InvalidDataException>(()=>PersonalPdfService.Combine(Pdf(3),evidence,5,5));
    }
    [Theory][InlineData("javascript:alert(1)",false)][InlineData("data:text/html,test",false)][InlineData("https://example.com/work",true)][InlineData("",true)]
    public void Portfolio_links_allow_only_web_urls(string value,bool expected)=>Assert.Equal(expected,PersonalInput.SafeLink(value));
    [Fact] public void Application_form_validates_grades_limits_and_duplicate_evidence()
    {
        var f=new AdmissionForm{FullName="Test",School="Test",Gpax=5,MaxPages=0};var results=new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(f,new ValidationContext(f),results,true));
        f.Gpax=3.5m;f.MaxPages=10;f.DocumentIds=[1,1];results.Clear();
        Assert.False(Validator.TryValidateObject(f,new ValidationContext(f),results,true));
    }
    [Fact] public void Checklist_requires_role_and_reflection_not_only_an_entry_title()
    {
        var p=new AdmissionPortfolio();p.Entries.Add(new PortfolioEntry{Title="Project"});
        Assert.False(AdmissionPortfolioService.Checklist(p).Single(x=>x.Label.StartsWith("ผลงานพร้อม")).Complete);
        p.Entries.First().Role="Researcher";p.Entries.First().Reflection="Learned to test assumptions";
        Assert.True(AdmissionPortfolioService.Checklist(p).Single(x=>x.Label.StartsWith("ผลงานพร้อม")).Complete);
    }
}
