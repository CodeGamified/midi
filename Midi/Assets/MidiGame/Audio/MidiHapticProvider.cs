// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using CodeGamified.Audio;

namespace MidiGame.Audio
{
    /// <summary>
    /// Haptic provider for the MIDI Code Game.
    /// Stub — platform-specific implementations can override
    /// for mobile vibration synced to beats.
    /// </summary>
    public class MidiHapticProvider : IHapticProvider
    {
        public void TapLight() { }
        public void TapMedium() { }
        public void TapHeavy() { }
        public void Buzz(float duration) { }
    }
}
