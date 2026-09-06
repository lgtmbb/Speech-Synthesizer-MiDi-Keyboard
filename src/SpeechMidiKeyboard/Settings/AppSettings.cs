using System.Text.Json;

namespace SpeechMidiKeyboard.Settings;

public enum VoiceEngineKind
{
    Sapi5,
    Sapi4,
    OneCore
}

/// <summary>
/// Portable settings: stored as JSON next to the executable, never in the registry,
/// so the app folder can be copied/run from a USB stick without installation.
/// </summary>
public sealed class AppSettings
{
    public VoiceEngineKind Engine { get; set; } = VoiceEngineKind.OneCore;
    public string? VoiceName { get; set; }
    public int Rate { get; set; } = 0;      // -10..10, SAPI5/OneCore convention
    public int Volume { get; set; } = 100;  // 0..100
    public int BaseOctave { get; set; } = 4; // matches keys 0-9 -> octave, 4 = default/no shift
    public string CurrentBuffer { get; set; } = string.Empty; // syllables/letters queued by Ctrl+1 / Ctrl+2

    private static string SettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings LoadOrCreateDefault()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings file: fall back to defaults rather than crash.
        }

        var defaults = new AppSettings();
        defaults.Save();
        return defaults;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
