namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for the Format command. Steps define the full pipeline as data.
/// </summary>
[PresetGroup("Workflow")]
public class FormatConfig {
    public List<PipelineStep> Steps { get; set; } = [
        new() {
            Command = "dotnet build --no-restore {target}",
            TargetPatterns = [["*.sln", "*.slnx"], ["*.*proj"]]
        },
        new() {
            Command = "jb cleanupcode {dir} --profile=\"Built-in: Full Cleanup\"",
            FilePattern = "*.csproj"
        },
        new() {
            Command = "fantomas {dir}",
            FilePattern = "*.fsproj"
        },
        new() {
            Command = "dotnet format {target} --verbosity diag --severity info",
            TargetPatterns = [["*.sln", "*.slnx"], ["*.*proj"]]
        },
        new() {
            Command = "dotnet build --no-restore {target}",
            TargetPatterns = [["*.sln", "*.slnx"], ["*.*proj"]]
        }
    ];
}
