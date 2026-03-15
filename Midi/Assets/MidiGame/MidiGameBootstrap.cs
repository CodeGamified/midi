// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using CodeGamified.Audio;
using CodeGamified.Bootstrap;
using CodeGamified.Camera;
using CodeGamified.Editor;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Persistence;
using CodeGamified.Persistence.Providers;
using CodeGamified.Procedural;
using CodeGamified.Quality;
using CodeGamified.Settings;
using CodeGamified.Time;
using MidiGame.Audio;
using MidiGame.Persistence;
using MidiGame.Procedural;
using MidiGame.TUI;

namespace MidiGame
{
    /// <summary>
    /// Bootstrap for MIDI — the music coding game.
    ///
    /// Architecture (same pattern as Pong / SeaRäuber / BitNaughts):
    ///   - Extends GameBootstrap for shared logging, camera, SimulationTime
    ///   - .engine submodule gives us TUI + Code Execution + Persistence
    ///   - Players WRITE CODE to compose music (Sonic Pi–style)
    ///
    ///   MidiGameBootstrap
    ///   ├─ MidiCanvas (Canvas + CanvasScaler + GraphicRaycaster)
    ///   │  ├─ EditorPanel (Image + CodeEditorWindow)
    ///   │  ├─ MonitorPanel (Image + MidiMonitorWindow)
    ///   │  ├─ DebuggerPanel (Image + MidiCodeDebugger)
    ///   │  └─ StatusBarPanel (Image + MidiStatusBar)
    ///   ├─ MidiExecutor (MidiProgramBehaviour)
    ///   ├─ SimulationTime (MidiSimulationTime)
    ///   ├─ Persistence (MidiPersistenceManager)
    ///   └─ EventSystem (if none exists)
    /// </summary>
    public class MidiGameBootstrap : GameBootstrap, ISettingsListener
    {
        protected override string LogTag => "MIDI";

        [Header("Progression")]
        [SerializeField] int _startingTier = 1;

        [Header("Layout")]
        [SerializeField] float _editorWidthRatio = 0.5f;
        [SerializeField] float _debuggerHeightRatio = 0.35f;

        [Header("Persistence")]
        [Tooltip("Enable composition persistence (saves to .data/)")]
        public bool enablePersistence = true;

        [Tooltip("Use local git repo instead of in-memory (survives restarts)")]
        public bool useLocalGitProvider = false;

        [Tooltip("Path to .data/ directory (relative to project root)")]
        public string dataPath = "Assets/.data";

        [Header("Piano")]
        [Tooltip("Lowest MIDI note on the keyboard")]
        public int pianoLowNote = 48;

        [Tooltip("Highest MIDI note on the keyboard")]
        public int pianoHighNote = 84;

        [Header("Camera")]
        public bool configureCamera = true;

        // ── Created at runtime — all visible in hierarchy ───────
        Canvas _canvas;
        CodeEditorWindow _editor;
        MidiMonitorWindow _monitor;
        MidiCodeDebugger _debugger;
        MidiStatusBar _statusBar;
        MidiProgramBehaviour _programBehaviour;

        // ── Shared instances ────────────────────────────────────
        MidiAudioProvider _audioProvider;
        MidiHapticProvider _hapticProvider;
        MidiCompilerExtension _compilerExt;
        MidiEditorExtension _editorExt;

        // ── Persistence ─────────────────────────────────────────
        MidiPersistenceManager _persistence;

        // ── Procedural (3D piano) ──────────────────────────────
        ColorPalette _palette;
        MidiPianoBlueprint _pianoBlueprint;
        MidiPianoController _pianoController;
        CameraAmbientMotion _cameraSway;

        // ── Audio / haptic bridges ──────────────────────────────
        AudioBridge.EditorHandlers _editorAudio;
        AudioBridge.EngineHandlers _engineAudio;
        HapticBridge.EditorHandlers _editorHaptic;

        // ── Public accessors ────────────────────────────────────
        public CodeEditorWindow Editor => _editor;
        public MidiMonitorWindow Monitor => _monitor;
        public MidiCodeDebugger Debugger => _debugger;
        public MidiProgramBehaviour ProgramBehaviour => _programBehaviour;
        public MidiAudioProvider AudioProvider => _audioProvider;

        // ═══════════════════════════════════════════════════════════
        //  SAMPLE PROGRAMS
        // ═══════════════════════════════════════════════════════════

        public enum SampleLevel { Simple, Medium, Advanced, Pro }

