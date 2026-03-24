using CalqFramework.Config;
using CalqFramework.Config.Json;
using CalqFramework.Dev.Config;

namespace CalqFramework.Dev;

/// <summary>
///     Configurable developer workflow orchestrator.
///     All subcommand behavior is driven by CalqFramework.Config preset POCOs.
/// </summary>
public class DevManager {
    private readonly JsonConfigurationRegistry<MasterPreset> _config;

    public DevManager() : this(new()) { }
    public DevManager(JsonConfigurationRegistry<MasterPreset> config) {
        _config = config;
        Config = new ConfigManager(config);
    }

    // ── Preflight ──

    private static void RequireTool(string tool) {
        try {
            CMD($"{tool} --version");
        } catch (System.ComponentModel.Win32Exception) {
            throw new InvalidOperationException($"Required tool '{tool}' not found on PATH.");
        } catch {
            // Tool exists but returned non-zero (e.g., jb doesn't support --version)  Ethat's fine
        }
    }

    private static void RequireTools(IEnumerable<string> commands) {
        HashSet<string> tools = new();
        foreach (string cmd in commands) {
            string? tool = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (tool != null) {
                tools.Add(tool);
            }
        }

        foreach (string tool in tools) {
            RequireTool(tool);
        }
    }

    // ── Completion Providers ──

    public IEnumerable<string> CompleteProjectType() =>
        _config.GetAsync<NewConfig>().Result.ProjectTypes.Keys;

    public IEnumerable<string> CompleteIssues() {
        try {
            string command = _config.GetAsync<UtilityConfig>().Result.IssueCompletionCommand;
            string output = CMD(command);
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        } catch {
            return [];
        }
    }

    // ── Submodules ──

    /// <summary>
    ///     Configuration management (path, push, pull).
    /// </summary>
    public ConfigManager Config { get; }

    // ── Subcommands ──

    /// <summary>
    ///     Bootstraps a new repository from organization templates.
    /// </summary>
    /// <param name="type">Project type key from config (e.g., classlib, console, tool).</param>
    /// <param name="name">Full project name (e.g., CalqFramework.Dev). Dot-separated.</param>
    /// <param name="lang">Language override (e.g., "C#", "F#").</param>
    /// <param name="public">Create a public GitHub repository.</param>
    /// <param name="private">Create a private GitHub repository.</param>
    /// <param name="internal">Create an internal GitHub repository (Enterprise only).</param>
    /// <param name="organization">GitHub organization override. Falls back to config default.</param>
    public async Task New([CliCompletion(nameof(CompleteProjectType))] string type, string name, string? lang = null, bool @public = false, bool @private = false, bool @internal = false, string? organization = null) {
        NewConfig cfg = await _config.GetAsync<NewConfig>();
        string org = organization ?? cfg.Organization;
        if (string.IsNullOrEmpty(org)) {
            try {
                org = CMD("gh api user --jq .login").Trim();
            } catch {
                throw new InvalidOperationException("Organization not set and 'gh' not available. Set it via --organization or config in: " + _config.ConfigDir);
            }
        }
        string language = lang ?? cfg.Lang;

        if (!cfg.ProjectTypes.ContainsKey(type)) {
            throw new InvalidOperationException($"Unknown project type '{type}'. Available: {string.Join(", ", cfg.ProjectTypes.Keys)}");
        }

        // Resolve names
        string nameSuffix = name.Contains('.') ? name[(name.IndexOf('.') + 1)..] : name;
        string kebabName = ToKebabCase(nameSuffix);
        string langFlag = language.Equals("F#", StringComparison.OrdinalIgnoreCase) ? "-lang \"F#\"" : "";

        bool createRepo = @public || @private || @internal;
        string visibility = @public ? "--public" : @internal ? "--internal" : "--private";

        // Preflight (only check required steps)
        List<string> allSteps = new(cfg.CommonRequiredSteps);
        allSteps.AddRange(cfg.ProjectTypes[type]);
        if (createRepo) allSteps.AddRange(cfg.GitInitSteps);
        RequireTools(allSteps);

        // Execute optional steps (warn on failure)
        foreach (string step in cfg.CommonOptionalSteps) {
            try {
                RUN(Expand(step, name, kebabName, nameSuffix, langFlag, visibility, org, cfg));
            } catch (Exception ex) {
                Console.Error.WriteLine($"Warning: {step} failed  E{ex.Message}");
            }
        }

        // Execute required common steps
        foreach (string step in cfg.CommonRequiredSteps) {
            RUN(Expand(step, name, kebabName, nameSuffix, langFlag, visibility, org, cfg));
        }

        // Execute project type steps
        List<string> expandedSteps = new();
        foreach (string step in cfg.ProjectTypes[type]) {
            string expanded = Expand(step, name, kebabName, nameSuffix, langFlag, visibility, org, cfg);
            RUN(expanded);
            expandedSteps.Add(expanded);
        }

        // Seed workflows from workflow-templates
        SeedWorkflowTemplates(PWD);

        // XML injection  Ematch each *.*proj to its dotnet new template type
        Dictionary<string, string> projectTemplateMap = MapProjectsToTemplates(expandedSteps);
        InjectProjXml(cfg.ProjXml, projectTemplateMap, name, kebabName, cfg.InitialVersion);

        // Git init and repo creation
        if (createRepo) {
            foreach (string step in cfg.GitInitSteps) {
                RUN(Expand(step, name, kebabName, nameSuffix, langFlag, visibility, org, cfg));
            }
        }
    }


