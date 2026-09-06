using SpeechMidiKeyboard.Settings;

namespace SpeechMidiKeyboard.Services;

public static class VoiceEngineFactory
{
    public static IVoiceEngine Create(VoiceEngineKind kind) => kind switch
    {
        VoiceEngineKind.Sapi5 => new Sapi5VoiceEngine(),
        VoiceEngineKind.OneCore => new OneCoreVoiceEngine(),
        VoiceEngineKind.Sapi4 => new Sapi4VoiceEngine(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
