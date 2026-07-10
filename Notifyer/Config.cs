using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notifyer;

public sealed class AppConfig
{
    public int IntervalMinutes { get; set; } = 20;

    /// <summary>Legacy; migrated into <see cref="Sound"/> on load.</summary>
    public bool? SoundEnabled { get; set; }

    /// <summary>Toast sound id from <see cref="ToastSounds"/> (e.g. default, reminder, alarm5, off).</summary>
    public string Sound { get; set; } = ToastSounds.Default;

    public string Message { get; set; } = "Camdan dışarı bak, gözlerini dinlendir.";
    public int FilmModeMinutes { get; set; } = 180;
    public List<string> QuietProcesses { get; set; } = ["cs2.exe", "csgo.exe"];
    public bool StartWithWindows { get; set; } = true;

    /// <summary>Use urgent toast scenario so reminders can break through Do Not Disturb / game focus.</summary>
    public bool BypassDoNotDisturb { get; set; } = true;

    [JsonIgnore]
    public bool FilmModeEnabled { get; set; }

    [JsonIgnore]
    public bool IsPaused { get; set; }

    [JsonIgnore]
    public bool IsSoundOn => !ToastSounds.Resolve(Sound).Id.Equals(ToastSounds.Off, StringComparison.OrdinalIgnoreCase);
}

public static class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ConfigDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Notifyer");

    public static string ConfigPath { get; } = Path.Combine(ConfigDirectory, "config.json");

    public static AppConfig Load()
    {
        Directory.CreateDirectory(ConfigDirectory);

        if (!File.Exists(ConfigPath))
        {
            var defaults = new AppConfig();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            Normalize(config);
            return config;
        }
        catch
        {
            var defaults = new AppConfig();
            Save(defaults);
            return defaults;
        }
    }

    public static void Save(AppConfig config)
    {
        Normalize(config);
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    public static void OpenInEditor()
    {
        Directory.CreateDirectory(ConfigDirectory);
        if (!File.Exists(ConfigPath))
            Save(new AppConfig());

        Process.Start(new ProcessStartInfo
        {
            FileName = ConfigPath,
            UseShellExecute = true
        });
    }

    private static void Normalize(AppConfig config)
    {
        if (config.IntervalMinutes < 1)
            config.IntervalMinutes = 1;

        if (config.FilmModeMinutes < 1)
            config.FilmModeMinutes = 1;

        // Migrate old soundEnabled bool → sound id.
        if (config.SoundEnabled is false &&
            (string.IsNullOrWhiteSpace(config.Sound) ||
             config.Sound.Equals(ToastSounds.Default, StringComparison.OrdinalIgnoreCase)))
        {
            config.Sound = ToastSounds.Off;
        }

        config.SoundEnabled = null;
        config.Sound = ToastSounds.Resolve(config.Sound).Id;

        config.QuietProcesses ??= [];
        config.QuietProcesses = config.QuietProcesses
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(config.Message))
            config.Message = "Camdan dışarı bak, gözlerini dinlendir.";
    }
}
