namespace CalqFramework.Dev.Config;

/// <summary>
///     A single step in a command pipeline.
///     Supports optional file-pattern guards and configurable target discovery.
/// </summary>
public class PipelineStep {
    /// <summary>
    ///     Shell command to execute. Supports {dir} and {target} placeholders.
    /// </summary>
    public string Command { get; set; } = "";

    /// <summary>
    ///     If set, this step only runs when files matching this glob exist in the target directory.
    /// </summary>
    public string? FilePattern { get; set; }

    /// <summary>
    ///     Ordered list of glob-pattern groups for target discovery.
    ///     Each group is a list of globs. Groups are tried in priority order:
    ///     the first group that yields exactly one file wins.
    ///     If no group yields exactly one, all results from all groups are combined.
    ///     When null or empty, the command runs once with {dir} only (no {target} substitution).
    /// </summary>
    public List<List<string>>? TargetPatterns { get; set; }
}
