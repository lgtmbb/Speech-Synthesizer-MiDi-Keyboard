using System.Text.Json;
using SpeechMidiKeyboard.Settings;

namespace SpeechMidiKeyboard.Services;

/// <summary>
/// The "default speech values" file described in the spec:
/// captured once (first run), and used to reset Rate/Volume/PitchSemitones
/// on the live engine every time the MIDI keyboard overlay is closed with Escape,
/// so normal typed/spoken text after closing the keyboard always uses the
/// user's normal voice settings rather than whatever note was last played.
/// </summary>
public sealed class PitchSnapshot
{
    public int Rate { get; set; }
    public int Volume { get; set; }
    public double PitchSemitones { get; set; } // 0 = engine default pitch
}

public static class PitchSnapshotStore
{
    private static string SnapshotPath =>
        Path.Combine(AppContext.BaseDirectory, "pitch_default.json");

    public static void EnsureBaselineSnapshotExists(AppSettings settings)
    {
        if (File.Exists(SnapshotPath))
        {
            return;
        }

        var baseline = new PitchSnapshot
        {
            Rate = settings.Rate,
            Volume = settings.Volume,
            PitchSemitones = 0
        };
        Save(baseline);
    }

    public static PitchSnapshot Load()
    {
        try
        {
            var json = File.ReadAllText(SnapshotPath);
            return JsonSerializer.Deserialize<PitchSnapshot>(json) ?? new PitchSnapshot();
        }
        catch (Exception)
        {
            return new PitchSnapshot();
        }
    }

    public static void Save(PitchSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SnapshotPath, json);
    }
}
