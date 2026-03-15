// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System;
using CodeGamified.Engine;
using CodeGamified.Time;
using UnityEngine;

namespace MidiGame
{
    /// <summary>
    /// Runtime I/O handler for MIDI opcodes.
    /// Receives CUSTOM instructions from CodeExecutor,
    /// translates them to GameEvents (NoteOn/NoteOff/SampleTrigger/etc.),
    /// and manages active synth / effect state.
    ///
    /// Actual audio synthesis is done by MidiAudioProvider, which
    /// listens to events via CodeExecutor.OnOutput.
    /// </summary>
    public class MidiIOHandler : IGameIOHandler
    {
        readonly CodeExecutor _executor;

        // ── Runtime state ───────────────────────────────────────
        float _bpm = 120f;
        float _volume = 0.8f;
        int _currentSynthIndex;
        int _currentFxIndex;
        float _currentFxMix = 0.5f;

        // ── Public state accessors ──────────────────────────────
        public float BPM => _bpm;
        public float Volume => _volume;
        public int CurrentSynthIndex => _currentSynthIndex;

        public MidiIOHandler(CodeExecutor executor)
        {
            _executor = executor;
        }

        // ═══════════════════════════════════════════════════════════
        //  IGameIOHandler
        // ═══════════════════════════════════════════════════════════

        public bool PreExecute(Instruction inst, MachineState state)
        {
            // No pre-execution gating for MIDI — all instructions always proceed
            return true;
        }

        public void ExecuteIO(Instruction inst, MachineState state)
        {
            switch (inst.Op)
            {
                case MidiOpCode.PLAY:
                    ExecutePlay(inst, state);
                    break;

                case MidiOpCode.SAMPLE:
                    ExecuteSample(inst, state);
                    break;

                case MidiOpCode.USE_SYNTH:
                    ExecuteUseSynth(inst, state);
                    break;

                case MidiOpCode.SET_BPM:
                    ExecuteSetBpm(inst, state);
                    break;

                case MidiOpCode.SET_VOLUME:
                    ExecuteSetVolume(inst, state);
                    break;

                case MidiOpCode.USE_FX:
                    ExecuteUseFx(inst, state);
                    break;

                case MidiOpCode.STOP_ALL:
                    _executor.EmitEvent(new GameEvent(
                        MidiEventType.NoteOff, 0f, -1, GetSimulationTime()));
                    break;

                case MidiOpCode.NOTE_OFF:
                    float noteOff = state.GetRegister(inst.Arg0);
                    _executor.EmitEvent(new GameEvent(
                        MidiEventType.NoteOff, noteOff, inst.Tag, GetSimulationTime()));
                    break;

                default:
                    Debug.LogWarning($"[MidiIO] Unknown MIDI opcode: {inst.Op}");
                    break;
            }
        }

        public float GetTimeScale()
        {
            return SimulationTime.Instance?.timeScale ?? 1f;
        }

        public double GetSimulationTime()
        {
            return SimulationTime.Instance?.simulationTime ?? 0.0;
        }

        /// <summary>Call from Update() to keep BPM synced with SimulationTime.</summary>
        public void Tick(float deltaTime, float timeScale)
        {
            // SimulationTime now owns the clock; just sync BPM to it
            if (SimulationTime.Instance is MidiSimulationTime mst)
                mst.BPM = _bpm;
        }

        // ═══════════════════════════════════════════════════════════
        //  INSTRUCTION EXECUTION
        // ═══════════════════════════════════════════════════════════

        void ExecutePlay(Instruction inst, MachineState state)
        {
            float note = state.GetRegister(inst.Arg0);
            float velocity = state.ReadMemory(inst.Arg1);
            float duration = state.ReadMemory(inst.Arg2);

            // Clamp to valid MIDI range
            note = Mathf.Clamp(note, 0f, 127f);
            velocity = Mathf.Clamp(velocity, 0f, 127f);
            if (duration <= 0f) duration = 0.25f;

            // Tag encodes: synth index in upper 8 bits, channel in lower 8
            int tag = (_currentSynthIndex << 8) | (inst.Tag & 0xFF);

            _executor.EmitEvent(new GameEvent(
                MidiEventType.NoteOn,
                note,
                (int)velocity,
                GetSimulationTime(),
                tag));

            // Encode duration as a float stored in the event value for the audio layer
            // The audio provider will handle note-off scheduling
        }

        void ExecuteSample(Instruction inst, MachineState state)
        {
            float sampleIdx = state.GetRegister(inst.Arg0);
            float velocity = state.ReadMemory(inst.Arg1);
            velocity = Mathf.Clamp(velocity, 0f, 127f);

            _executor.EmitEvent(new GameEvent(
                MidiEventType.SampleTrigger,
                sampleIdx,
                (int)velocity,
                GetSimulationTime()));
        }

        void ExecuteUseSynth(Instruction inst, MachineState state)
        {
            _currentSynthIndex = (int)state.GetRegister(inst.Arg0);
            _executor.EmitEvent(new GameEvent(
                MidiEventType.SynthChange,
                _currentSynthIndex,
                0,
                GetSimulationTime()));
        }

        void ExecuteSetBpm(Instruction inst, MachineState state)
        {
            _bpm = Mathf.Clamp(state.GetRegister(inst.Arg0), 20f, 300f);
            _executor.EmitEvent(new GameEvent(
                MidiEventType.BpmChange,
                _bpm,
                0,
                GetSimulationTime()));
        }

        void ExecuteSetVolume(Instruction inst, MachineState state)
        {
            _volume = Mathf.Clamp01(state.GetRegister(inst.Arg0));
            _executor.EmitEvent(new GameEvent(
                MidiEventType.VolumeChange,
                _volume,
                0,
                GetSimulationTime()));
        }

        void ExecuteUseFx(Instruction inst, MachineState state)
        {
            _currentFxIndex = (int)state.GetRegister(inst.Arg0);
            _currentFxMix = Mathf.Clamp01(state.ReadMemory(inst.Arg1));

            // Pack fx index and mix into event
            _executor.EmitEvent(new GameEvent(
                MidiEventType.FxChange,
                _currentFxIndex,
                (int)(_currentFxMix * 100f),
                GetSimulationTime()));
        }
    }
}
