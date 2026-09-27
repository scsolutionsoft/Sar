using SchoolPortfolioPR.Models;
namespace SchoolPortfolioPR.ViewModels;
public record WorkItem(int Id, string Controller, string Title, string Department, WorkflowStatus Status, DateTime CreatedAt);
public record WorkspaceLink(string Title, string Description, string Url);
public class WorkspaceView
{
    public string Role { get; set; } = "";
    public string Description { get; set; } = "";
    public int Documents { get; set; }
    public int Portfolios { get; set; }
    public int Pending { get; set; }
    public List<WorkspaceLink> Links { get; set; } = [];
}
