namespace CalqFramework.Dev.Config;

/// <summary>
///     Master preset controlling which workflow profile is active.
///     Changing this value and calling ReloadAllAsync() cascades to all subcommand configs.
/// </summary>
public class MasterPreset {
    public string Workflow { get; set; } = "default";
}
