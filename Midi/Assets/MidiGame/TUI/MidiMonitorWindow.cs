// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.TUI;
using CodeGamified.Engine;
using MidiGame.Audio;

namespace MidiGame.TUI
{
    /// <summary>
    /// Live piano-roll visualizer for the MIDI Code Game.
    /// Shows recent notes as a scrolling grid + current state.
    ///
    /// Layout:
    /// ┌─────────────────────────────────────┐
    /// │ ♪━━ MIDI MONITOR ━━━━━━━━━━━━━━━◆  │  header
    /// │ BPM: 120  VOL: 80%  SYNTH: sine    │  status bar
    /// │ ├── separator ──────────────────    │
    /// │  C5│ ·  ·  ■  ·  ·  ·  ·  ·       │  piano roll
    /// │  B4│ ·  ·  ·  ·  ·  ·  ·  ·       │    (scrolling)
    /// │  A4│ ·  ■  ·  ·  ·  ·  ■  ·       │
    /// │  G4│ ·  ·  ·  ·  ■  ·  ·  ·       │
    /// │  F4│ ·  ·  ·  ·  ·  ·  ·  ·       │
    /// │  E4│ ·  ·  ·  ■  ·  ·  ·  ·       │
    /// │  D4│ ·  ·  ·  ·  ·  ·  ·  ·       │
    /// │  C4│ ■  ·  ·  ·  ·  ■  ·  ·       │
    /// │ ├── separator ──────────────────    │
    /// │  ► Playing | Notes: 12 | Beat: 3.5 │  footer
    /// └─────────────────────────────────────┘
    /// </summary>
    public class MidiMonitorWindow : TerminalWindow
    {
        // ── Note history (circular buffer) ──────────────────────
        const int HISTORY_SIZE = 32;
        readonly NoteEvent[] _history = new NoteEvent[HISTORY_SIZE];
        int _historyHead;
        int _noteCount;

        // ── Display range ───────────────────────────────────────
        int _lowNote = 60;  // C4
        int _highNote = 72; // C5

        // ── State from IO handler ───────────────────────────────
        float _bpm = 120f;
        float _volume = 0.8f;
        string _synthName = "sine";
        bool _isPlaying;
        float _beatCounter;

        struct NoteEvent
        {
            public int MidiNote;
            public float Velocity;
            public double Time;
        }

        // ═══════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "MIDI MONITOR";
            totalRows = 18;
        }

        // ═══════════════════════════════════════════════════════════
        //  PUBLIC API — called by MidiGameBootstrap
        // ═══════════════════════════════════════════════════════════

        /// <summary>Feed a GameEvent from CodeExecutor.OnOutput.</summary>
        public void HandleEvent(GameEvent evt, CompiledProgram program)
        {
            switch (evt.Type)
            {
                case MidiEventType.NoteOn:
                    int note = Mathf.RoundToInt(evt.Value);
                    _history[_historyHead] = new NoteEvent
                    {
                        MidiNote = note,
                        Velocity = evt.Channel,
                        Time = evt.SimulationTime
                    };
                    _historyHead = (_historyHead + 1) % HISTORY_SIZE;
                    _noteCount++;

                    // Auto-expand display range
                    if (note < _lowNote) _lowNote = Mathf.Max(0, note - 2);
                    if (note > _highNote) _highNote = Mathf.Min(127, note + 2);
                    break;

                case MidiEventType.BpmChange:
                    _bpm = evt.Value;
                    break;

                case MidiEventType.VolumeChange:
                    _volume = evt.Value;
                    break;

                case MidiEventType.SynthChange:
                    int idx = Mathf.RoundToInt(evt.Value);
                    if (program?.StringConstants != null && idx >= 0 && idx < program.StringConstants.Length)
                        _synthName = program.StringConstants[idx];
                    break;
            }
        }

        public void SetPlaying(bool playing) => _isPlaying = playing;
        public void SetBeatCounter(float beat) => _beatCounter = beat;

        // ═══════════════════════════════════════════════════════════
        //  RENDER
        // ═══════════════════════════════════════════════════════════

        protected override void Render()
        {
            if (!rowsReady) return;

            // Header
            RenderMidiHeader();

            // Status bar (row 1)
            string bpmStr = $"BPM:{_bpm:F0}";
            string volStr = $"VOL:{(_volume * 100):F0}%";
            string synStr = $"SYN:{_synthName}";
            SetRow(ROW_SUBTITLE, $" {bpmStr}  {volStr}  {synStr}");

            // Separator
            WriteSeparator(ROW_SEP_TOP);

            // Piano roll
            int visibleNotes = Mathf.Min(_highNote - _lowNote + 1, ContentRows);
            int rollStart = ROW_CONTENT_START;
            int gridWidth = Mathf.Max(1, totalChars - 5); // 4 chars for note label + separator

            for (int row = 0; row < ContentRows; row++)
            {
                int rowIdx = rollStart + row;
                if (row >= visibleNotes)
                {
                    SetRow(rowIdx, "");
                    continue;
                }

                int midiNote = _highNote - row;
                string label = MidiAudioProvider.MidiNoteToName(midiNote);
                // Pad label to 3 chars
                while (label.Length < 3) label = " " + label;

                string line = $"{label}{TUIGlyphs.BoxV}";

                // Build grid cells — recent notes light up
                int stepsToShow = Mathf.Min(gridWidth / 2, HISTORY_SIZE);
                for (int step = 0; step < stepsToShow; step++)
                {
                    int histIdx = (_historyHead - stepsToShow + step + HISTORY_SIZE) % HISTORY_SIZE;
                    bool active = _history[histIdx].MidiNote == midiNote &&
                                  _history[histIdx].Velocity > 0;

                    if (step < _noteCount || step < stepsToShow)
                    {
                        line += active
                            ? (IsBlackKey(midiNote) ? "■ " : "█ ")
                            : (IsBlackKey(midiNote) ? "· " : ". ");
                    }
                }

                SetRow(rowIdx, line);
            }

            // Bottom separator
            WriteSeparator(RowSepBot);

            // Footer
            string status = _isPlaying ? "► Playing" : "■ Stopped";
            string noteInfo = $"Notes:{_noteCount}";
            string beatInfo = $"Beat:{_beatCounter:F1}";
            SetRow(RowActions, $" {status} {TUIGlyphs.BoxV} {noteInfo} {TUIGlyphs.BoxV} {beatInfo}");
        }

        void RenderMidiHeader()
        {
            string title = "♪━━ MIDI MONITOR ";
            int pad = Mathf.Max(0, totalChars - title.Length - 1);
            SetRow(ROW_HEADER, title + new string('━', pad) + "◆");
        }

        void WriteSeparator(int row)
        {
            SetRow(row, new string('─', totalChars));
        }

        static bool IsBlackKey(int midiNote)
        {
            int n = midiNote % 12;
            return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
        }
    }
}
