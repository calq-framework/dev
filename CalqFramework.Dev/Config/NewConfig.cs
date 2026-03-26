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
        "gh repo clone {organization}/.github -- -d {dir}",
        "gh repo clone {organization}/.license -- -d {dir}"
    ];

    public Dictionary<string, List<string>> ProjectTypes { get; set; } = new() {
        ["classlib"] = [
            "dotnet new classlib -n {name} -o {name} {langFlag}",
            "dotnet new xunit -n {name}.Tests -o {name}.Tests {langFlag}",
            "dotnet new sln -n {name}",
            "dotnet sln add {name} {name}.Tests",
            "dotnet add {name}.Tests reference {name}"
        ],
        ["console"] = [
            "dotnet new console -n {name} -o {name} {langFlag}",
            "dotnet new sln -n {name}",
            "dotnet sln add {name}"
        ],
        ["tool"] = [
            "dotnet new console -n {name} -o {name} {langFlag}",
            "dotnet new console -n {name}.Cli -o {name}.Cli {langFlag}",
            "dotnet new xunit -n {name}.Tests -o {name}.Tests {langFlag}",
            "dotnet new sln -n {name}",
            "dotnet sln add {name} {name}.Cli {name}.Tests",
            "dotnet add {name}.Cli reference {name}",
            "dotnet add {name}.Tests reference {name}"
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
