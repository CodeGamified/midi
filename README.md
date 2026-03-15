# MIDI — The Music Coding Game

A game where you **write code to compose music**. Type Sonic Pi–flavored Python into a terminal UI, hit run, and hear your composition play back in real time with procedural audio synthesis.

Part of the [CodeGamified](https://github.com/CodeGamified) ecosystem.

## How It Works

Players write short programs using a simplified Python syntax. The code compiles to custom opcodes and executes on a virtual machine that drives a polyphonic synthesizer. A live piano-roll monitor visualizes notes as they play.

```python
set_bpm(140)
use_synth("square")

while True:
    play(60)
    sleep(0.5)
    play(64)
    sleep(0.5)
    play(67)
    sleep(1)
```

### Available Functions

| Function | Description | Tier |
|---|---|---|
| `play(note)` | Play a MIDI note (0–127) | 1 |
| `sleep(beats)` | Wait for a number of beats | 1 |
| `set_bpm(n)` | Set tempo (beats per minute) | 2 |
| `set_volume(v)` | Set master volume (0.0–1.0) | 2 |
| `sample(name)` | Trigger a named sample | 3 |
| `stop()` | Stop all notes | 3 |
| `use_synth(name)` | Switch synthesizer waveform | 5 |
| `use_fx(name, mix)` | Apply an audio effect | 5 |

### Synthesizers

`sine` · `square` · `saw` · `triangle` · `piano` · `pluck` · `bass` · `pad`

## Tier Progression

New language features unlock as the player advances:

| Tier | Unlocks |
|---|---|
| 1 | `play()`, `sleep()` |
| 2 | `while True` loops, `set_bpm()`, `set_volume()` |
| 3 | `for` loops, variables, `sample()` |
| 4 | `if`/conditionals, `use_synth()` |
| 5 | `use_fx()`, expressions |

## Architecture

```
MidiGameBootstrap
├─ MidiCanvas
│  ├─ EditorPanel     — code editor (TUI)
│  ├─ MonitorPanel    — live piano-roll visualizer
│  ├─ DebuggerPanel   — step-through debugger
│  └─ StatusBarPanel  — BPM / volume / synth info
├─ MidiExecutor       — program runtime (MidiProgramBehaviour)
├─ SimulationTime     — beat-synced clock
└─ Persistence        — git-backed composition storage
```

The shared [.engine](https://github.com/CodeGamified/.engine) submodule provides the TUI framework, code editor, compiler, virtual machine, and persistence layer — the same foundation used by Pong, SeaRäuber, and BitNaughts.

### Key Source Files

| File | Purpose |
|---|---|
| `MidiGameBootstrap.cs` | Scene setup and wiring |
| `MidiCompilerExtension.cs` | Compiles music functions to opcodes |
| `MidiIOHandler.cs` | Executes opcodes at runtime |
| `MidiOpCode.cs` | Custom opcode definitions |
| `MidiProgramBehaviour.cs` | MonoBehaviour bridge to the VM |
| `MidiAudioProvider.cs` | Procedural waveform synthesis |
| `MidiMonitorWindow.cs` | Piano-roll TUI visualizer |
| `MidiEditorExtension.cs` | Editor autocomplete and option tree |

## Requirements

- **Unity 6** (6000.0.36f1)
- Universal Render Pipeline (URP)
- Input System package

## Getting Started

```bash
git clone --recurse-submodules https://github.com/CodeGamified/codegamified.github.io.git
```

Open the `midi/Midi` folder in Unity. The main scenes are in `Assets/Scenes/`.

### Persistence

Composition persistence saves player scripts to a `.data/` directory. Configure in the bootstrap inspector:

- **enablePersistence** — toggle save/load
- **useLocalGitProvider** — use a local git repo instead of in-memory (survives restarts)

## License

MIT — Copyright CodeGamified 2025-2026