    /// <summary>
    ///     Runs the configurable format pipeline on a directory.
    /// </summary>
    /// <param name="dir">Target directory containing .NET projects.</param>
    public async Task Format(DirectoryInfo? dir = null) {
        string target = dir?.FullName ?? ".";
        FormatConfig cfg = await _config.GetAsync<FormatConfig>();

        // Discover targets
        List<string> targets = DiscoverTargets(target);

        // Preflight  Eonly check tools for steps that will actually run
        List<string> activeCommands = new();
        foreach (FormatStep step in cfg.Steps) {
            if (step.FilePattern != null && !HasMatchingFiles(target, step.FilePattern)) {
                continue;
            }

            activeCommands.Add(step.Command);
        }

        RequireTools(activeCommands);

        // Execute pipeline
        foreach (FormatStep step in cfg.Steps) {
            if (step.FilePattern != null && !HasMatchingFiles(target, step.FilePattern)) {
                continue;
            }

            if (step.PerTarget) {
                foreach (string t in targets) {
                    RUN(step.Command.Replace("{target}", t).Replace("{dir}", target));
                }
            } else {
                RUN(step.Command.Replace("{dir}", target));
            }
        }
    }

    /// <summary>
    ///     Pulls from remote with rebase, regardless of current branch.
    /// </summary>
    public async Task Pull() {
        UtilityConfig cfg = await _config.GetAsync<UtilityConfig>();
        RequireTools(cfg.Pull);
        foreach (string step in cfg.Pull) {
            RUN(step);
        }
    }

    /// <summary>
    ///     Switches to or creates an issue branch.
    /// </summary>
    /// <param name="input">Issue number (numeric) or title (string to create new issue).</param>
    public async Task Switch([CliCompletion(nameof(CompleteIssues))] string input) {
        SwitchConfig cfg = await _config.GetAsync<SwitchConfig>();
        RequireTool("gh");

        string branchName;
        if (int.TryParse(input, out int issueId)) {
            branchName = $"{cfg.BranchPrefix}{issueId}";
        } else if (cfg.AutoCreateIssue) {
            string url = CMD($"gh issue create --title \"{input}\" --body \"\"");
            Match match = Regex.Match(url, @"/(\d+)\s*$");
            if (!match.Success) {
                throw new InvalidOperationException($"Failed to parse issue ID from: {url}");
            }

            issueId = int.Parse(match.Groups[1].Value);
            branchName = $"{cfg.BranchPrefix}{issueId}";
        } else {
            branchName = $"{cfg.BranchPrefix}{input}";
        }

        GitHelper.SafeExecute($"git switch -c {branchName}");
    }

