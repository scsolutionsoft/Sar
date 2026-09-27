namespace SchoolPortfolioPR.ViewModels;

public record HelpStep(string Title, string Instruction, string Url, string Action);
public record HelpTopic(string Id, string Title, string Description, string Icon, IReadOnlyList<HelpStep> Steps);
public record HelpIndexView(string Role, string StorageKey, IReadOnlyList<HelpTopic> Topics);
public record HelpWizardView(string Role, string StorageKey, HelpTopic Topic, int Step);
