namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for the Merge command.
/// </summary>
[PresetGroup("Workflow")]
public class MergeConfig {
    public string MergeStrategy { get; set; } = "squash";
    public bool DeleteBranch { get; set; } = true;
    public bool CloseIssue { get; set; } = true;
    public bool PullAfterMerge { get; set; } = true;
}
