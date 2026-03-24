using CalqFramework.Config.Json;
using CalqFramework.Dev;
using CalqFramework.Dev.Config;

string configDir = Environment.GetEnvironmentVariable("CODESPACES") == "true"
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dotfiles", ".config", "dev")
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dev");

var config = new JsonConfigurationRegistry<MasterPreset>(configDir);

try {
    object? result = new CommandLineInterface().Execute(new DevManager(config));
    switch (result) {
        case ValueTuple:
            break;
        case string str:
            Console.WriteLine(str);
            break;
        case object obj:
            Console.WriteLine(JsonSerializer.Serialize(obj));
            break;
    }
} catch (CliException ex) {
    Console.Error.WriteLine(ex.Message);
    Environment.Exit(1);
}
