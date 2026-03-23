using CalqFramework.Config;

namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for the Switch command.
/// </summary>
[PresetGroup("Workflow")]
public class SwitchConfig {
    public string BranchPrefix { get; set; } = "issues/";
    public bool AutoCreateIssue { get; set; } = true;
}
