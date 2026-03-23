using CalqFramework.Config.Json;
using CalqFramework.Dev.Config;

namespace CalqFramework.Dev.Tests;

/// <summary>
///     Tests the Format subcommand with a minimal .NET project.
/// </summary>
public class DevManagerFormatTest : IDisposable {
    private readonly string _workDir;

    public DevManagerFormatTest() {
        TestHelper.SuppressLogging();
        _workDir = Path.Combine(Path.GetTempPath(), $"dev-test-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
    }

    public void Dispose() {
        TestHelper.CleanupDir(_workDir);
    }

    private static bool IsToolAvailable(string tool) {
        try {
            CMD($"{tool} --version");
            return true;
        } catch (System.ComponentModel.Win32Exception) {
            return false;
        } catch {
            // Tool exists but returned non-zero (e.g., jb doesn't support --version)
            return true;
        }
    }

    [Fact]
    public void IsToolAvailable_NonExistentTool_ReturnsFalse() {
        Assert.False(IsToolAvailable("totallyfaketool_xyz123"));
    }

    [Fact]
    public void IsToolAvailable_ExistingTool_ReturnsTrue() {
        Assert.True(IsToolAvailable("dotnet"));
    }

    [Fact]
    public async Task Format_DotnetOnly_BuildsAndFormats() {
        TestHelper.CreateProject(_workDir, "TestLib");
        string projectDir = Path.Combine(_workDir, "TestLib");

        string prev = PWD;
        CD(_workDir);
        RUN("dotnet restore TestLib");

        var registry = new JsonConfigurationRegistry<MasterPreset>();
        var cfg = await registry.GetAsync<FormatConfig>();
        cfg.Steps = [
            new() { Command = "dotnet build --no-restore {dir}" },
            new() { Command = "dotnet format {target} --verbosity diag --severity info", PerTarget = true },
            new() { Command = "dotnet build --no-restore {dir}" },
        ];
        var dev = new DevManager(registry);
        await dev.Format(new DirectoryInfo(projectDir));

        CD(prev);

        // Verify the project still builds
        CD(_workDir);
        RUN("dotnet build TestLib --no-restore");
        CD(prev);
    }
}
