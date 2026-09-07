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
///
/// PITCH BEND IMPLEMENTATION NOTE:
/// None of the three engines (SAPI5/SAPI4/OneCore) can glide the pitch of an
/// utterance that's already playing - each Speak() call is a discrete, separate
/// utterance. To *simulate* a continuous bend, we do what the person who filed
/// this spec described: while Shift is held and a note is sounding, a fast
/// Timer repeatedly re-triggers Speak() with the pitch nudged a small step
/// further up/down each tick (like tapping a spinner's up-arrow very quickly).
/// Fast enough steps, close enough together, read to the ear as a slide rather
/// than as discrete notes - the same trick synth arpeggiators use to fake a
/// portamento out of a monophonic step sequencer.
/// </summary>
public sealed class PianoKeyboardForm : Form
{
    private readonly IVoiceEngine _engine;
    private readonly AppSettings _settings;
    private readonly Label _statusLabel;
    private readonly System.Windows.Forms.Timer _bendTimer;

    private const double BendStepSemitones = 0.35; // size of each "tap" in the fast step sequence
    private const int BendTickIntervalMs = 35;      // how often we re-trigger while bending - fast enough to read as a glide

    private int _octave;
    private int _activeBendPointIndex = 0; // defaults to F2 = smallest bend
    private bool _bendingUp;
    private bool _bendingDown;
    private double _currentBendSemitones; // where the fast step-sequence currently is, toward the active point's target
    private int? _heldNoteSemitone;       // the note currently being sustained (key held down), if any

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

        _bendTimer = new System.Windows.Forms.Timer { Interval = BendTickIntervalMs };
        _bendTimer.Tick += OnBendTimerTick;

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        FormClosed += (_, _) =>
        {
            _bendTimer.Stop();
            _bendTimer.Dispose();
            RestoreDefaultsFromSnapshot();
        };
    }

    private string BuildStatusText() =>
        $"Oktáv: {_octave}    Bend pont: F{_activeBendPointIndex + 2} " +
        $"(±{NoteKeyMap.SemitonesForPoint(_activeBendPointIndex):0.#} félhang)    " +
        $"Puffer: \"{_settings.CurrentBuffer}\"";

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
            _bendTimer.Start();
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
            _heldNoteSemitone = semitone;
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

            if (!_bendingUp && !_bendingDown)
            {
                _bendTimer.Stop();
                _currentBendSemitones = 0; // snap back to unbent pitch once both Shifts are released
            }
            _statusLabel.Text = BuildStatusText();
            return;
        }

        if (NoteKeyMap.TryGetSemitoneOffset(e.KeyCode, out var semitone) && _heldNoteSemitone == semitone)
        {
            _heldNoteSemitone = null;
        }
    }

    /// <summary>
    /// Fires every BendTickIntervalMs while at least one Shift is held. Each tick
    /// nudges the live bend amount one small step closer to the active point's
    /// target magnitude and, if a note is currently sustained, re-speaks it at
    /// the new pitch - the "very fast up/down stepping" the spec asked for.
    /// </summary>
    private void OnBendTimerTick(object? sender, EventArgs e)
    {
        var targetMagnitude = NoteKeyMap.SemitonesForPoint(_activeBendPointIndex);
        var target = _bendingUp ? targetMagnitude : _bendingDown ? -targetMagnitude : 0;

        if (_currentBendSemitones < target)
        {
            _currentBendSemitones = Math.Min(target, _currentBendSemitones + BendStepSemitones);
        }
        else if (_currentBendSemitones > target)
        {
            _currentBendSemitones = Math.Max(target, _currentBendSemitones - BendStepSemitones);
        }

        _statusLabel.Text = BuildStatusText();

        if (_heldNoteSemitone.HasValue)
        {
            PlayNote(_heldNoteSemitone.Value);
        }
    }

    private void PlayNote(int semitoneOffsetFromC)
    {
        var text = string.IsNullOrEmpty(_settings.CurrentBuffer) ? "la" : _settings.CurrentBuffer;

        // Middle reference octave is 4; each octave away is +/-12 semitones,
        // plus whatever the fast step-sequenced pitch bend is currently at.
        double totalSemitones = (_octave - 4) * 12 + semitoneOffsetFromC + _currentBendSemitones;

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

