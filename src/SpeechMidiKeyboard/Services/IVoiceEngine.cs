namespace SpeechMidiKeyboard.Services;

/// <summary>
/// A single speech engine's voice, engine-agnostic so the UI can list
/// SAPI5 / SAPI4 / OneCore voices side by side.
/// </summary>
public sealed record VoiceInfo(string Id, string DisplayName);

/// <summary>
/// Common surface used by MainForm / PianoKeyboardForm regardless of which
/// of the three engines is active. Pitch is expressed in semitones relative
/// to the voice's natural pitch (0 = unchanged) via SSML &lt;prosody&gt;,
/// since none of the three engines expose a live "bend while speaking"
/// API — each keypress speaks the current buffer text with the requested
/// pitch/rate, the same "typing plays a note" model used by VocalWriter
/// and browser-JS syllable keyboards.
/// </summary>
public interface IVoiceEngine : IDisposable
{
    IReadOnlyList<VoiceInfo> GetVoices();
    void Speak(string text, string? voiceId, int rate, int volume, double pitchSemitones);
    void StopSpeaking();
}
