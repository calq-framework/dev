using CalqFramework.Config;

namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for the Format command. Steps define the full pipeline as data.
/// </summary>
[PresetGroup("Workflow")]
public class FormatConfig {
    public List<FormatStep> Steps { get; set; } = new() {
        new() { Command = "dotnet build --no-restore {dir}" },
        new() { Command = "jb cleanupcode {dir} --profile=\"Built-in: Full Cleanup\"", FilePattern = "*.csproj" },
        new() { Command = "fantomas {dir}", FilePattern = "*.fsproj" },
        new() { Command = "dotnet format {target} --verbosity diag --severity info", PerTarget = true },
        new() { Command = "dotnet build --no-restore {dir}" },
    };
}
