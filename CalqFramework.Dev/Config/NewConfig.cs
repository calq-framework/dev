namespace CalqFramework.Dev.Config;

/// <summary>
///     Configuration for the New command. ProjectTypes maps a template key
///     to its scaffolding steps, enabling user-defined project archetypes.
/// </summary>
[PresetGroup("Workflow")]
public class NewConfig {
    public string Organization { get; set; } = "";
    public string Lang { get; set; } = "C#";
    public string InitialVersion { get; set; } = "0.0.0";

    public List<string> CommonRequiredSteps { get; set; } = [];

    public List<string> CommonOptionalSteps { get; set; } = [
        "gh repo clone {organization}/.github -- {tempDir}/.github",
        "gh repo clone {organization}/.license -- {tempDir}/.license"
    ];

    public Dictionary<string, List<string>> ProjectTypes { get; set; } = new() {
        ["classlib"] = [
            "dotnet new classlib -n {projectFullName} -o {projectFullName} {langFlag}",
            "dotnet new xunit -n {projectFullName}.Tests -o {projectFullName}.Tests {langFlag}",
            "dotnet new sln -n {projectFullName}",
            "dotnet sln add {projectFullName} {projectFullName}.Tests",
            "dotnet add {projectFullName}.Tests reference {projectFullName}"
        ],
        ["console"] = [
            "dotnet new console -n {projectFullName} -o {projectFullName} {langFlag}",
            "dotnet new sln -n {projectFullName}",
            "dotnet sln add {projectFullName}"
        ],
        ["tool"] = [
            "dotnet new console -n {projectFullName} -o {projectFullName} {langFlag}",
            "dotnet new console -n {projectFullName}.Cli -o {projectFullName}.Cli {langFlag}",
            "dotnet new xunit -n {projectFullName}.Tests -o {projectFullName}.Tests {langFlag}",
            "dotnet new sln -n {projectFullName}",
            "dotnet sln add {projectFullName} {projectFullName}.Cli {projectFullName}.Tests",
            "dotnet add {projectFullName}.Cli reference {projectFullName}",
            "dotnet add {projectFullName}.Tests reference {projectFullName}"
        ]
    };

    /// <summary>
    ///     XML properties to inject into *.*proj files, keyed by dotnet new template type
    ///     (e.g., classlib, console, xunit). Matched by parsing ProjectTypes step commands.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> ProjXml { get; set; } = new() {
        ["classlib"] = new() {
            ["RootNamespace"] = "{projectFullName}",
            ["PackageId"] = "{projectFullName}",
            ["Version"] = "{initialVersion}",
            ["PublishRepositoryUrl"] = "true",
            ["EmbedUntrackedSources"] = "true"
        },
        ["console"] = new() {
            ["RootNamespace"] = "{projectFullName}",
            ["Version"] = "{initialVersion}"
        },
        ["xunit"] = new() {
            ["RootNamespace"] = "{projectFullName}"
        }
    };

    public string GhRepoFlags { get; set; } = "--disable-wiki";

    public List<string> GitInitSteps { get; set; } = [
        "git init --initial-branch=main",
        "git add .",
        "git commit -m \"init\"",
        "gh repo create {organization}/{kebabName} {visibility} --source=. --remote=origin {ghRepoFlags}",
        "git push --set-upstream origin main"
    ];
}
