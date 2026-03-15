// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using MidiGame.Audio;

namespace MidiGame
{
    /// <summary>
    /// MonoBehaviour wrapper for MIDI program execution.
    /// Extends ProgramBehaviour with MIDI-specific event processing:
    ///   NoteOn  → MidiAudioProvider.PlayNote()
    ///   Sample  → (future sample bank)
    ///   Synth   → waveform switch
    ///   BPM     → tempo display update
    ///   Volume  → master volume
    ///   FX      → (future effect chain)
    /// </summary>
    public class MidiProgramBehaviour : ProgramBehaviour
    {
        MidiAudioProvider _midiAudio;
        MidiIOHandler _midiIO;

        // ── Synth name table (matches string constant indices) ──
        static readonly Dictionary<string, MidiAudioProvider.WaveForm> SynthMap = new()
        {
            { "sine",     MidiAudioProvider.WaveForm.Sine },
            { "square",   MidiAudioProvider.WaveForm.Square },
            { "saw",      MidiAudioProvider.WaveForm.Saw },
            { "triangle", MidiAudioProvider.WaveForm.Triangle },
            { "piano",    MidiAudioProvider.WaveForm.Sine },     // alias
            { "pluck",    MidiAudioProvider.WaveForm.Triangle }, // alias
            { "bass",     MidiAudioProvider.WaveForm.Saw },      // alias
            { "pad",      MidiAudioProvider.WaveForm.Sine },     // alias
        };

        // ═══════════════════════════════════════════════════════════
        //  SETUP
        // ═══════════════════════════════════════════════════════════

        /// <summary>Inject the shared audio provider (created by MidiGameBootstrap).</summary>
        public void SetAudioProvider(MidiAudioProvider audioProvider)
        {
            _midiAudio = audioProvider;
        }

        // ═══════════════════════════════════════════════════════════
        //  ProgramBehaviour OVERRIDES
        // ═══════════════════════════════════════════════════════════

        protected override IGameIOHandler CreateIOHandler()
        {
            _midiIO = new MidiIOHandler(_executor);
            return _midiIO;
        }

        protected override CompiledProgram CompileSource(string source, string name)
        {
            return PythonCompiler.Compile(source, name, new MidiCompilerExtension());
        }

        protected override void Update()
        {
            if (_midiIO != null)
                _midiIO.Tick(Time.deltaTime, Time.timeScale);
            base.Update();
        }

        protected override void ProcessEvents()
        {
            if (_executor?.State == null) return;

            while (_executor.State.OutputEvents.Count > 0)
            {
                var evt = _executor.State.OutputEvents.Dequeue();
                HandleMidiEvent(evt);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  EVENT DISPATCH
        // ═══════════════════════════════════════════════════════════

        void HandleMidiEvent(GameEvent evt)
        {
            if (_midiAudio == null) return;

            switch (evt.Type)
            {
                case MidiEventType.NoteOn:
                    int midiNote = Mathf.RoundToInt(evt.Value);
                    float velocity = evt.Channel; // velocity stored in Channel field
                    // Duration: read from the tag's associated data or use default
                    float duration = 0.25f;
                    _midiAudio.PlayNote(midiNote, velocity, duration);
                    break;

                case MidiEventType.NoteOff:
                    if (evt.Channel < 0)
                        _midiAudio.StopAllNotes();
                    break;

                case MidiEventType.SynthChange:
                    int synthIdx = Mathf.RoundToInt(evt.Value);
                    if (_program?.StringConstants != null && synthIdx >= 0 && synthIdx < _program.StringConstants.Length)
                    {
                        string synthName = _program.StringConstants[synthIdx];
                        if (SynthMap.TryGetValue(synthName, out var wf))
                            _midiAudio.SetWaveForm(wf);
                    }
                    break;

                case MidiEventType.VolumeChange:
                    _midiAudio.SetMasterVolume(evt.Value);
                    break;

                case MidiEventType.BpmChange:
                    // BPM is tracked by MidiIOHandler; UI can query it
                    break;

                case MidiEventType.SampleTrigger:
                    // Future: sample bank playback
                    break;

                case MidiEventType.FxChange:
                    // Future: effect chain
                    break;
            }
        }
    }
}
