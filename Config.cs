using System.Text.Json;
namespace STWDailyClaimer;

internal class Config
{

    private const string ConfigPath = "accounts.json";

    public static ConfigObj CurrentConfig = default!;
    public static void InitConfig()
    {
        if (!File.Exists(ConfigPath))
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new ConfigObj()));


        CurrentConfig = JsonSerializer.Deserialize<ConfigObj>(File.ReadAllText(ConfigPath))!;
    }
    public static void SaveConfig() => File.WriteAllText(ConfigPath, JsonSerializer.Serialize(CurrentConfig));
    public class ConfigObj
    {
        public List<Dictionary<string, string>> Accounts { get; set; } = new();
    }
}