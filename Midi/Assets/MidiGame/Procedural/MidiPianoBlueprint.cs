// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using CodeGamified.Procedural;
using UnityEngine;

namespace MidiGame.Procedural
{
    /// <summary>
    /// Procedural blueprint for a 3D piano keyboard.
    ///
    /// Generates white and black keys as Cubes, correctly sized and positioned
    /// following real piano layout (C to B per octave, sharps/flats raised).
    ///
    /// Part IDs: "white_60", "black_61", etc. (MIDI note number).
    /// Each key gets a collider for future click-to-play.
    /// </summary>
    public class MidiPianoBlueprint : IProceduralBlueprint
    {
        readonly int _lowNote;   // inclusive
        readonly int _highNote;  // inclusive

        // Key dimensions
        const float WhiteKeyWidth  = 0.24f;
        const float WhiteKeyHeight = 0.12f;
        const float WhiteKeyDepth  = 1.2f;

        const float BlackKeyWidth  = 0.14f;
        const float BlackKeyHeight = 0.18f;
        const float BlackKeyDepth  = 0.75f;

        // Which semitones within an octave are black keys (C#, D#, F#, G#, A#)
        static readonly bool[] IsBlack = { false, true, false, true, false, false, true, false, true, false, true, false };

        // X offset of each semitone within one octave (white keys evenly spaced,
        // black keys centered between their adjacent whites)
        // White keys: C=0, D=1, E=2, F=3, G=4, A=5, B=6  → 7 whites per octave
        static readonly int[] WhiteIndex = { 0, -1, 1, -1, 2, 3, -1, 4, -1, 5, -1, 6 };

        public MidiPianoBlueprint(int lowNote = 48, int highNote = 84)
        {
            _lowNote = lowNote;
            _highNote = highNote;
        }

        public string DisplayName => "MidiPiano";
        public ProceduralLODHint LODHint => ProceduralLODHint.Standard;
        public string PaletteId => "midi";

        public ProceduralPartDef[] GetParts()
        {
            var parts = new List<ProceduralPartDef>();

            // Count total white keys to compute total width
            int totalWhites = 0;
            for (int n = _lowNote; n <= _highNote; n++)
            {
                int semi = n % 12;
                if (!IsBlack[semi]) totalWhites++;
            }

            // Center the keyboard: white key 0 starts at -halfWidth
            float totalWidth = totalWhites * WhiteKeyWidth;
            float startX = -totalWidth / 2f;

            // Base platform
            parts.Add(new ProceduralPartDef("base", PrimitiveType.Cube,
                new Vector3(0f, -WhiteKeyHeight / 2f - 0.05f, WhiteKeyDepth * 0.3f),
                new Vector3(totalWidth + 0.4f, 0.08f, WhiteKeyDepth + 0.4f),
                "piano_body"));

            // First pass: white keys — build an X lookup for all notes
            var whiteXByNote = new Dictionary<int, float>();
            int whiteIdx = 0;
            for (int n = _lowNote; n <= _highNote; n++)
            {
                int semi = n % 12;
                if (IsBlack[semi]) continue;

                float x = startX + whiteIdx * WhiteKeyWidth + WhiteKeyWidth / 2f;
                whiteXByNote[n] = x;

                parts.Add(new ProceduralPartDef(
                    $"white_{n}", PrimitiveType.Cube,
                    new Vector3(x, 0f, 0f),
                    new Vector3(WhiteKeyWidth - 0.02f, WhiteKeyHeight, WhiteKeyDepth),
                    "key_white")
                {
                    Collider = ColliderMode.Box,
                    Tag = "key"
                });

                whiteIdx++;
            }

            // Second pass: black keys — positioned between adjacent whites
            for (int n = _lowNote; n <= _highNote; n++)
            {
                int semi = n % 12;
                if (!IsBlack[semi]) continue;

                // Find adjacent white keys (n-1 and n+1 are always white neighbors)
                int prevWhite = n - 1;
                int nextWhite = n + 1;

                // Need both neighbors in range
                if (!whiteXByNote.ContainsKey(prevWhite) || !whiteXByNote.ContainsKey(nextWhite))
                    continue;

                float blackX = (whiteXByNote[prevWhite] + whiteXByNote[nextWhite]) / 2f;

                parts.Add(new ProceduralPartDef(
                    $"black_{n}", PrimitiveType.Cube,
                    new Vector3(blackX, (BlackKeyHeight - WhiteKeyHeight) / 2f, -(WhiteKeyDepth - BlackKeyDepth) / 2f),
                    new Vector3(BlackKeyWidth, BlackKeyHeight, BlackKeyDepth),
                    "key_black")
                {
                    Collider = ColliderMode.Box,
                    Tag = "key"
                });
            }

            return parts.ToArray();
        }

        /// <summary>Get the X position for a given MIDI note, for note particle spawning.</summary>
        public float GetKeyX(int midiNote)
        {
            // Clamp to keyboard range
            midiNote = System.Math.Max(_lowNote, System.Math.Min(_highNote, midiNote));

            int semi = midiNote % 12;
            bool isBlack = IsBlack[semi];

            // Count whites up to this note
            int whitesBefore = 0;
            for (int n = _lowNote; n <= midiNote; n++)
            {
                int s = n % 12;
                if (!IsBlack[s]) whitesBefore++;
            }

            // Total white count for centering
            int totalWhites = 0;
            for (int n = _lowNote; n <= _highNote; n++)
            {
                int s = n % 12;
                if (!IsBlack[s]) totalWhites++;
            }

            float totalWidth = totalWhites * WhiteKeyWidth;
            float startX = -totalWidth / 2f;

            if (!isBlack)
            {
                // White key position
                return startX + (whitesBefore - 1) * WhiteKeyWidth + WhiteKeyWidth / 2f;
            }
            else
            {
                // Black key: between previous and next white
                float prevX = startX + (whitesBefore - 1) * WhiteKeyWidth + WhiteKeyWidth / 2f;
                float nextX = startX + whitesBefore * WhiteKeyWidth + WhiteKeyWidth / 2f;
                return (prevX + nextX) / 2f;
            }
        }
    }
}