        /// <summary>Current sample loaded in the editor.</summary>
        SampleLevel _currentSample = SampleLevel.Simple;

        static string GetSample(SampleLevel level) => level switch
        {
            SampleLevel.Simple => Simple,
            SampleLevel.Medium => Medium,
            SampleLevel.Advanced => Advanced,
            SampleLevel.Pro => Pro,
            _ => Simple
        };

        // ── Simple: Tier 1 ──────────────────────────────────────
        // play() + sleep() only — C major triad, one note at a time
        const string Simple =
            "# C major triad\n" +
            "play(60)\n" +
            "sleep(0.5)\n" +
            "play(64)\n" +
            "sleep(0.5)\n" +
            "play(67)\n" +
            "sleep(0.5)\n" +
            "play(72)\n" +
            "sleep(1)\n";

        // ── Medium: Tier 2 ──────────────────────────────────────
        // Adds while loop + set_bpm + set_volume — ascending scale
        const string Medium =
            "# Ascending scale — loops back\n" +
            "set_bpm(120)\n" +
            "set_volume(0.8)\n" +
            "note = 60\n" +
            "while True:\n" +
            "    play(note)\n" +
            "    sleep(0.25)\n" +
            "    note = note + 1\n" +
            "    if note > 72:\n" +
            "        note = 60\n";

        // ── Advanced: Tier 3 ────────────────────────────────────
        // for loop + sample() + conditional rhythm pattern
        const string Advanced =
            "# Drum + melody pattern\n" +
            "set_bpm(140)\n" +
            "step = 0\n" +
            "while True:\n" +
            "    sample(\"kick\")\n" +
            "    sleep(0.25)\n" +
            "    if step == 2:\n" +
            "        sample(\"snare\")\n" +
            "        sleep(0.25)\n" +
            "    else:\n" +
            "        sample(\"hihat\")\n" +
            "        sleep(0.25)\n" +
            "    for i in range(4):\n" +
            "        play(60 + i * 2)\n" +
            "        sleep(0.125)\n" +
            "    step = step + 1\n" +
            "    if step > 3:\n" +
            "        step = 0\n";

        // ── Pro: Tier 5 ─────────────────────────────────────────
        // Full feature set — synth switching, fx, velocity, nested loops
        const string Pro =
            "# Synth layers + effects\n" +
            "set_bpm(100)\n" +
            "set_volume(0.7)\n" +
            "use_fx(\"reverb\")\n" +
            "beat = 0\n" +
            "while True:\n" +
            "    use_synth(\"bass\")\n" +
            "    play(36)\n" +
            "    sleep(0.5)\n" +
            "    use_synth(\"sine\")\n" +
            "    for i in range(4):\n" +
            "        note = 60 + i * 3\n" +
            "        if note > 72:\n" +
            "            note = note - 12\n" +
            "        velocity = 80 + i * 10\n" +
            "        play(note, velocity)\n" +
            "        sleep(0.125)\n" +
            "    if beat == 0:\n" +
            "        sample(\"kick\")\n" +
            "    elif beat == 2:\n" +
            "        sample(\"snare\")\n" +
            "    else:\n" +
            "        sample(\"hihat\")\n" +
            "    sleep(0.25)\n" +
            "    use_synth(\"pluck\")\n" +
            "    for i in range(3):\n" +
            "        play(72 - i * 4)\n" +
            "        sleep(0.125)\n" +
            "    beat = beat + 1\n" +
            "    if beat > 3:\n" +
            "        beat = 0\n";

        /// <summary>
        /// Load a sample program into the editor.
        /// Automatically sets the tier to match the sample's complexity.
        /// </summary>
        public void LoadSample(SampleLevel level)
        {
            _currentSample = level;

            // Auto-set tier so the editor unlocks the right constructs
            int tier = level switch
            {
                SampleLevel.Simple   => 1,
                SampleLevel.Medium   => 2,
                SampleLevel.Advanced => 3,
                SampleLevel.Pro      => 5,
                _ => 1
            };
            _editorExt.SetTier(tier);
            _startingTier = tier;

            string source = GetSample(level);
            string name = $"sample_{level.ToString().ToLower()}";
            _editor.OpenSource(source, name, _compilerExt, _editorExt);

            Log($"Loaded sample: {level} (tier {tier})");

            // Auto-compile and run
            AutoRun(source);
        }

        /// <summary>Cycle to the next sample level.</summary>
        public void NextSample()
        {
            int next = ((int)_currentSample + 1) % 4;
            LoadSample((SampleLevel)next);
        }

