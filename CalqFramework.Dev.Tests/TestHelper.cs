namespace CalqFramework.Dev.Tests;

public static class TestHelper {
    public static string CreateBareRepo() {
        string path = Path.Combine(Path.GetTempPath(), $"dev-test-bare-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        string prev = PWD;
        CD(path);
        CMD("git init --bare");
        CD(prev);
        return path;
    }

    public static string CreateWorkingRepo(string bareRepoPath) {
        string path = Path.Combine(Path.GetTempPath(), $"dev-test-work-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        string prev = PWD;
        CD(path);
        CMD("git init -b main");
        CMD("git config user.email \"test@test.com\"");
        CMD("git config user.name \"Test\"");
        CMD($"git remote add origin \"{bareRepoPath}\"");
        CD(prev);
        return path;
    }

    public static string CreateProject(string parentDir, string projectName) {
        string projectDir = Path.Combine(parentDir, projectName);
        Directory.CreateDirectory(projectDir);
        string csprojPath = Path.Combine(projectDir, $"{projectName}.csproj");
        File.WriteAllText(csprojPath, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Version>0.0.0</Version>
  </PropertyGroup>
</Project>");
        File.WriteAllText(Path.Combine(projectDir, "Class1.cs"), $"namespace {projectName}; public class Class1 {{ }}");
        return csprojPath;
    }

    public static void CommitAndPush(string workingDir, string message = "commit") {
        string prev = PWD;
        CD(workingDir);
        CMD("git add -A");
        CMD($"git commit -m \"{message}\" --allow-empty");
        try {
            CMD("git push origin main");
        } catch {
            CMD("git push -u origin main");
        }

        CD(prev);
    }

    public static void SuppressLogging() {
        LocalTerminal.TerminalLogger = new NullTerminalLogger();
        LocalTerminal.Out = Stream.Null;
    }

    public static void CleanupDir(string path) {
        try {
            if (Directory.Exists(path)) {
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, true);
            }
        } catch {
            // Best effort
        }
    }
}
