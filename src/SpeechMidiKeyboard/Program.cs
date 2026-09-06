using SpeechMidiKeyboard.Services;
using SpeechMidiKeyboard.Settings;

namespace SpeechMidiKeyboard;

internal static class Program
{
    /// <summary>
    /// Portable app entry point. Loads/creates settings next to the executable
    /// (no registry writes, no installer), then checks the three speech engines
    /// (SAPI5 / SAPI4 / OneCore) before showing the main window.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settings = AppSettings.LoadOrCreateDefault();

        // Snapshot the engine defaults to disk the very first time the app runs,
        // so the "singing" pitch changes made while the MIDI keyboard is open
        // can always be reverted to a known-good baseline on Escape.
        PitchSnapshotStore.EnsureBaselineSnapshotExists(settings);

        using var checker = new VoiceInstallChecker();
        var report = checker.CheckInstalledEngines();

        if (!report.AllAvailable)
        {
            using var dialog = new EngineCheckForm(report, checker);
            dialog.ShowDialog();
        }

        Application.Run(new MainForm(settings));
    }
}
