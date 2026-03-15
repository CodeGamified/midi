// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using CodeGamified.Engine;

namespace MidiGame
{
    /// <summary>
    /// MIDI-specific opcodes mapped to CUSTOM_0..CUSTOM_7.
    /// Used by MidiCompilerExtension to emit instructions
    /// and MidiIOHandler to execute them.
    /// </summary>
    public static class MidiOpCode
    {
        /// <summary>Play a MIDI note. Arg0=noteReg, Arg1=velocityAddr, Arg2=durationAddr. Tag=channel.</summary>
        public const OpCode PLAY = OpCode.CUSTOM_0;

        /// <summary>Trigger a named sample. Arg0=sampleStringIndex, Arg1=velocityAddr.</summary>
        public const OpCode SAMPLE = OpCode.CUSTOM_1;

        /// <summary>Change synth. Arg0=synthStringIndex.</summary>
        public const OpCode USE_SYNTH = OpCode.CUSTOM_2;

        /// <summary>Set BPM. Arg0=bpmReg.</summary>
        public const OpCode SET_BPM = OpCode.CUSTOM_3;

        /// <summary>Set volume (0-1). Arg0=volReg.</summary>
        public const OpCode SET_VOLUME = OpCode.CUSTOM_4;

        /// <summary>Apply effect. Arg0=fxStringIndex, Arg1=mixReg.</summary>
        public const OpCode USE_FX = OpCode.CUSTOM_5;

        /// <summary>Stop all notes on channel. Tag=channel.</summary>
        public const OpCode STOP_ALL = OpCode.CUSTOM_6;

        /// <summary>Note off. Arg0=noteReg. Tag=channel.</summary>
        public const OpCode NOTE_OFF = OpCode.CUSTOM_7;
    }

    /// <summary>
    /// MIDI event types for GameEvent.Type.
    /// </summary>
    public static class MidiEventType
    {
        public const int NoteOn = 1;
        public const int NoteOff = 2;
        public const int SampleTrigger = 3;
        public const int SynthChange = 4;
        public const int BpmChange = 5;
        public const int VolumeChange = 6;
        public const int FxChange = 7;
    }
}
