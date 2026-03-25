using System.Reflection;
using CalqFramework.Config;
using CalqFramework.Config.Json;
using CalqFramework.Dev.Config;

namespace CalqFramework.Dev;

/// <summary>
///     Manages configuration directory and dotfiles sync for codespaces.
/// </summary>
public class ConfigManager {
    private readonly JsonConfigurationRegistry<MasterPreset> _config;

    public ConfigManager(JsonConfigurationRegistry<MasterPreset> config) => _config = config;

    private static string DotfilesConfigDir =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dotfiles", ".config", "dev");

    private static string DotfilesRepoDir =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dotfiles");

    // ── Completion Providers ──

    public IEnumerable<string> CompleteConfigName() =>
        typeof(MasterPreset).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(MasterPreset).Namespace
                      && t.GetCustomAttribute<PresetGroupAttribute>() != null)
            .Select(t => t.Name);

    public IEnumerable<string> CompletePreset() =>
        _config.GetAvailablePresets("Workflow");

    /// <summary>
    ///     Prints the configuration directory path.
    /// </summary>
    public void Path() {
        Console.WriteLine(_config.ConfigDir);
    }

    /// <summary>
    ///     Copies local config to the dotfiles repo and pushes.
    /// </summary>
    public void Push() {
        string dotfilesConfig = DotfilesConfigDir;
        Directory.CreateDirectory(dotfilesConfig);

        foreach (string file in Directory.EnumerateFiles(_config.ConfigDir)) {
            File.Copy(file, System.IO.Path.Combine(dotfilesConfig, System.IO.Path.GetFileName(file)), true);
        }

        string repo = DotfilesRepoDir;
        RUN($"git -C \"{repo}\" add .config/dev/");
        try {
            RUN($"git -C \"{repo}\" commit -m \"dev config sync\"");
            RUN($"git -C \"{repo}\" push");
        } catch {
            // Nothing to commit
        }
    }

    /// <summary>
    ///     Gets or sets the active workflow preset. No argument prints the current preset.
    /// </summary>
    /// <param name="name">Preset name to switch to. Omit to print current.</param>
    public async Task Preset([CliCompletion(nameof(CompletePreset))] string? name = null) {
        MasterPreset master = await _config.GetAsync<MasterPreset>();
        if (name == null) {
            Console.WriteLine(master.Workflow);
            return;
        }

        master.Workflow = name;
        await _config.SaveAsync<MasterPreset>();
        await _config.ReloadAllAsync();
    }

    /// <summary>
    ///     Sets a config value by path. First segment is the config class short name.
    /// </summary>
    /// <param name="configName">Config class name (e.g., NewConfig, FormatConfig, UtilityConfig).</param>
    /// <param name="path">Dot-separated property path (e.g., Organization, ProjXml.classlib.Version).</param>
    /// <param name="value">Value to set.</param>
    public async Task Set([CliCompletion(nameof(CompleteConfigName))] string configName, string path, string value) {
        switch (configName) {
            case "NewConfig":
                await _config.SetByPathAsync<NewConfig>(path, value);
                break;
            case "FormatConfig":
                await _config.SetByPathAsync<FormatConfig>(path, value);
                break;
            case "UtilityConfig":
                await _config.SetByPathAsync<UtilityConfig>(path, value);
                break;
            case "SwitchConfig":
                await _config.SetByPathAsync<SwitchConfig>(path, value);
                break;
            case "PushConfig":
                await _config.SetByPathAsync<PushConfig>(path, value);
                break;
            case "MergeConfig":
                await _config.SetByPathAsync<MergeConfig>(path, value);
                break;
            default:
                throw new InvalidOperationException($"Unknown config: {configName}. Available: NewConfig, FormatConfig, UtilityConfig, SwitchConfig, PushConfig, MergeConfig");
        }
    }

    /// <summary>
    ///     Pulls dotfiles repo and copies config to local config dir.
    /// </summary>
    public void Pull() {
        string repo = DotfilesRepoDir;
        RUN($"git -C \"{repo}\" pull --rebase --quiet");

        string dotfilesConfig = DotfilesConfigDir;
        if (!Directory.Exists(dotfilesConfig)) return;

        foreach (string file in Directory.EnumerateFiles(dotfilesConfig)) {
            File.Copy(file, System.IO.Path.Combine(_config.ConfigDir, System.IO.Path.GetFileName(file)), true);
        }
    }
}
