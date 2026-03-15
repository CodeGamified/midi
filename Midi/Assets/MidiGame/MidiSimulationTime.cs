// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using UnityEngine;

namespace MidiGame
{
    /// <summary>
    /// MIDI-specific simulation time — subclasses the engine's abstract SimulationTime.
    /// Musical time: max scale 100x, formatted as bars:beats.
    /// </summary>
    public class MidiSimulationTime : CodeGamified.Time.SimulationTime
    {
        protected override float MaxTimeScale => 100f;

        float _bpm = 120f;

        /// <summary>Current BPM — set by MidiIOHandler when player calls set_bpm().</summary>
        public float BPM
        {
            get => _bpm;
            set => _bpm = Mathf.Clamp(value, 20f, 999f);
        }

        protected override void OnInitialize()
        {
            timeScalePresets = new float[]
                { 0f, 0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f, 50f, 100f };
            currentPresetIndex = 3; // Start at 1x
        }

        public override string GetFormattedTime()
        {
            // Format as bars:beats (4/4 time)
            float beatsPerSecond = _bpm / 60f;
            float totalBeats = (float)(simulationTime * beatsPerSecond);
            int bar = (int)(totalBeats / 4f) + 1;
            float beat = (totalBeats % 4f) + 1f;
            return $"{bar}:{beat:F1}";
        }
    }
}
