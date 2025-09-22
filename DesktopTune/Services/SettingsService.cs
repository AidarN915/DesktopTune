using DesktopTune.Model;
using System;
using System.IO;
using System.Text.Json;

public static class SettingsService
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfYoutubePlayer", "settings.json");
    private static readonly string CommandsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WpfYoutubePlayer", "commands.json");

    public static UserSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<UserSettings>(json);
            }
        }
        catch { }
        return new UserSettings();
    }

    public static void SaveSettings(UserSettings settings)
    {
        try
        {
            string dir = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    public static List<TwitchChatCommand> LoadCommands()
    {
        try
        {
            if (File.Exists(CommandsPath))
            {
                string json = File.ReadAllText(CommandsPath);
                return JsonSerializer.Deserialize<List<TwitchChatCommand>>(json);
            }
        }
        catch { }
        return new List<TwitchChatCommand>();
    }

    public static void SaveCommands(List<TwitchChatCommand> commands)
    {
        try
        {
            string dir = Path.GetDirectoryName(CommandsPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(commands, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CommandsPath, json);
        }
        catch { }
    }
}
