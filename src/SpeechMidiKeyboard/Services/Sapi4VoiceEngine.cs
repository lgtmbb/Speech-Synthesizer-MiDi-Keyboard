namespace SpeechMidiKeyboard.Services;

/// <summary>
/// SAPI4 support, honestly documented.
///
/// SAPI4 (1998-era "Speech API 4") was superseded by SAPI5 over twenty years ago.
/// Microsoft has not shipped SAPI4 voices or its runtime with Windows since the
/// XP era, there is no current redistributable, and there is no COM interface on
/// modern 64-bit Windows 10/11 that reliably hosts old SAPI4 voice DLLs anymore
/// (SAPI4 voices were 32-bit in-process COM servers with no x64 build).
///
/// This class exists so the app's engine-selection list can still *offer* SAPI4
/// as an option (per the original spec) and explain the situation in-place,
/// rather than silently omitting it or — worse — pretending to support it and
/// failing mysteriously when the user picks it.
///
/// If a genuine legacy SAPI4 installation is later found on a machine (see
/// VoiceInstallChecker.HasLegacySapi4Registration), a maintainer who wants to
/// wire up real playback would need to P/Invoke the old ISpVoice-predecessor
/// COM interfaces directly; that is out of scope for this initial version.
/// </summary>
public sealed class Sapi4VoiceEngine : IVoiceEngine
{
    public IReadOnlyList<VoiceInfo> GetVoices() => Array.Empty<VoiceInfo>();

    public void Speak(string text, string? voiceId, int rate, int volume, double pitchSemitones)
    {
        throw new NotSupportedException(
            "A SAPI4 nem támogatott modern Windows alatt: a Microsoft nem terjeszti " +
            "hivatalosan Windows 10/11-re, és a régi 32-bites hangmotorok nem futtathatók " +
            "natívan 64-bites folyamatban. Kérlek válassz SAPI5 vagy OneCore hangot.");
    }

    public void StopSpeaking()
    {
    }

    public void Dispose()
    {
    }
}
