// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using CodeGamified.Audio;
using CodeGamified.Settings;
using UnityEngine;

namespace MidiGame.Audio
{
    /// <summary>
    /// Procedural synth audio provider for the MIDI Code Game.
    /// Generates musical tones via AudioClip.Create() for UI/editor feedback,
    /// and provides the note playback engine for player programs.
    ///
    /// Two AudioSource layers:
    ///   _uiSource   — editor taps, compile feedback, navigation
    ///   _noteSource — polyphonic note playback from player code (pooled)
    /// </summary>
    public class MidiAudioProvider : IAudioProvider
    {
        AudioSource _uiSource;
        AudioSource _musicSource;

        // ── Pooled note sources for polyphony ───────────────────
        const int NOTE_POOL_SIZE = 8;
        readonly AudioSource[] _notePool = new AudioSource[NOTE_POOL_SIZE];
        int _nextNoteSource;

        // ── Pre-generated UI clips ──────────────────────────────
        AudioClip _tap;
        AudioClip _insert;
        AudioClip _delete;
        AudioClip _undo;
        AudioClip _redo;
        AudioClip _compileOk;
        AudioClip _compileErr;
        AudioClip _navigate;
        AudioClip _step;
        AudioClip _output;
        AudioClip _halted;

        // ── Synth waveform generators ───────────────────────────
        public enum WaveForm { Sine, Square, Saw, Triangle }
        WaveForm _currentWaveForm = WaveForm.Sine;
        float _masterVolume = 0.8f;

        public MidiAudioProvider()
        {
            var go = new GameObject("MidiAudio");
            Object.DontDestroyOnLoad(go);

            // UI feedback source
            _uiSource = go.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.spatialBlend = 0f;

            // Music / background source
            _musicSource = go.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.loop = true;

            // Note pool for polyphonic playback
            for (int i = 0; i < NOTE_POOL_SIZE; i++)
            {
                _notePool[i] = go.AddComponent<AudioSource>();
                _notePool[i].playOnAwake = false;
                _notePool[i].spatialBlend = 0f;
            }

            GenerateUIClips();

            SettingsBridge.OnChanged += OnSettingsChanged;
        }

        void OnSettingsChanged(SettingsSnapshot snapshot, SettingsCategory category)
        {
            if (category == SettingsCategory.Audio)
                UpdateVolumes();
        }

        void UpdateVolumes()
        {
            if (_musicSource != null && _musicSource.clip != null)
                _musicSource.volume = SettingsBridge.MusicVolume * SettingsBridge.MasterVolume;
        }

        // ═══════════════════════════════════════════════════════════
        //  UI CLIP GENERATION
        // ═══════════════════════════════════════════════════════════

        void GenerateUIClips()
        {
            // Musical UI sounds — pitched to common intervals
            _tap       = SynthTone(880f, 0.03f, "tap");          // A5 — quick tick
            _insert    = SynthChirp(440f, 880f, 0.08f, "insert"); // A4→A5 rising
            _delete    = SynthChirp(440f, 220f, 0.08f, "delete"); // A4→A3 falling
            _undo      = SynthChirp(660f, 440f, 0.06f, "undo");  // E5→A4 down
            _redo      = SynthChirp(440f, 660f, 0.06f, "redo");  // A4→E5 up
            _compileOk = SynthChord(new[] { 523f, 659f, 784f }, 0.15f, "compile_ok"); // C-E-G major
            _compileErr = SynthBuzz(110f, 0.2f, "compile_err");   // Low buzz
            _navigate  = SynthTone(660f, 0.02f, "navigate");     // E5 — soft tick
            _step      = SynthTone(1320f, 0.015f, "step");       // E6 — tiny tick
            _output    = SynthTone(784f, 0.04f, "output");       // G5
            _halted    = SynthChirp(784f, 262f, 0.15f, "halted"); // G5→C4 descending
        }

        void PlayUI(AudioClip clip, float volume = 0.2f)
        {
            if (_uiSource != null && clip != null)
                _uiSource.PlayOneShot(clip, volume * SettingsBridge.SfxVolume);
        }

        // ═══════════════════════════════════════════════════════════
        //  IAudioProvider — Editor
        // ═══════════════════════════════════════════════════════════

        public void PlayTap()            => PlayUI(_tap, 0.15f);
        public void PlayInsert()         => PlayUI(_insert, 0.2f);
        public void PlayDelete()         => PlayUI(_delete, 0.2f);
        public void PlayUndo()           => PlayUI(_undo, 0.15f);
        public void PlayRedo()           => PlayUI(_redo, 0.15f);
        public void PlayCompileSuccess() => PlayUI(_compileOk, 0.25f);
        public void PlayCompileError()   => PlayUI(_compileErr, 0.3f);
        public void PlayNavigate()       => PlayUI(_navigate, 0.1f);

        // ═══════════════════════════════════════════════════════════
        //  IAudioProvider — Engine
        // ═══════════════════════════════════════════════════════════

        public void PlayInstructionStep() => PlayUI(_step, 0.03f);
        public void PlayOutput()          => PlayUI(_output, 0.1f);
        public void PlayHalted()          => PlayUI(_halted, 0.15f);
        public void PlayIOBlocked()       => PlayUI(_compileErr, 0.15f);
        public void PlayWaitStateChanged() => PlayUI(_tap, 0.06f);