        // ═══════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        void Awake()
        {
            Log("♪ MIDI Bootstrap starting...");

            // ── Load persisted settings (volume, font, quality) ─
            SettingsBridge.Load();
            QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);
            Log($"Settings loaded (Quality={SettingsBridge.QualityLevel}, Font={SettingsBridge.FontSize}pt)");

            // ── Simulation time (.engine) ───────────────────────
            EnsureSimulationTime<MidiSimulationTime>();
            System.Func<float> getTimeScale = () => SimulationTime.Instance?.timeScale ?? 1f;

            // ── Providers ───────────────────────────────────────
            _audioProvider = new MidiAudioProvider();
            _hapticProvider = new MidiHapticProvider();

            // ── Extensions ──────────────────────────────────────
            _compilerExt = new MidiCompilerExtension();
            _editorExt = new MidiEditorExtension(_startingTier);

            // ── Audio / haptic bridges (use SimulationTime) ─────
            _editorAudio = AudioBridge.ForEditor(_audioProvider, getTimeScale);
            _engineAudio = AudioBridge.ForEngine(_audioProvider, getTimeScale);
            _editorHaptic = HapticBridge.ForEditor(_hapticProvider, getTimeScale);

            // ── Build scene hierarchy ───────────────────────────
            SetupCamera();
            CreatePalette();
            CreatePiano();
            EnsureEventSystem();
            CreateCanvas();
            CreateEditorPanel();
            CreateMonitorPanel();
            CreateDebuggerPanel();
            CreateStatusBarPanel();
            CreateExecutor();
            if (enablePersistence) CreatePersistence();
        }

        void Start()
        {
            // ── Wire audio into executor ────────────────────────
            _programBehaviour.SetAudioProvider(_audioProvider);

            // ── Bind debugger to executor ───────────────────────
            _debugger.Bind(_programBehaviour);

            // ── Load persisted composition or starting sample ───
            if (_persistence != null && _persistence.LoadedSource != null)
            {
                _editorExt.SetTier(_persistence.LoadedTier);
                _startingTier = _persistence.LoadedTier;
                _editor.OpenSource(_persistence.LoadedSource, _persistence.LoadedName,
                    _compilerExt, _editorExt);
                Log("Loaded persisted composition");
                AutoRun(_persistence.LoadedSource);
            }
            else
            {
                LoadSample(SampleLevel.Simple);
            }

            // ── Wire editor events → audio / haptic ─────────────
            WireEditorEvents();

            // ── Wire compile+run → executor ─────────────────────
            _editor.OnCompileAndRun += OnCompileAndRun;

            // ── Wire SimulationTime events ──────────────────────
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged += s => Log($"Time scale → {s:F0}x");
                SimulationTime.Instance.OnPausedChanged += p => Log(p ? "⏸ PAUSED" : "▶ RESUMED");
            }

            // ── Register for settings changes ───────────────────
            SettingsBridge.Register(this);

