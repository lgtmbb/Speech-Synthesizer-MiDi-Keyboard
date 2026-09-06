using System.Diagnostics;
using Microsoft.Win32;

namespace SpeechMidiKeyboard.Services;

public sealed class EngineAvailabilityReport
{
    public bool Sapi5Available { get; init; }
    public bool Sapi4Available { get; init; }
    public bool OneCoreAvailable { get; init; }

    public bool AllAvailable => Sapi5Available && OneCoreAvailable;
    // NOTE: SAPI4 is deliberately excluded from AllAvailable — see VoiceInstallChecker remarks.
}

/// <summary>
/// Best-effort detection for the three engines named in the spec.
///
/// IMPORTANT HONESTY NOTE (read before "fixing" this class):
/// - SAPI5: ships with every modern Windows install. We only check whether any
///   *voices* are registered for it. Missing voices are fixed by opening the
///   Windows Speech settings page — Windows does not expose a supported silent
///   install API for this to third-party apps.
/// - OneCore: same story — engine is part of Windows 10/11, voices are managed
///   through Settings > Time & Language > Speech. We open that page for the user;
///   we do not, and cannot, silently download voice packs ourselves.
/// - SAPI4: Microsoft stopped distributing SAPI4 and its voices in the early
///   2000s. There is no current, official, silently-installable package for it on
///   Windows 10/11. We can *detect* legacy SAPI4 registrations left over from old
///   installs, but we cannot honestly offer a working one-click install for it.
///   The UI reflects this directly instead of pretending otherwise.
/// </summary>
public sealed class VoiceInstallChecker : IDisposable
{
    public EngineAvailabilityReport CheckInstalledEngines()
    {
        return new EngineAvailabilityReport
        {
            Sapi5Available = HasSapi5Voices(),
            Sapi4Available = HasLegacySapi4Registration(),
            OneCoreAvailable = HasOneCoreVoices()
        };
    }

    private static bool HasSapi5Voices()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Speech\Voices\Tokens");
            return key is not null && key.GetSubKeyNames().Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool HasOneCoreVoices()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens");
            return key is not null && key.GetSubKeyNames().Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool HasLegacySapi4Registration()
    {
        try
        {
            // Legacy SAPI4 TTS engines registered themselves under this key.
            // Its presence only proves an old installation exists on this machine;
            // it is not something this app can install for the user.
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Speech\Voices");
            if (key is null) return false;

            foreach (var sub in key.GetSubKeyNames())
            {
                if (sub.Contains("4", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Opens the Windows Settings page where SAPI5/OneCore voices are managed.</summary>
    public void OpenWindowsSpeechSettings()
    {
        Process.Start(new ProcessStartInfo("ms-settings:speech") { UseShellExecute = true });
    }

    public void Dispose()
    {
    }
}