        // ═══════════════════════════════════════════════════════════
        //  IAudioProvider — Time (warp)
        // ═══════════════════════════════════════════════════════════

        public void PlayWarpStart()      => PlayUI(_insert, 0.2f);
        public void PlayWarpCruise()     { }
        public void PlayWarpDecelerate() => PlayUI(_undo, 0.15f);
        public void PlayWarpArrived()    => PlayUI(_compileOk, 0.3f);
        public void PlayWarpCancelled()  => PlayUI(_compileErr, 0.2f);
        public void PlayWarpComplete()   => PlayUI(_redo, 0.2f);

        // ═══════════════════════════════════════════════════════════
        //  IAudioProvider — Persistence
        // ═══════════════════════════════════════════════════════════

        public void PlaySaveStarted()    { }
        public void PlaySaveCompleted()  => PlayUI(_tap, 0.1f);
        public void PlaySyncCompleted()  => PlayUI(_redo, 0.1f);

        // ═══════════════════════════════════════════════════════════
        //  NOTE PLAYBACK — called by MidiProgramBehaviour
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Play a MIDI note. Converts note number to frequency,
        /// generates waveform, plays through pooled AudioSource.
        /// </summary>
        public void PlayNote(int midiNote, float velocity, float duration, WaveForm waveForm)
        {
            float freq = MidiNoteToFrequency(midiNote);
            float vol = (velocity / 127f) * _masterVolume * SettingsBridge.SfxVolume;
            var clip = GenerateNoteClip(freq, duration, waveForm);

            var source = _notePool[_nextNoteSource];
            _nextNoteSource = (_nextNoteSource + 1) % NOTE_POOL_SIZE;

            source.clip = clip;
            source.volume = vol;
            source.Play();
        }

        /// <summary>Play a note using the current synth waveform.</summary>
        public void PlayNote(int midiNote, float velocity, float duration)
        {
            PlayNote(midiNote, velocity, duration, _currentWaveForm);
        }

        public void SetWaveForm(WaveForm wf) => _currentWaveForm = wf;
        public void SetMasterVolume(float vol) => _masterVolume = Mathf.Clamp01(vol);

        /// <summary>Stop all playing notes.</summary>
        public void StopAllNotes()
        {
            for (int i = 0; i < NOTE_POOL_SIZE; i++)
            {
                if (_notePool[i].isPlaying)
                    _notePool[i].Stop();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  MIDI HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>MIDI note number → frequency (Hz). A4 (69) = 440 Hz.</summary>
        public static float MidiNoteToFrequency(int midiNote)
        {
            return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
        }

        /// <summary>Note name from MIDI number (e.g. 60 → "C4").</summary>
        public static string MidiNoteToName(int midiNote)
        {
            string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int octave = (midiNote / 12) - 1;
            int noteIdx = midiNote % 12;
            return $"{names[noteIdx]}{octave}";
        }

        // ═══════════════════════════════════════════════════════════
        //  WAVEFORM SYNTHESIS
        // ═══════════════════════════════════════════════════════════

        AudioClip GenerateNoteClip(float freq, float duration, WaveForm wf)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create("note", sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float phase = t * freq;
                float envelope = ComputeEnvelope(t, duration);

                data[i] = wf switch
                {
                    WaveForm.Sine     => Mathf.Sin(2f * Mathf.PI * phase),
                    WaveForm.Square   => Mathf.Sin(2f * Mathf.PI * phase) >= 0f ? 1f : -1f,
                    WaveForm.Saw      => 2f * (phase - Mathf.Floor(phase + 0.5f)),
                    WaveForm.Triangle => 2f * Mathf.Abs(2f * (phase - Mathf.Floor(phase + 0.5f))) - 1f,
                    _ => Mathf.Sin(2f * Mathf.PI * phase)
                } * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>ADSR-like envelope: quick attack, sustain, release.</summary>
        static float ComputeEnvelope(float t, float duration)
        {
            float attack = 0.01f;
            float release = Mathf.Min(0.05f, duration * 0.2f);
            float releaseStart = duration - release;

            if (t < attack)
                return t / attack; // attack ramp
            if (t > releaseStart)
                return Mathf.Max(0f, 1f - (t - releaseStart) / release); // release decay
            return 1f; // sustain
        }

        // ═══════════════════════════════════════════════════════════
        //  UI SYNTH HELPERS
        // ═══════════════════════════════════════════════════════════

        static AudioClip SynthTone(float freq, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-8f * t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Quick frequency sweep (rising or falling chirp).</summary>
        static AudioClip SynthChirp(float startFreq, float endFreq, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                float envelope = Mathf.Exp(-4f * progress);
                phase += 2f * Mathf.PI * freq / sampleRate;
                data[i] = Mathf.Sin(phase) * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Three-note chord — simultaneous tones with decay.</summary>
        static AudioClip SynthChord(float[] frequencies, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];
            float invCount = 1f / frequencies.Length;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-5f * t / duration);
                float sample = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                    sample += Mathf.Sin(2f * Mathf.PI * frequencies[f] * t);
                data[i] = sample * invCount * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Distorted low buzz for error feedback.</summary>
        static AudioClip SynthBuzz(float freq, float duration, string name)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-4f * t / duration);
                float raw = Mathf.Sin(2f * Mathf.PI * freq * t) * 3f;
                float clipped = raw / (1f + Mathf.Abs(raw));
                data[i] = clipped * envelope;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