            Log("♪ MIDI Bootstrap complete");
        }

        void OnDestroy()
        {
            SettingsBridge.Unregister(this);
        }

        void WireEditorEvents()
        {
            _editor.OnOptionSelected += _editorAudio.OptionSelected;
            _editor.OnOptionSelected += _editorHaptic.OptionSelected;
            _editor.OnUndoPerformed += _editorAudio.UndoPerformed;
            _editor.OnUndoPerformed += _editorHaptic.UndoPerformed;
            _editor.OnRedoPerformed += _editorAudio.RedoPerformed;
            _editor.OnRedoPerformed += _editorHaptic.RedoPerformed;
            _editor.OnCompileError += _editorAudio.CompileError;
            _editor.OnCompileError += _editorHaptic.CompileError;
            _editor.OnDocumentChanged += _editorAudio.DocumentChanged;
            _editor.OnDocumentChanged += _editorHaptic.DocumentChanged;
        }

        void UnwireEditorEvents()
        {
            _editor.OnOptionSelected -= _editorAudio.OptionSelected;
            _editor.OnOptionSelected -= _editorHaptic.OptionSelected;
            _editor.OnUndoPerformed -= _editorAudio.UndoPerformed;
            _editor.OnUndoPerformed -= _editorHaptic.UndoPerformed;
            _editor.OnRedoPerformed -= _editorAudio.RedoPerformed;
            _editor.OnRedoPerformed -= _editorHaptic.RedoPerformed;
            _editor.OnCompileError -= _editorAudio.CompileError;
            _editor.OnCompileError -= _editorHaptic.CompileError;
            _editor.OnDocumentChanged -= _editorAudio.DocumentChanged;
            _editor.OnDocumentChanged -= _editorHaptic.DocumentChanged;
        }

        void Update()
        {
            // F1-F4: switch samples
            if (Input.GetKeyDown(KeyCode.F1)) LoadSample(SampleLevel.Simple);
            if (Input.GetKeyDown(KeyCode.F2)) LoadSample(SampleLevel.Medium);
            if (Input.GetKeyDown(KeyCode.F3)) LoadSample(SampleLevel.Advanced);
            if (Input.GetKeyDown(KeyCode.F4)) LoadSample(SampleLevel.Pro);
            // Tab: cycle through samples
            if (Input.GetKeyDown(KeyCode.Tab)) NextSample();

            // Volume controls: +/- master, Shift+/- SFX, Ctrl+/- music
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    SettingsBridge.SetSfxVolume(SettingsBridge.SfxVolume + 0.1f);
                else if (Input.GetKey(KeyCode.LeftControl))
                    SettingsBridge.SetMusicVolume(SettingsBridge.MusicVolume + 0.1f);
                else
                    SettingsBridge.SetMasterVolume(SettingsBridge.MasterVolume + 0.1f);
            }
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    SettingsBridge.SetSfxVolume(SettingsBridge.SfxVolume - 0.1f);
                else if (Input.GetKey(KeyCode.LeftControl))
                    SettingsBridge.SetMusicVolume(SettingsBridge.MusicVolume - 0.1f);
                else
                    SettingsBridge.SetMasterVolume(SettingsBridge.MasterVolume - 0.1f);
            }

            // Font size: F5/F6 decrease/increase
            if (Input.GetKeyDown(KeyCode.F5))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize - 1f);
            if (Input.GetKeyDown(KeyCode.F6))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize + 1f);

            // F9: save settings, F10: reset to defaults
            if (Input.GetKeyDown(KeyCode.F9))
            {
                SettingsBridge.Save();
                Debug.Log("[MidiGame] Settings saved");
            }
            if (Input.GetKeyDown(KeyCode.F10))
            {
                SettingsBridge.ResetToDefaults();
                Debug.Log("[MidiGame] Settings reset to defaults");
            }
        }
        // ═══════════════════════════════════════════════════════════
        //  HIERARCHY BUILDERS
        // ═══════════════════════════════════════════════════════════

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        // ═══════════════════════════════════════════════════════════
        //  CAMERA — perspective 3D view looking down at the piano
        // ═══════════════════════════════════════════════════════════

        static readonly Vector3 DefaultCameraPos = new Vector3(0f, 4f, -5f);

        void SetupCamera()
        {
            if (!configureCamera) return;

            var cam = EnsureCamera();
            cam.orthographic = false;
            cam.fieldOfView = 50f;
            cam.transform.position = DefaultCameraPos;
            cam.transform.LookAt(Vector3.zero, Vector3.up);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            _cameraSway = cam.gameObject.AddComponent<CameraAmbientMotion>();
            _cameraSway.lookAtTarget = Vector3.zero;

            Log("Camera: perspective, FOV=50, 3D piano view + sway");
        }

        // ═══════════════════════════════════════════════════════════
        //  COLOR PALETTE (Procedural — .engine)
        // ═══════════════════════════════════════════════════════════

        void CreatePalette()
        {
            var colors = new Dictionary<string, Color>
            {
                { "key_white",    Color.white },
                { "key_black",    new Color(0.08f, 0.08f, 0.08f) },
                { "piano_body",   new Color(0.05f, 0.05f, 0.08f) },
            };
            _palette = ColorPalette.CreateRuntime(colors);
            Log("Created MIDI ColorPalette");
        }

        // ═══════════════════════════════════════════════════════════
        //  3D PIANO (Procedural — .engine)
        // ═══════════════════════════════════════════════════════════

        void CreatePiano()
        {
            _pianoBlueprint = new MidiPianoBlueprint(pianoLowNote, pianoHighNote);

            var go = new GameObject("MidiPiano");
            _pianoController = go.AddComponent<MidiPianoController>();
            _pianoController.Initialize(_pianoBlueprint, _palette);

            Log($"Created 3D Piano ({pianoLowNote}–{pianoHighNote}) via ProceduralAssembler");
        }

        // ═══════════════════════════════════════════════════════════
        //  HIERARCHY BUILDERS
        // ═══════════════════════════════════════════════════════════

        void CreateCanvas()
        {
            var go = new GameObject("MidiCanvas");
            go.transform.SetParent(transform, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }

        void CreateEditorPanel()
        {
            var go = new GameObject("EditorPanel");
            go.transform.SetParent(_canvas.transform, false);

            // RectTransform — left portion of screen, above status bar
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, _debuggerHeightRatio);
            rt.anchorMax = new Vector2(_editorWidthRatio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.03f, 0.06f, 0.95f);
            bg.raycastTarget = true;

            // CodeEditorWindow component
            _editor = go.AddComponent<CodeEditorWindow>();
            _editor.InitializeProgrammatic(GetDefaultFont(), SettingsBridge.FontSize, bg);
        }

        void CreateMonitorPanel()
        {
            var go = new GameObject("MonitorPanel");
            go.transform.SetParent(_canvas.transform, false);

            // RectTransform — right upper portion of screen
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(_editorWidthRatio, _debuggerHeightRatio);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
            bg.raycastTarget = true;

            // MidiMonitorWindow component
            _monitor = go.AddComponent<MidiMonitorWindow>();
            _monitor.InitializeProgrammatic(GetDefaultFont(), SettingsBridge.FontSize, bg);
        }

        void CreateDebuggerPanel()
        {
            var go = new GameObject("DebuggerPanel");
            go.transform.SetParent(_canvas.transform, false);

            // RectTransform — bottom left (below editor)
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(_editorWidthRatio, _debuggerHeightRatio);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.02f, 0.04f, 0.95f);
            bg.raycastTarget = true;

            _debugger = go.AddComponent<MidiCodeDebugger>();
            _debugger.InitializeProgrammatic(GetDefaultFont(), SettingsBridge.FontSize, bg);
            _debugger.SetTitle("DEBUGGER");
        }

        void CreateStatusBarPanel()
        {
            var go = new GameObject("StatusBarPanel");
            go.transform.SetParent(_canvas.transform, false);

            // RectTransform — bottom right (below monitor)
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(_editorWidthRatio, 0f);
            rt.anchorMax = new Vector2(1f, _debuggerHeightRatio);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);
            bg.raycastTarget = true;

            _statusBar = go.AddComponent<MidiStatusBar>();
            _statusBar.InitializeProgrammatic(GetDefaultFont(), SettingsBridge.FontSize - 1f, bg);
        }

        void CreateExecutor()
        {
            var go = new GameObject("MidiExecutor");
            go.transform.SetParent(transform, false);
            _programBehaviour = go.AddComponent<MidiProgramBehaviour>();
        }

        void CreatePersistence()
        {
            var go = new GameObject("Persistence");
            _persistence = go.AddComponent<MidiPersistenceManager>();

            IGitRepository repo;
            string providerName;
            if (useLocalGitProvider)
            {
                string fullPath = System.IO.Path.GetFullPath(dataPath);
                var localRepo = new LocalGitProvider(fullPath);
                localRepo.EnsureInitialized();
                repo = localRepo;
                providerName = "LocalGitProvider";
            }
            else
            {
                repo = new MemoryGitProvider();
                providerName = "MemoryGitProvider";
            }

            _persistence.Initialize(repo);
            _persistence.autosaveInterval = 30f;
            _persistence.syncInterval = useLocalGitProvider ? 300f : 0f;

            Log($"Created MidiPersistenceManager ({providerName})");
        }

        /// <summary>
        /// Resolve TMP default font at runtime. Returns null if unavailable
        /// (TerminalRow handles null font gracefully).
        /// </summary>
        static TMP_FontAsset GetDefaultFont()
        {
            // TMP ships a default font in Resources
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) return font;

            // Fallback: TMP's static default
            return TMP_Settings.defaultFontAsset;
        }

        // ═══════════════════════════════════════════════════════════
        //  COMPILE + RUN
        // ═══════════════════════════════════════════════════════════

        /// <summary>Compile source and auto-run it (called on load/sample switch).</summary>
        void AutoRun(string source)
        {
            var program = CodeGamified.Engine.Compiler.PythonCompiler.Compile(
                source, "autorun", _compilerExt);
            if (program.IsValid)
                OnCompileAndRun(program);
        }

        void OnCompileAndRun(CompiledProgram program)
        {
            if (!program.IsValid)
            {
                Log("Program has errors — not running.");
                return;
            }

            bool ok = _programBehaviour.LoadAndRun(program.SourceCode);
            if (ok)
            {
                WireExecutorEvents(_programBehaviour.Executor);
                _monitor.SetPlaying(true);
                _statusBar.SetPlaying(true);
                _debugger.Bind(_programBehaviour);

                // Persist current composition
                if (_persistence != null)
                {
                    var doc = _editor.Document;
                    _persistence.SetCurrentComposition(
                        program.SourceCode,
                        doc?.Name ?? "my_song",
                        _startingTier,
                        120f);
                }
            }
        }

        void WireExecutorEvents(CodeExecutor executor)
        {
            if (executor == null) return;

            executor.OnInstructionExecuted += (inst, state) => _engineAudio.InstructionStep();
            executor.OnOutput += HandleOutput;
            executor.OnHalted += () =>
            {
                _engineAudio.Halted();
                _monitor.SetPlaying(false);
                _statusBar.SetPlaying(false);
            };
        }

        void HandleOutput(GameEvent evt)
        {
            _engineAudio.Output();
            _monitor.HandleEvent(evt, _programBehaviour.Program);

            // Feed 3D piano — spawn rising note cubes
            if (evt.Type == MidiEventType.NoteOn && _pianoController != null)
            {
                int midiNote = Mathf.RoundToInt(evt.Value);
                float velocity = evt.Channel;
                _pianoController.OnNoteOn(midiNote, velocity);
            }

            // Feed status bar with live state
            switch (evt.Type)
            {
                case MidiEventType.BpmChange:
                    _statusBar.SetBPM(evt.Value);
                    // Also update SimulationTime's BPM for formatted display
                    if (SimulationTime.Instance is MidiSimulationTime mst)
                        mst.BPM = evt.Value;
                    break;
                case MidiEventType.VolumeChange:
                    _statusBar.SetVolume(evt.Value);
                    break;
                case MidiEventType.SynthChange:
                    int idx = Mathf.RoundToInt(evt.Value);
                    if (_programBehaviour.Program?.StringConstants != null &&
                        idx >= 0 && idx < _programBehaviour.Program.StringConstants.Length)
                        _statusBar.SetSynthName(_programBehaviour.Program.StringConstants[idx]);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  PROGRESSION
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Advance player tier. Call from progression/mission system.
        /// Re-opens editor to refresh available options.
        /// </summary>
        public void SetTier(int tier)
        {
            _startingTier = tier;
            _editorExt.SetTier(tier);
            _editor.Open(_editor.Document, _compilerExt, _editorExt);
        }

        // ═══════════════════════════════════════════════════════════
        //  ISettingsListener
        // ═══════════════════════════════════════════════════════════

        public void OnSettingsChanged(SettingsSnapshot settings, SettingsCategory changed)
        {
            switch (changed)
            {
                case SettingsCategory.Audio:
                    _audioProvider.SetMasterVolume(settings.SfxVolume);
                    Log($"Audio — Master:{settings.MasterVolume:F1} " +
                        $"Music:{settings.MusicVolume:F1} SFX:{settings.SfxVolume:F1}");
                    break;

                case SettingsCategory.Display:
                    RebuildPanels(settings.FontSize);
                    Log($"Font size → {settings.FontSize:F0}pt");
                    break;

                case SettingsCategory.Quality:
                    QualityBridge.SetTier((QualityTier)settings.QualityLevel);
                    break;
            }
        }

        /// <summary>
        /// Tear down and rebuild all panels with a new font size.
        /// Preserves the current document and cursor state.
        /// </summary>
        void RebuildPanels(float newFontSize)
        {
            // Snapshot current state
            var doc = _editor.Document;
            string currentSource = doc?.ToSource();
            string currentName = doc?.Name ?? "my_song";

            // Unwire editor events before destroying
            UnwireEditorEvents();
            _editor.OnCompileAndRun -= OnCompileAndRun;

            // Destroy old panels
            Destroy(_editor.gameObject);
            Destroy(_monitor.gameObject);
            Destroy(_debugger.gameObject);
            Destroy(_statusBar.gameObject);

            // Recreate with new font size
            CreateEditorPanel();
            CreateMonitorPanel();
            CreateDebuggerPanel();
            CreateStatusBarPanel();

            // Restore document
            if (!string.IsNullOrEmpty(currentSource))
                _editor.OpenSource(currentSource, currentName, _compilerExt, _editorExt);
            else
                LoadSample(_currentSample);

            // Rebind debugger
            _debugger.Bind(_programBehaviour);

            // Rewire events
            WireEditorEvents();
            _editor.OnCompileAndRun += OnCompileAndRun;
        }
    }
}
