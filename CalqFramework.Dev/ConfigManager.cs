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
        CMD($"git -C \"{repo}\" add .config/dev/");
        try {
            CMD($"git -C \"{repo}\" commit -m \"dev config sync\"");
            CMD($"git -C \"{repo}\" push");
        } catch {
            // Nothing to commit
        }
    }

    /// <summary>
    ///     Pulls dotfiles repo and copies config to local config dir.
    /// </summary>
    public void Pull() {
        string repo = DotfilesRepoDir;
        CMD($"git -C \"{repo}\" pull --rebase --quiet");

        string dotfilesConfig = DotfilesConfigDir;
        if (!Directory.Exists(dotfilesConfig)) return;

        foreach (string file in Directory.EnumerateFiles(dotfilesConfig)) {
            File.Copy(file, System.IO.Path.Combine(_config.ConfigDir, System.IO.Path.GetFileName(file)), true);
        }
    }
}
