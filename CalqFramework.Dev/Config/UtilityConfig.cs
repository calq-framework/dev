using CalqFramework.Config;

namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for linear subcommands that are pure step lists.
///     Each property is a list of shell commands executed in order.
/// </summary>
[PresetGroup("Workflow")]
public class UtilityConfig {
    public List<string> Pull { get; set; } = new() {
        "git pull --rebase --autostash origin main",
    };

    public List<string> Relock { get; set; } = new() {
        "dotnet restore --no-cache --force-evaluate --use-lock-file",
    };

    public List<string> Issues { get; set; } = new() {
        "gh issue list --limit 20 --search \"is:open no:assignee sort:created-desc\"",
    };

    public string IssueCompletionCommand { get; set; } = "gh issue list --limit 20 --json number --jq \".[].number\"";
}