    /// <summary>
    ///     Pushes to remote. Behavior differs on main vs feature branches.
    /// </summary>
    public async Task Push() {
        PushConfig cfg = await _config.GetAsync<PushConfig>();
        string branch = GitHelper.GetCurrentBranch();

        // Always pull first
        await Pull();

        if (branch == cfg.MainBranch) {
            RUN($"git push {cfg.Remote} {cfg.MainBranch}");
        } else {
            if (cfg.ForceWithLeaseOnFeature) {
                RUN($"git push --force-with-lease {cfg.Remote} {branch}");
            } else {
                RUN($"git push {cfg.Remote} {branch}");
            }

            if (cfg.CreatePr) {
                int? issueId = GitHelper.ExtractIssueId(branch);
                string title = branch;
                if (issueId != null) {
                    RequireTool("gh");
                    string issueTitle = CMD($"gh issue view {issueId} --json title --jq .title");
                    title = cfg.PrTitleFormat
                        .Replace("{IssueID}", issueId.ToString())
                        .Replace("{IssueTitle}", issueTitle);
                }

                try {
                    RUN($"gh pr create --title \"{title}\" --body \"\" --head {branch}");
                } catch {
                    // PR may already exist  Eupdate it
                    RUN($"gh pr edit --title \"{title}\"");
                }
            }
        }
    }

    /// <summary>
    ///     Squash-merges the current feature branch PR and cleans up.
    /// </summary>
    public async Task Merge() {
        MergeConfig cfg = await _config.GetAsync<MergeConfig>();
        PushConfig pushCfg = await _config.GetAsync<PushConfig>();
        string branch = GitHelper.GetCurrentBranch();

        if (branch == pushCfg.MainBranch) {
            throw new InvalidOperationException("Cannot merge from main branch.");
        }

        RequireTool("gh");

        // Sync and push
        await Push();

        // Atomic remote merge
        string mergeFlags = $"--{cfg.MergeStrategy}";
        if (cfg.DeleteBranch) mergeFlags += " --delete-branch";
        RUN($"gh pr merge {mergeFlags}");

        // Close issue
        if (cfg.CloseIssue) {
            int? issueId = GitHelper.ExtractIssueId(branch);
            if (issueId != null) {
                RUN($"gh issue close {issueId}");
            }
        }

        // Local cleanup
        GitHelper.SafeExecute($"git switch {pushCfg.MainBranch}");
        RUN($"git branch --delete --force {branch}");

        if (cfg.PullAfterMerge) {
            await Pull();
        }
    }

    /// <summary>
    ///     Forces re-evaluation of the dependency graph and updates lock files.
    /// </summary>
    public async Task Relock() {
        UtilityConfig cfg = await _config.GetAsync<UtilityConfig>();
        RequireTools(cfg.Relock);
        foreach (string step in cfg.Relock) {
            RUN(step);
        }
    }

    /// <summary>
    ///     Lists open issues from GitHub.
    /// </summary>
    public async Task Issues() {
        UtilityConfig cfg = await _config.GetAsync<UtilityConfig>();
        RequireTools(cfg.Issues);
        foreach (string step in cfg.Issues) {
            RUN(step);
        }
    }

    // ── Helpers ──

    private static string ToKebabCase(string value) =>
        Regex.Replace(value, "([a-z0-9])([A-Z])", "$1-$2").ToLower().Replace('.', '-');

    private static string Expand(string template, string projectFullName, string kebabName, string name, string langFlag, string visibility, string organization, NewConfig cfg) =>
        template
            .Replace("{organization}", organization)
            .Replace("{projectFullName}", projectFullName)
            .Replace("{kebabName}", kebabName)
            .Replace("{name}", name)
            .Replace("{langFlag}", langFlag)
            .Replace("{initialVersion}", cfg.InitialVersion)
            .Replace("{visibility}", visibility)
            .Replace("{ghRepoFlags}", cfg.GhRepoFlags)
            .Replace("{dir}", ".");

