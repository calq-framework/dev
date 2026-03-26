namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for the Push command.
/// </summary>
[PresetGroup("Workflow")]
public class PushConfig {
    public string Remote { get; set; } = "origin";
    public string MainBranch { get; set; } = "main";
    public bool ForceWithLeaseOnFeature { get; set; } = true;
    public bool CreatePr { get; set; } = true;
    public string PrTitleFormat { get; set; } = "(#{IssueID}) {IssueTitle}";
}
