namespace CalqFramework.Dev.Tests;

/// <summary>
///     Tests the Pull subcommand using local bare repos.
/// </summary>
public class DevManagerPullTest : IDisposable {
    private readonly string _bareRepo;
    private readonly string _workDir;

    public DevManagerPullTest() {
        TestHelper.SuppressLogging();
        _bareRepo = TestHelper.CreateBareRepo();
        _workDir = TestHelper.CreateWorkingRepo(_bareRepo);
    }

    public void Dispose() {
        TestHelper.CleanupDir(_bareRepo);
        TestHelper.CleanupDir(_workDir);
    }

    [Fact]
    public async Task Pull_OnMain_PullsFromOrigin() {
        // Setup: create a file, commit, push
        File.WriteAllText(Path.Combine(_workDir, "file.txt"), "hello");
        TestHelper.CommitAndPush(_workDir, "initial");

        // Create a second clone, make a change, push
        string workDir2 = TestHelper.CreateWorkingRepo(_bareRepo);
        try {
            string prev = PWD;
            CD(workDir2);
            CMD("git pull origin main");
            File.WriteAllText(Path.Combine(workDir2, "file2.txt"), "world");
            CMD("git add -A");
            CMD("git commit -m \"second commit\"");
            CMD("git push origin main");
            CD(prev);

            // Pull from the first working dir
            CD(_workDir);
            DevManager dev = new();
            await dev.Pull();
            CD(prev);

            // Verify the file from the second clone is now present
            Assert.True(File.Exists(Path.Combine(_workDir, "file2.txt")));
        } finally {
            TestHelper.CleanupDir(workDir2);
        }
    }

    [Fact]
    public async Task Pull_OnFeatureBranch_RebasesOntoMain() {
        // Setup: initial commit on main
        File.WriteAllText(Path.Combine(_workDir, "file.txt"), "hello");
        TestHelper.CommitAndPush(_workDir, "initial");

        string prev = PWD;
        CD(_workDir);

        // Create feature branch with a commit
        CMD("git switch -c issues/1");
        File.WriteAllText(Path.Combine(_workDir, "feature.txt"), "feature");
        CMD("git add -A");
        CMD("git commit -m \"feature commit\"");

        // Simulate remote main advancing (via second clone)
        string workDir2 = TestHelper.CreateWorkingRepo(_bareRepo);
        try {
            CD(workDir2);
            CMD("git pull origin main");
            File.WriteAllText(Path.Combine(workDir2, "main-advance.txt"), "advanced");
            CMD("git add -A");
            CMD("git commit -m \"main advanced\"");
            CMD("git push origin main");
            CD(_workDir);

            // Pull while on feature branch — should rebase onto updated main
            DevManager dev = new();
            await dev.Pull();

            // Verify linear history (rebase, not merge)
            string log = CMD("git log --oneline");
            Assert.DoesNotContain("Merge", log);
        } finally {
            CD(prev);
            TestHelper.CleanupDir(workDir2);
        }
    }
}
