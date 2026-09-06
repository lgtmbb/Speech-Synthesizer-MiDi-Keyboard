using System.Speech.Synthesis;

namespace SpeechMidiKeyboard.Services;

/// <summary>
/// Wraps System.Speech.Synthesis (SAPI5). Pitch bend is done via SSML
/// &lt;prosody pitch="..."&gt; because SpeechSynthesizer has no direct pitch
/// property — this is the standard, documented way to affect SAPI5 pitch.
/// </summary>
public sealed class Sapi5VoiceEngine : IVoiceEngine
{
    private readonly SpeechSynthesizer _synth = new();

    public IReadOnlyList<VoiceInfo> GetVoices()
    {
        var list = new List<VoiceInfo>();
        foreach (var voice in _synth.GetInstalledVoices())
        {
            if (voice.Enabled)
            {
                list.Add(new VoiceInfo(voice.VoiceInfo.Id, voice.VoiceInfo.Name));
            }
        }
        return list;
    }

    public void Speak(string text, string? voiceId, int rate, int volume, double pitchSemitones)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (voiceId is not null)
        {
            try
            {
                var match = _synth.GetInstalledVoices()
                    .FirstOrDefault(v => v.VoiceInfo.Id == voiceId);
                if (match is not null)
                {
                    _synth.SelectVoice(match.VoiceInfo.Name);
                }
            }
            catch (Exception)
            {
                // Keep previously selected voice if this one can't be selected.
            }
        }

        _synth.Rate = Math.Clamp(rate, -10, 10);
        _synth.Volume = Math.Clamp(volume, 0, 100);

        // Convert semitones to a percentage-ish SSML pitch offset. SAPI5's SSML
        // pitch attribute accepts "+N%"/"-N%" or "xx.xxst" (semitone) style values
        // depending on the underlying engine; "st" (semitone) is the most portable.
        var sign = pitchSemitones >= 0 ? "+" : "";
        var ssml =
            $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" +
            $"<prosody pitch=\"{sign}{pitchSemitones:0.##}st\">{System.Security.SecurityElement.Escape(text)}</prosody>" +
            $"</speak>";

        _synth.SpeakAsyncCancelAll();
        try
        {
            _synth.SpeakSsmlAsync(ssml);
        }
        catch (Exception)
        {
            // Some SAPI5 voices reject unsupported SSML pitch syntax; fall back to plain speech.
            _synth.SpeakAsync(text);
        }
    }

    public void StopSpeaking() => _synth.SpeakAsyncCancelAll();

    public void Dispose() => _synth.Dispose();
}
