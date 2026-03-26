namespace CalqFramework.Dev.Config;

/// <summary>
///     A single step in the Format pipeline.
/// </summary>
public class FormatStep {
    /// <summary>
    ///     Shell command to execute. Supports {dir} and {target} placeholders.
    /// </summary>
    public string Command { get; set; } = "";

    /// <summary>
    ///     If set, this step only runs when files matching this glob exist in the target directory.
    /// </summary>
    public string? FilePattern { get; set; }

    /// <summary>
    ///     If true, the command runs once per discovered target (.sln, *.*proj),
    ///     substituting {target}. Otherwise runs once with {dir}.
    /// </summary>
    public bool PerTarget { get; set; }
}
