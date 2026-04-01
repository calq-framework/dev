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

    public void Dispose() => TestHelper.CleanupDir(_workDir);

    [Fact]
    public async Task New_Classlib_CreatesProjectsAndSolution() {
        string prev = PWD;
        CD(_workDir);

        DevManager dev = new();
        await dev.New("classlib", "CalqFramework.Foo", organization: "calq-framework");

        CD(prev);

        string subDir = Path.Combine(_workDir, "foo");

        // Verify solution exists
        Assert.True(
            File.Exists(Path.Combine(subDir, "CalqFramework.Foo.sln")) ||
            File.Exists(Path.Combine(subDir, "CalqFramework.Foo.slnx")));

        // Verify classlib project
        Assert.True(File.Exists(Path.Combine(subDir, "CalqFramework.Foo", "CalqFramework.Foo.csproj")));

        // Verify test project
        Assert.True(File.Exists(Path.Combine(subDir, "CalqFramework.Foo.Tests", "CalqFramework.Foo.Tests.csproj")));
    }

    [Fact]
    public async Task New_Console_CreatesProjectAndSolution() {
        string prev = PWD;
        CD(_workDir);

        DevManager dev = new();
        await dev.New("console", "CalqFramework.Bar", organization: "calq-framework");

        CD(prev);

        string subDir = Path.Combine(_workDir, "bar");

        Assert.True(
            File.Exists(Path.Combine(subDir, "CalqFramework.Bar.sln")) ||
            File.Exists(Path.Combine(subDir, "CalqFramework.Bar.slnx")));
        Assert.True(File.Exists(Path.Combine(subDir, "CalqFramework.Bar", "CalqFramework.Bar.csproj")));
    }

    [Fact]
    public async Task New_Tool_CreatesAllProjects() {
        string prev = PWD;
        CD(_workDir);

        DevManager dev = new();
        await dev.New("tool", "CalqFramework.Baz", organization: "calq-framework");

        CD(prev);

        string subDir = Path.Combine(_workDir, "baz");

        Assert.True(
            File.Exists(Path.Combine(subDir, "CalqFramework.Baz.sln")) ||
            File.Exists(Path.Combine(subDir, "CalqFramework.Baz.slnx")));
        Assert.True(File.Exists(Path.Combine(subDir, "CalqFramework.Baz", "CalqFramework.Baz.csproj")));
        Assert.True(File.Exists(Path.Combine(subDir, "CalqFramework.Baz.Cli", "CalqFramework.Baz.Cli.csproj")));
        Assert.True(File.Exists(Path.Combine(subDir, "CalqFramework.Baz.Tests", "CalqFramework.Baz.Tests.csproj")));
    }

    [Fact]
    public async Task New_Classlib_InjectsProjXml() {
        string prev = PWD;
        CD(_workDir);

        DevManager dev = new();
        await dev.New("classlib", "CalqFramework.Foo", organization: "calq-framework");

        CD(prev);

        string subDir = Path.Combine(_workDir, "foo");

        string csproj = File.ReadAllText(Path.Combine(subDir, "CalqFramework.Foo", "CalqFramework.Foo.csproj"));
        Assert.Contains("<PackageId>CalqFramework.Foo</PackageId>", csproj);
        Assert.Contains("<Version>0.0.0</Version>", csproj);
        Assert.Contains("<PublishRepositoryUrl>true</PublishRepositoryUrl>", csproj);
    }

    [Fact]
    public async Task New_InvalidType_Throws() {
        string prev = PWD;
        CD(_workDir);

        DevManager dev = new();
        await Assert.ThrowsAsync<InvalidOperationException>(() => dev.New("nonexistent", "CalqFramework.Nope", organization: "calq-framework"));

        CD(prev);
    }

    [Fact]
    public async Task New_NoVisibilityFlag_SkipsGitInit() {
        string prev = PWD;
        CD(_workDir);

        DevManager dev = new();
        await dev.New("console", "CalqFramework.Local", organization: "calq-framework");

        CD(prev);

        string subDir = Path.Combine(_workDir, "local");

        // No .git directory should exist
        Assert.False(Directory.Exists(Path.Combine(subDir, ".git")));
    }
}
