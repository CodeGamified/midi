// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using CodeGamified.Editor;

namespace MidiGame
{
    /// <summary>
    /// Editor extension for the MIDI Code Game.
    /// Provides the option tree with music-specific functions, note pickers,
    /// synth names, sample names, and domain variable name suggestions.
    ///
    /// Tier progression:
    ///   Tier 1 — play(), sleep()
    ///   Tier 2 — while True loops, set_bpm(), set_volume()
    ///   Tier 3 — for loops, variables, sample()
    ///   Tier 4 — if/conditionals, use_synth()
    ///   Tier 5 — use_fx(), expressions
    /// </summary>
    public class MidiEditorExtension : IEditorExtension
    {
        int _currentTier;

        public MidiEditorExtension(int tier = 1)
        {
            _currentTier = tier;
        }

        public void SetTier(int tier) => _currentTier = tier;

        // ═══════════════════════════════════════════════════════════
        //  TYPES (future: Synth, Sampler objects)
        // ═══════════════════════════════════════════════════════════

        public List<EditorTypeInfo> GetAvailableTypes() => new();

        // ═══════════════════════════════════════════════════════════
        //  FUNCTIONS
        // ═══════════════════════════════════════════════════════════

        public List<EditorFuncInfo> GetAvailableFunctions()
        {
            var funcs = new List<EditorFuncInfo>();

            // Tier 1: core
            funcs.Add(new EditorFuncInfo { Name = "play", Hint = "play(note)", ArgCount = 1 });
            funcs.Add(new EditorFuncInfo { Name = "sleep", Hint = "sleep(beats)", ArgCount = 1 });

            // Tier 2: tempo/volume
            if (_currentTier >= 2)
            {
                funcs.Add(new EditorFuncInfo { Name = "set_bpm", Hint = "set_bpm(120)", ArgCount = 1 });
                funcs.Add(new EditorFuncInfo { Name = "set_volume", Hint = "set_volume(0.8)", ArgCount = 1 });
            }

            // Tier 3: samples
            if (_currentTier >= 3)
            {
                funcs.Add(new EditorFuncInfo { Name = "sample", Hint = "sample(name)", ArgCount = 1 });
                funcs.Add(new EditorFuncInfo { Name = "stop", Hint = "stop all", ArgCount = 0 });
            }

            // Tier 5: effects
            if (_currentTier >= 5)
            {
                funcs.Add(new EditorFuncInfo { Name = "use_synth", Hint = "use_synth(name)", ArgCount = 1 });
                funcs.Add(new EditorFuncInfo { Name = "use_fx", Hint = "use_fx(name)", ArgCount = 1 });
            }

            return funcs;
        }

        // ═══════════════════════════════════════════════════════════
        //  METHODS (future)
        // ═══════════════════════════════════════════════════════════

        public List<EditorMethodInfo> GetMethodsForType(string typeName) => new();

        // ═══════════════════════════════════════════════════════════
        //  VARIABLE NAME SUGGESTIONS
        // ═══════════════════════════════════════════════════════════

        public List<string> GetVariableNameSuggestions() => new()
        {
            "note", "beat", "velocity", "duration",
            "pitch", "octave", "tempo", "vol",
            "i", "n", "step"
        };

        // ═══════════════════════════════════════════════════════════
        //  TIER GATING
        // ═══════════════════════════════════════════════════════════

        public bool IsWhileLoopAllowed() => _currentTier >= 2;
        public bool IsForLoopAllowed() => _currentTier >= 3;
        public string GetWhileLoopGateReason() => "unlocks at Tier 2 — Loops";
        public string GetForLoopGateReason() => "unlocks at Tier 3 — Sequences";

        // ═══════════════════════════════════════════════════════════
        //  STRING LITERAL SUGGESTIONS
        // ═══════════════════════════════════════════════════════════

        public List<string> GetStringLiteralSuggestions() => new()
        {
            // Synth names
            "sine", "square", "saw", "triangle", "piano", "pluck", "bass", "pad",
            // Sample names
            "kick", "snare", "hihat", "clap", "rim", "tom", "crash", "ride",
            "bass_drum", "open_hat", "closed_hat", "shaker",
            // Effect names
            "reverb", "delay", "echo", "distortion", "chorus", "flanger"
        };
    }
}
