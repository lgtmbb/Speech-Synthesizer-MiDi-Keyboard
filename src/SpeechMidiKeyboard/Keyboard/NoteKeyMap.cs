namespace SpeechMidiKeyboard.Keyboard;

/// <summary>
/// Standard "computer keyboard as piano" layout, the same idea used by countless
/// browser piano-typing toys and by VocalWriter/UTAU-style typing instruments:
/// the bottom two rows become one octave of white/black keys.
///
///   Black keys (upper row):   W  E     T  Y  U
///   White keys (lower row): Z  X  C  V  B  N  M  ,  .
///
/// This is documented in the README and in the in-app Súgó (Help) menu so users
/// can learn it without sighted reference to a diagram.
/// </summary>
public static class NoteKeyMap
{
    // Semitone offset from the octave's C, in the classic 2-row typing-piano layout.
    private static readonly Dictionary<Keys, int> SemitoneOffsets = new()
    {
        [Keys.Z] = 0,   // C
        [Keys.S] = 1,   // C#
        [Keys.X] = 2,   // D
        [Keys.D] = 3,   // D#
        [Keys.C] = 4,   // E
        [Keys.V] = 5,   // F
        [Keys.G] = 6,   // F#
        [Keys.B] = 7,   // G
        [Keys.H] = 8,   // G#
        [Keys.N] = 9,   // A
        [Keys.J] = 10,  // A#
        [Keys.M] = 11,  // B
        [Keys.Oemcomma] = 12, // C (next octave)
        [Keys.L] = 13,        // C#
        [Keys.OemPeriod] = 14 // D
    };

    public static bool TryGetSemitoneOffset(Keys key, out int semitoneOffset) =>
        SemitoneOffsets.TryGetValue(key, out semitoneOffset);

    /// <summary>Octave number for keys 0-9 across the top of the keyboard (spec item 4).</summary>
    public static bool TryGetOctaveFromDigitKey(Keys key, out int octave)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            octave = key - Keys.D0;
            return true;
        }
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            octave = key - Keys.NumPad0;
            return true;
        }
        octave = 0;
        return false;
    }

    /// <summary>
    /// F2-F12 select one of 11 pitch-bend "points" (spec item 5) — think of these
    /// as 11 preset bend depths/curve stops the right/left Shift bend moves between,
    /// rather than a single fixed +/- amount. Point 0 (F2) = smallest bend,
    /// point 10 (F12) = largest bend, evenly spaced across a configurable max range.
    /// </summary>
    public static bool TryGetPitchBendPointIndex(Keys key, out int pointIndex)
    {
        if (key >= Keys.F2 && key <= Keys.F12)
        {
            pointIndex = key - Keys.F2;
            return true;
        }
        pointIndex = -1;
        return false;
    }

    public const double MaxPitchBendSemitones = 12.0; // one octave, spread across the 11 F-key points
    public const int PitchBendPointCount = 11;         // F2..F12

    public static double SemitonesForPoint(int pointIndex) =>
        MaxPitchBendSemitones * (pointIndex + 1) / PitchBendPointCount;
}