    private static Dictionary<string, string> MapProjectsToTemplates(List<string> steps) {
        Dictionary<string, string> map = new();
        foreach (string step in steps) {
            string[] args = step.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args is not ["dotnet", "new", var template, ..]) continue;

            int outputIndex = Array.IndexOf(args, "-o");
            if (outputIndex < 0) outputIndex = Array.IndexOf(args, "--output");
            if (outputIndex >= 0 && outputIndex + 1 < args.Length) {
                map[args[outputIndex + 1]] = template;
            }
        }
        return map;
    }

    private static void InjectProjXml(Dictionary<string, Dictionary<string, string>> projXmlByTemplate, Dictionary<string, string> projectTemplateMap, string projectFullName, string kebabName, string initialVersion) {
        foreach (string projFile in Directory.EnumerateFiles(PWD, "*.*proj", SearchOption.AllDirectories)) {
            XDocument doc = XDocument.Load(projFile);
            XElement? propGroup = doc.Root?.Element("PropertyGroup");
            if (propGroup == null) continue;

            // Match this proj file to its template type via output directory
            string? rawDir = Path.GetDirectoryName(projFile);
            if (rawDir == null) continue;
            string projDir = Path.GetRelativePath(PWD, rawDir).Replace("\\", "/");
            if (!projectTemplateMap.TryGetValue(projDir, out string? template)) continue;
            if (!projXmlByTemplate.TryGetValue(template, out Dictionary<string, string>? projXml)) continue;

            foreach (KeyValuePair<string, string> kvp in projXml) {
                string value = kvp.Value
                    .Replace("{projectFullName}", projectFullName)
                    .Replace("{kebabName}", kebabName)
                    .Replace("{initialVersion}", initialVersion);

                XElement? existing = propGroup.Element(kvp.Key);
                if (existing != null) {
                    existing.Value = value;
                } else {
                    propGroup.Add(new XElement(kvp.Key, value));
                }
            }

            // F# compilation order: ensure Program.fs stays at the bottom
            if (projFile.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)) {
                ReorderFSharpCompileItems(doc);
            }

            doc.Save(projFile);
        }
    }

    private static void ReorderFSharpCompileItems(XDocument doc) {
        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        List<XElement> itemGroups = doc.Root?.Elements(ns + "ItemGroup").ToList() ?? new();

        foreach (XElement itemGroup in itemGroups) {
            List<XElement> compileItems = itemGroup.Elements(ns + "Compile").ToList();
            if (compileItems.Count <= 1) continue;

            XElement? programFs = compileItems.FirstOrDefault(e =>
                e.Attribute("Include")?.Value.EndsWith("Program.fs", StringComparison.OrdinalIgnoreCase) == true);

            if (programFs != null) {
                programFs.Remove();
                itemGroup.Add(programFs);
            }
        }
    }

    private static List<string> DiscoverTargets(string dir) {
        List<string> targets = new();
        foreach (string file in Directory.EnumerateFiles(dir, "*.sln", SearchOption.TopDirectoryOnly)) {
            targets.Add(file);
        }

        if (targets.Count == 0) {
            foreach (string file in Directory.EnumerateFiles(dir, "*.*proj", SearchOption.TopDirectoryOnly)) {
                targets.Add(file);
            }
        }

        return targets;
    }

    private static bool HasMatchingFiles(string dir, string pattern) {
        try {
            return Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories).Any();
        } catch {
            return false;
        }
    }

    /// <summary>
    ///     Copies workflow templates from workflow-templates/ into .github/workflows/,
    ///     replacing $default-branch with main and skipping .properties.json metadata.
    /// </summary>
    private static void SeedWorkflowTemplates(string dir) {
        string templatesDir = Path.Combine(dir, "workflow-templates");
        if (!Directory.Exists(templatesDir)) return;

        string workflowsDir = Path.Combine(dir, ".github", "workflows");
        Directory.CreateDirectory(workflowsDir);

        foreach (string file in Directory.EnumerateFiles(templatesDir, "*.yaml")) {
            string content = File.ReadAllText(file);
            content = content.Replace("$default-branch", "main");
            string destPath = Path.Combine(workflowsDir, Path.GetFileName(file));
            File.WriteAllText(destPath, content);
        }

        // Clean up the downloaded workflow-templates directory
        Directory.Delete(templatesDir, true);
    }
}
