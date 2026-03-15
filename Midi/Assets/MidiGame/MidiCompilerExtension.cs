// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;

namespace MidiGame
{
    /// <summary>
    /// Compiler extension for the MIDI Code Game.
    /// Maps Sonic-Pi-flavored Python calls to CUSTOM opcodes:
    ///   play(note)            → PLAY
    ///   play(note, vel, dur)  → PLAY with velocity + duration
    ///   sleep(seconds)        → WAIT (engine built-in, handled by CallNode)
    ///   sample("name")        → SAMPLE
    ///   use_synth("name")     → USE_SYNTH
    ///   set_bpm(n)            → SET_BPM
    ///   set_volume(v)         → SET_VOLUME
    ///   use_fx("name", mix)   → USE_FX
    ///   stop()                → STOP_ALL
    /// </summary>
    public class MidiCompilerExtension : ICompilerExtension
    {
        // ═══════════════════════════════════════════════════════════
        //  REGISTRATION
        // ═══════════════════════════════════════════════════════════

        public void RegisterBuiltins(CompilerContext ctx)
        {
            // Register known synth types as object-like declarations (not required for MVP,
            // but allows future `Synth s = new Synth("piano")` syntax).
            ctx.KnownTypes.Add("Synth");
            ctx.KnownTypes.Add("Sampler");
        }

        // ═══════════════════════════════════════════════════════════
        //  FUNCTION CALLS
        // ═══════════════════════════════════════════════════════════

