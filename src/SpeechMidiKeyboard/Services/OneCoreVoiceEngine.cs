using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Media.SpeechSynthesis;

namespace SpeechMidiKeyboard.Services;

/// <summary>
/// OneCore voices (the modern Windows 10/11 narrator-quality voices) are exposed
/// to Win32 apps through the WinRT Windows.Media.SpeechSynthesis API, not through
/// System.Speech. We synthesize to an in-memory stream via SSML and play it back
/// with MediaPlayer, since WinRT's synthesizer has no direct "speaker output" call.
/// </summary>
public sealed class OneCoreVoiceEngine : IVoiceEngine
{
    private readonly SpeechSynthesizer _synth = new();
    private readonly MediaPlayer _player = new();

    public IReadOnlyList<VoiceInfo> GetVoices()
    {
        return SpeechSynthesizer.AllVoices
            .Select(v => new VoiceInfo(v.Id, $"{v.DisplayName} ({v.Language})"))
            .ToList();
    }

    public async void Speak(string text, string? voiceId, int rate, int volume, double pitchSemitones)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (voiceId is not null)
        {
            var match = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == voiceId);
            if (match is not null)
            {
                _synth.Voice = match;
            }
        }

        // OneCore's SSML pitch range is roughly x-low..x-high / percentage; semitone
        // strings are also accepted by the underlying engine on current Windows builds.
        var sign = pitchSemitones >= 0 ? "+" : "";
        var rateWord = rate switch
        {
            <= -6 => "x-slow",
            <= -2 => "slow",
            >= 6 => "x-fast",
            >= 2 => "fast",
            _ => "medium"
        };

        var ssml =
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" +
            $"<prosody pitch=\"{sign}{pitchSemitones:0.##}st\" rate=\"{rateWord}\" volume=\"{Math.Clamp(volume, 0, 100)}\">" +
            System.Security.SecurityElement.Escape(text) +
            "</prosody></speak>";

        try
        {
            var stream = await _synth.SynthesizeSsmlToStreamAsync(ssml);
            _player.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            _player.Play();
        }
        catch (Exception)
        {
            // If SSML pitch syntax is rejected by a particular installed voice,
            // fall back to plain text so the app never goes silent.
            var stream = await _synth.SynthesizeTextToStreamAsync(text);
            _player.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            _player.Play();
        }
    }

    public void StopSpeaking() => _player.Pause();

    public void Dispose()
    {
        _player.Dispose();
        _synth.Dispose();
    }
}
