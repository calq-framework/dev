namespace CalqFramework.Dev.Tests;

/// <summary>
///     Tests the New subcommand with local-only scaffolding (no git, no gh).
/// </summary>
public class DevManagerNewTest : IDisposable {
    private readonly string _workDir;

    public DevManagerNewTest() {
        TestHelper.SuppressLogging();
        _workDir = Path.Combine(Path.GetTempPath(), $"dev-test-new-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
    }

    public void Dispose() {
        TestHelper.CleanupDir(_workDir);
    }

    [Fact]
    public async Task New_Classlib_CreatesProjectsAndSolution() {
        string prev = PWD;
        CD(_workDir);

        var dev = new DevManager();
        await dev.New("classlib", "CalqFramework.Foo");

        CD(prev);

        // Verify solution exists
        Assert.True(File.Exists(Path.Combine(_workDir, "Foo.sln")));

        // Verify classlib project
        Assert.True(File.Exists(Path.Combine(_workDir, "Foo", "Foo.csproj")));

        // Verify test project
        Assert.True(File.Exists(Path.Combine(_workDir, "Foo.Tests", "Foo.Tests.csproj")));
    }

    [Fact]
    public async Task New_Console_CreatesProjectAndSolution() {
        string prev = PWD;
        CD(_workDir);

        var dev = new DevManager();
        await dev.New("console", "CalqFramework.Bar");

        CD(prev);

        Assert.True(File.Exists(Path.Combine(_workDir, "Bar.sln")));
        Assert.True(File.Exists(Path.Combine(_workDir, "Bar", "Bar.csproj")));
    }

    [Fact]
    public async Task New_Tool_CreatesAllProjects() {
        string prev = PWD;
        CD(_workDir);

        var dev = new DevManager();
        await dev.New("tool", "CalqFramework.Baz");

        CD(prev);

        Assert.True(File.Exists(Path.Combine(_workDir, "Baz.sln")));
        Assert.True(File.Exists(Path.Combine(_workDir, "Baz", "Baz.csproj")));
        Assert.True(File.Exists(Path.Combine(_workDir, "Baz.Cli", "Baz.Cli.csproj")));
        Assert.True(File.Exists(Path.Combine(_workDir, "Baz.Tests", "Baz.Tests.csproj")));
    }

    [Fact]
    public async Task New_Classlib_InjectsProjXml() {
        string prev = PWD;
        CD(_workDir);

        var dev = new DevManager();
        await dev.New("classlib", "CalqFramework.Foo");

        CD(prev);

        string csproj = File.ReadAllText(Path.Combine(_workDir, "Foo", "Foo.csproj"));
        Assert.Contains("<PackageId>CalqFramework.Foo</PackageId>", csproj);
        Assert.Contains("<Version>0.0.0</Version>", csproj);
        Assert.Contains("<PublishRepositoryUrl>true</PublishRepositoryUrl>", csproj);
    }

    [Fact]
    public async Task New_InvalidType_Throws() {
        string prev = PWD;
        CD(_workDir);

        var dev = new DevManager();
        await Assert.ThrowsAsync<InvalidOperationException>(() => dev.New("nonexistent", "CalqFramework.Nope"));

        CD(prev);
    }

    [Fact]
    public async Task New_NoVisibilityFlag_SkipsGitInit() {
        string prev = PWD;
        CD(_workDir);

        var dev = new DevManager();
        await dev.New("console", "CalqFramework.Local");

        CD(prev);

        // No .git directory should exist
        Assert.False(Directory.Exists(Path.Combine(_workDir, ".git")));
    }
}
