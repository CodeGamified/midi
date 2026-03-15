// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using UnityEngine;
using CodeGamified.TUI;
using CodeGamified.Time;

namespace MidiGame.TUI
{
    /// <summary>
    /// MIDI status bar — TerminalWindow showing transport controls, BPM, volume,
    /// time scale, and keybinds.
    ///
    /// Layout:
    /// ┌──────────────────────────────────────────────────────┐
    /// │ ♪━━ MIDI ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━◆  │
    /// │ BPM:120  VOL:80%  SYN:sine  ×1.0  Bar 1:1.0       │
    /// │ ├─────────────────────────────────────────────────   │
    /// │ [F1-F4] Samples  [Tab] Next  [+/-] Volume          │
    /// │ [F5/F6] Font     [F9] Save   [Space] Pause         │
    /// │ [</=/>] Time ×   [F10] Reset                       │
    /// └──────────────────────────────────────────────────────┘
    /// </summary>
    public class MidiStatusBar : TerminalWindow
    {
        float _bpm = 120f;
        float _volume = 0.8f;
        string _synthName = "sine";
        bool _isPlaying;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "MIDI";
            totalRows = 8;
        }

        public void SetBPM(float bpm) => _bpm = bpm;
        public void SetVolume(float vol) => _volume = vol;
        public void SetSynthName(string name) => _synthName = name ?? "sine";
        public void SetPlaying(bool playing) => _isPlaying = playing;

        protected override void Render()
        {
            if (!rowsReady) return;

            // Row 0: header (handled by base)

            // Row 1: status line
            string timeScale = SimulationTime.Instance != null
                ? $"×{SimulationTime.Instance.timeScale:F1}"
                : "×1.0";
            string formattedTime = SimulationTime.Instance != null
                ? SimulationTime.Instance.GetFormattedTime()
                : "1:1.0";
            string transport = _isPlaying
                ? TUIColors.Fg(TUIColors.BrightGreen, "▶")
                : TUIColors.Fg(TUIColors.BrightYellow, "■");

            string status = $" {transport} BPM:{_bpm:F0}  VOL:{(_volume * 100):F0}%  SYN:{_synthName}  {timeScale}  {formattedTime}";
            SetRow(ROW_SUBTITLE, status);

            // Row 2: separator
            SetRow(ROW_SEP_TOP, Separator());

            // Rows 3+: keybinds
            int row = ROW_CONTENT_START;
            SetRow(row++, " [F1-F4] Samples  [Tab] Next  [+/-] Volume");
            SetRow(row++, " [F5/F6] Font     [F9] Save   [Space] Pause");
            SetRow(row++, " [</=/>] Time ×   [F10] Reset defaults");

            // Clear remaining rows
            for (int i = row; i < totalRows; i++)
                SetRow(i, "");
        }
    }
}