        public bool TryCompileCall(string functionName, List<AstNodes.ExprNode> args,
                                   CompilerContext ctx, int sourceLine)
        {
            switch (functionName)
            {
                case "play":
                    return CompilePlay(args, ctx, sourceLine);

                case "sample":
                    return CompileSample(args, ctx, sourceLine);

                case "use_synth":
                    return CompileUseSynth(args, ctx, sourceLine);

                case "set_bpm":
                    return CompileSetBpm(args, ctx, sourceLine);

                case "set_volume":
                    return CompileSetVolume(args, ctx, sourceLine);

                case "use_fx":
                    return CompileUseFx(args, ctx, sourceLine);

                case "stop":
                    ctx.Emit(MidiOpCode.STOP_ALL, sourceLine: sourceLine, comment: "stop all notes");
                    return true;

                // sleep/wait is handled by engine's CallNode default case
                default:
                    return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  METHOD CALLS (future: synth.play(), sampler.trigger())
        // ═══════════════════════════════════════════════════════════

        public bool TryCompileMethodCall(string objectName, string methodName,
                                         List<AstNodes.ExprNode> args,
                                         CompilerContext ctx, int sourceLine)
        {
            // MVP: no object methods — all via top-level functions
            return false;
        }

        public bool TryCompileObjectDecl(string typeName, string varName,
                                         List<AstNodes.ExprNode> constructorArgs,
                                         CompilerContext ctx, int sourceLine)
        {
            // MVP: no object declarations
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        //  TIER GATING
        // ═══════════════════════════════════════════════════════════

        public void OnWhileLoop(CompilerContext ctx, int sourceLine) { }
        public void OnForLoop(CompilerContext ctx, int sourceLine) { }

        // ═══════════════════════════════════════════════════════════
        //  COMPILE HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// play(note) — single arg: note in R0, default velocity + duration
        /// play(note, velocity) — two args
        /// play(note, velocity, duration) — three args
        /// </summary>
        bool CompilePlay(List<AstNodes.ExprNode> args, CompilerContext ctx, int sourceLine)
        {
            if (args.Count < 1)
            {
                ctx.Error(sourceLine, "play() requires at least 1 argument (note)");
                return true;
            }

            // Note → R0
            args[0].Compile(ctx, 0);

            // Velocity → memory slot (default 100)
            int velAddr = ctx.GetVariableAddress("_play_vel");
            if (args.Count >= 2)
            {
                args[1].Compile(ctx, 1);
                ctx.Emit(OpCode.STORE_MEM, 1, velAddr, sourceLine: sourceLine, comment: "store velocity");
            }
            else
            {
                int defaultVel = ctx.AddFloatConstant(100f);
                ctx.Emit(OpCode.LOAD_FLOAT, 1, defaultVel, sourceLine: sourceLine, comment: "default velocity 100");
                ctx.Emit(OpCode.STORE_MEM, 1, velAddr, sourceLine: sourceLine, comment: "store velocity");
            }

            // Duration → memory slot (default 0.25)
            int durAddr = ctx.GetVariableAddress("_play_dur");
            if (args.Count >= 3)
            {
                args[2].Compile(ctx, 1);
                ctx.Emit(OpCode.STORE_MEM, 1, durAddr, sourceLine: sourceLine, comment: "store duration");
            }
            else
            {
                int defaultDur = ctx.AddFloatConstant(0.25f);
                ctx.Emit(OpCode.LOAD_FLOAT, 1, defaultDur, sourceLine: sourceLine, comment: "default duration 0.25");
                ctx.Emit(OpCode.STORE_MEM, 1, durAddr, sourceLine: sourceLine, comment: "store duration");
            }

            ctx.Emit(MidiOpCode.PLAY, 0, velAddr, durAddr,
                sourceLine: sourceLine, comment: $"play note");
            return true;
        }

        /// <summary>sample("kick") or sample("kick", velocity)</summary>
        bool CompileSample(List<AstNodes.ExprNode> args, CompilerContext ctx, int sourceLine)
        {
            if (args.Count < 1)
            {
                ctx.Error(sourceLine, "sample() requires at least 1 argument (name)");
                return true;
            }

            // Sample name string index (via StringNode → float encoding of string index)
            args[0].Compile(ctx, 0);

            int velAddr = ctx.GetVariableAddress("_sample_vel");
            if (args.Count >= 2)
            {
                args[1].Compile(ctx, 1);
                ctx.Emit(OpCode.STORE_MEM, 1, velAddr, sourceLine: sourceLine, comment: "store sample velocity");
            }
            else
            {
                int defaultVel = ctx.AddFloatConstant(100f);
                ctx.Emit(OpCode.LOAD_FLOAT, 1, defaultVel, sourceLine: sourceLine, comment: "default sample velocity");
                ctx.Emit(OpCode.STORE_MEM, 1, velAddr, sourceLine: sourceLine, comment: "store sample velocity");
            }

            ctx.Emit(MidiOpCode.SAMPLE, 0, velAddr, sourceLine: sourceLine,
                comment: "trigger sample");
            return true;
        }

        /// <summary>use_synth("piano")</summary>
        bool CompileUseSynth(List<AstNodes.ExprNode> args, CompilerContext ctx, int sourceLine)
        {
            if (args.Count < 1)
            {
                ctx.Error(sourceLine, "use_synth() requires 1 argument (synth name)");
                return true;
            }
            args[0].Compile(ctx, 0);
            ctx.Emit(MidiOpCode.USE_SYNTH, 0, sourceLine: sourceLine,
                comment: "change synth");
            return true;
        }

        /// <summary>set_bpm(120)</summary>
        bool CompileSetBpm(List<AstNodes.ExprNode> args, CompilerContext ctx, int sourceLine)
        {
            if (args.Count < 1)
            {
                ctx.Error(sourceLine, "set_bpm() requires 1 argument");
                return true;
            }
            args[0].Compile(ctx, 0);
            ctx.Emit(MidiOpCode.SET_BPM, 0, sourceLine: sourceLine,
                comment: "set BPM");
            return true;
        }

        /// <summary>set_volume(0.8)</summary>
        bool CompileSetVolume(List<AstNodes.ExprNode> args, CompilerContext ctx, int sourceLine)
        {
            if (args.Count < 1)
            {
                ctx.Error(sourceLine, "set_volume() requires 1 argument (0.0 - 1.0)");
                return true;
            }
            args[0].Compile(ctx, 0);
            ctx.Emit(MidiOpCode.SET_VOLUME, 0, sourceLine: sourceLine,
                comment: "set volume");
            return true;
        }

        /// <summary>use_fx("reverb") or use_fx("reverb", 0.5)</summary>
        bool CompileUseFx(List<AstNodes.ExprNode> args, CompilerContext ctx, int sourceLine)
        {
            if (args.Count < 1)
            {
                ctx.Error(sourceLine, "use_fx() requires at least 1 argument (effect name)");
                return true;
            }
            args[0].Compile(ctx, 0);

            int mixAddr = ctx.GetVariableAddress("_fx_mix");
            if (args.Count >= 2)
            {
                args[1].Compile(ctx, 1);
                ctx.Emit(OpCode.STORE_MEM, 1, mixAddr, sourceLine: sourceLine, comment: "store fx mix");
            }
            else
            {
                int defaultMix = ctx.AddFloatConstant(0.5f);
                ctx.Emit(OpCode.LOAD_FLOAT, 1, defaultMix, sourceLine: sourceLine, comment: "default fx mix 0.5");
                ctx.Emit(OpCode.STORE_MEM, 1, mixAddr, sourceLine: sourceLine, comment: "store fx mix");
            }

            ctx.Emit(MidiOpCode.USE_FX, 0, mixAddr, sourceLine: sourceLine,
                comment: "apply effect");
            return true;
        }
    }
}
