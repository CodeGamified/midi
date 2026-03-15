// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Runtime;
using CodeGamified.TUI;

namespace MidiGame.TUI
{
    /// <summary>
    /// Code debugger for MIDI — shows SOURCE │ MACHINE │ REGISTERS for the active program.
    /// Extends the engine's CodeDebuggerWindow (same pattern as PongCodeDebugger).
    /// </summary>
    public class MidiCodeDebugger : CodeDebuggerWindow
    {
        MidiProgramBehaviour _program;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CODE";
        }

        public void SetTitle(string title)
        {
            windowTitle = title;
        }

        public void Bind(MidiProgramBehaviour program)
        {
            _program = program;
        }

        protected override string[] GetSourceLines()
        {
            return _program?.Program?.SourceLines;
        }

        protected override string GetProgramName()
        {
            return _program?.ProgramName ?? "my_song";
        }

        protected override bool HasLiveProgram =>
            _program != null && _program.Executor != null && _program.Program != null
            && _program.Program.Instructions != null && _program.Program.Instructions.Length > 0;

        protected override int GetPC() =>
            _program?.State?.PC ?? 0;

        protected override long GetCycleCount() =>
            _program?.State?.CycleCount ?? 0;

        protected override string GetStatusString()
        {
            if (_program == null || _program.Executor == null)
                return TUIColors.Dimmed("NO PROGRAM");

            var state = _program.State;
            if (state == null) return TUIColors.Dimmed("NO STATE");

            if (state.IsHalted)
                return TUIColors.Fg(TUIColors.BrightYellow, "HALTED");
            if (state.IsWaiting)
                return TUIColors.Fg(TUIColors.BrightCyan, $"WAIT {state.WaitTimeRemaining:F2}s");

            int instCount = _program.Program?.Instructions?.Length ?? 0;
            return TUIColors.Fg(TUIColors.BrightGreen, $"RUN {instCount} inst");
        }

        protected override List<string> BuildSourceColumn(int pc)
        {
            var lines = new List<string>();
            var src = GetSourceLines();
            if (src == null) return lines;

            int activeLine = -1;
            if (HasLiveProgram && _program.Program.Instructions.Length > 0 && pc < _program.Program.Instructions.Length)
                activeLine = _program.Program.Instructions[pc].SourceLine - 1;

            for (int i = scrollOffset; i < src.Length && lines.Count < ContentRows; i++)
            {
                bool isActive = (i == activeLine);
                string prefix = isActive
                    ? TUIColors.Fg(TUIColors.BrightGreen, TUIGlyphs.ArrowR)
                    : " ";
                string num = TUIColors.Dimmed($"{i + 1,3}");
                string text = isActive
                    ? TUIColors.Fg(TUIColors.BrightGreen, src[i])
                    : src[i];
                lines.Add($"{prefix}{num} {text}");
            }
            return lines;
        }

        protected override List<string> BuildAsmColumn(int pc)
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var instructions = _program.Program.Instructions;
            int start = Mathf.Max(0, pc - ContentRows / 3);

            for (int i = start; i < instructions.Length && lines.Count < ContentRows; i++)
            {
                var inst = instructions[i];
                bool isPC = (i == pc);
                string prefix = isPC
                    ? TUIColors.Fg(TUIColors.BrightCyan, TUIGlyphs.ArrowR)
                    : " ";
                string addr = TUIColors.Dimmed($"{i:D4}:");

                string opName = inst.Op.ToString();
#if UNITY_EDITOR || DEVELOPMENT_BUILD || CODEGAMIFIED_DEBUG
                string comment = inst.Comment ?? opName;
#else
                string comment = opName;
#endif
                string text = isPC
                    ? TUIColors.Fg(TUIColors.BrightCyan, comment)
                    : comment;
                lines.Add($"{prefix}{addr} {text}");
            }
            return lines;
        }

        protected override List<string> BuildStateColumn()
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var state = _program.State;

            // Registers
            for (int r = 0; r < MachineState.REGISTER_COUNT; r++)
            {
                bool modified = (r == state.LastRegisterModified);
                string rName = $"R{r}:";
                string rVal = $"{state.Registers[r]:F2}";
                if (modified)
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {rName,-4} {rVal}"));
                else
                    lines.Add($" {TUIColors.Dimmed(rName),-4} {rVal}");
            }

            lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 2 : 16));

            // Flags
            lines.Add($" FLAGS: {state.Flags}");
            lines.Add($" PC: {state.PC}");
            lines.Add($" STACK [{state.Stack.Count}]");

            // Variables
            if (state.NameToAddress.Count > 0)
            {
                lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 2 : 16));
                lines.Add(TUIColors.Fg(TUIColors.BrightCyan, " VARIABLES"));
                foreach (var kvp in state.NameToAddress)
                {
                    string name = kvp.Key;
                    float val = 0;
                    if (state.Memory.ContainsKey(name))
                        val = state.Memory[name];
                    lines.Add($" {TUIColors.Dimmed(name + ":")} {val:F2}");
                }
            }

            return lines;
        }
    }
}
