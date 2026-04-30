namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for linear subcommands that are pure step lists.
///     Each property is a list of pipeline steps executed in order.
/// </summary>
[PresetGroup("Workflow")]
public class UtilityConfig {
    public List<PipelineStep> Pull { get; set; } = [
        new() {
            Command = "git pull --rebase --autostash origin main"
        }
    ];

    public List<PipelineStep> Relock { get; set; } = [
        new() {
            Command = "dotnet restore {target} --no-cache --force-evaluate --use-lock-file",
            TargetPatterns = [["*.sln", "*.slnx"], ["*.csproj"]]
        }
    ];

    public List<PipelineStep> Issues { get; set; } = [
        new() {
            Command = "gh issue list --limit 20 --search \"is:open no:assignee sort:created-desc\""
        }
    ];

    public string IssueCompletionCommand { get; set; } = "gh issue list --limit 20 --json number --jq \".[].number\"";
}
