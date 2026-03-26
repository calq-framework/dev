namespace CalqFramework.Dev;

/// <summary>
///     Stash-apply safety pattern for volatile Git operations.
/// </summary>
public static class GitHelper {
    public static string GetCurrentBranch() => CMD("git rev-parse --abbrev-ref HEAD");

    public static bool HasChanges() => CMD("git diff --stat")
        .Length > 0 || CMD("git diff --cached --stat")
        .Length > 0;

    public static int? ExtractIssueId(string branchName) {
        Match match = Regex.Match(branchName, @"(\d+)$");
        return match.Success ? int.Parse(match.Value) : null;
    }

    /// <summary>
    ///     Executes a Git command with stash-apply safety.
    ///     Stashes dirty state before, restores after.
    /// </summary>
    public static void SafeExecute(string command) {
        bool stashed = false;
        if (HasChanges()) {
            RUN("git stash push --include-untracked --quiet");
            stashed = true;
        }

        try {
            RUN(command);
        } finally {
            if (stashed) {
                try {
                    RUN("git stash apply --quiet");
                    RUN("git stash drop --quiet");
                } catch {
                    Console.Error.WriteLine("Stash apply failed — conflicts detected. Resolve manually, stash is preserved.");
                    throw;
                }
            }
        }
    }
}
