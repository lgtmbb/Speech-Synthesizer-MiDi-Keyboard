using SpeechMidiKeyboard.Services;
using SpeechMidiKeyboard.Settings;

namespace SpeechMidiKeyboard.Keyboard;

/// <summary>
/// The Ctrl+K "MIDI keyboard" overlay (spec items 2-5).
///
/// While this form is open:
///   - Z/S/X/D/C/V/G/B/H/N/J/M/,/L/. play the buffered syllable/letter text
///     as a note at the current octave + pitch bend.
///   - Right Shift (held) bends pitch up, Left Shift (held) bends pitch down,
///     toward whichever F2-F12 "point" was last selected.
///   - 0-9 change the current octave.
///   - F2-F12 select the active pitch-bend point (deeper/shallower bend).
///   - Escape closes the overlay and restores the engine to the values saved
///     in pitch_default.json (spec item 8's last clause).
/// </summary>
public sealed class PianoKeyboardForm : Form
{
    private readonly IVoiceEngine _engine;
    private readonly AppSettings _settings;
    private readonly Label _statusLabel;

    private int _octave;
    private int _activeBendPointIndex = 0; // defaults to F2 = smallest bend
    private bool _bendingUp;
    private bool _bendingDown;

    public PianoKeyboardForm(IVoiceEngine engine, AppSettings settings)
    {
        _engine = engine;
        _settings = settings;
        _octave = settings.BaseOctave;

        Text = "MIDI billentyűzet (Esc a kilépéshez)";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(560, 220);
        KeyPreview = true; // form sees all key events before controls, which is what we want here
        AccessibleName = "MIDI billentyűzet";
        AccessibleDescription =
            "Z S X D C V G B H N J M , L . billentyűkkel játszhatók a hangok. " +
            "Jobb Shift: hangmagasság feljebb. Bal Shift: hangmagasság lejjebb. " +
            "0-9: oktáv váltás. F2-F12: hangmagasság-eltolási pont kiválasztása. Esc: kilépés.";

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 12f),
            AccessibleName = "Aktuális állapot",
            Text = BuildStatusText()
        };
        Controls.Add(_statusLabel);

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        FormClosed += (_, _) => RestoreDefaultsFromSnapshot();
    }

    private string BuildStatusText() =>
        $"Oktáv: {_octave}    Bend pont: F{_activeBendPointIndex + 2} " +
        $"(±{NoteKeyMap.SemitonesForPoint(_activeBendPointIndex):0.#} félhang)    " +
        $"Puffer: \"{_settings.CurrentBuffer}\"";

    private double CurrentPitchOffset()
    {
        var magnitude = NoteKeyMap.SemitonesForPoint(_activeBendPointIndex);
        if (_bendingUp) return magnitude;
        if (_bendingDown) return -magnitude;
        return 0;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.ShiftKey)
        {
            // Distinguish left/right Shift, which plain KeyCode can't do.
            bool isRight = (User32.GetKeyState((int)Keys.RShiftKey) & 0x8000) != 0;
            bool isLeft = (User32.GetKeyState((int)Keys.LShiftKey) & 0x8000) != 0;
            if (isRight) _bendingUp = true;
            if (isLeft) _bendingDown = true;
            _statusLabel.Text = BuildStatusText();
            e.Handled = true;
            return;
        }

        if (NoteKeyMap.TryGetOctaveFromDigitKey(e.KeyCode, out var octave))
        {
            _octave = octave;
            _statusLabel.Text = BuildStatusText();
            e.Handled = true;
            return;
        }

        if (NoteKeyMap.TryGetPitchBendPointIndex(e.KeyCode, out var pointIndex))
        {
            _activeBendPointIndex = pointIndex;
            _statusLabel.Text = BuildStatusText();
            e.Handled = true;
            return;
        }

        if (NoteKeyMap.TryGetSemitoneOffset(e.KeyCode, out var semitone) && !e.Alt && !e.Control)
        {
            PlayNote(semitone);
            e.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.ShiftKey)
        {
            bool rightStillDown = (User32.GetKeyState((int)Keys.RShiftKey) & 0x8000) != 0;
            bool leftStillDown = (User32.GetKeyState((int)Keys.LShiftKey) & 0x8000) != 0;
            _bendingUp = rightStillDown;
            _bendingDown = leftStillDown;
            _statusLabel.Text = BuildStatusText();
        }
    }

    private void PlayNote(int semitoneOffsetFromC)
    {
        var text = string.IsNullOrEmpty(_settings.CurrentBuffer) ? "la" : _settings.CurrentBuffer;

        // Middle reference octave is 4; each octave away is +/-12 semitones,
        // plus whatever the current pitch-bend (Shift) is contributing.
        double totalSemitones = (_octave - 4) * 12 + semitoneOffsetFromC + CurrentPitchOffset();

        _engine.Speak(text, _settings.VoiceName, _settings.Rate, _settings.Volume, totalSemitones);
    }

    /// <summary>Spec item 8: closing the keyboard (Esc) always restores the saved defaults.</summary>
    private void RestoreDefaultsFromSnapshot()
    {
        var snapshot = Services.PitchSnapshotStore.Load();
        _settings.Rate = snapshot.Rate;
        _settings.Volume = snapshot.Volume;
        _engine.Speak(string.Empty, null, snapshot.Rate, snapshot.Volume, 0); // no-op speak just to reset internal state where relevant
        _engine.StopSpeaking();
        _settings.Save();
    }
}

/// <summary>Minimal P/Invoke needed to tell left/right Shift apart (WinForms KeyEventArgs can't).</summary>
internal static class User32
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern short GetKeyState(int nVirtKey);
}
