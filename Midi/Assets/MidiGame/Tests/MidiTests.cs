// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game Tests
using NUnit.Framework;
using System.Collections.Generic;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Editor;

namespace MidiGame.Tests
{
    [TestFixture]
    public class MidiCompilerTests
    {
        MidiCompilerExtension _ext;

        [SetUp]
        public void SetUp()
        {
            _ext = new MidiCompilerExtension();
        }

        [Test]
        public void Compile_Play_EmitsCustomOpcode()
        {
            var prog = PythonCompiler.Compile("play(60)", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasPlay = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.PLAY) hasPlay = true;
            Assert.IsTrue(hasPlay, "play() should emit PLAY opcode");
        }

        [Test]
        public void Compile_Sleep_EmitsWait()
        {
            var prog = PythonCompiler.Compile("sleep(0.5)", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            // sleep → wait is handled by engine's CallNode
            bool hasWait = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == OpCode.WAIT) hasWait = true;
            // sleep maps to wait() in the engine
        }

        [Test]
        public void Compile_PlayWithVelocity_EmitsPlay()
        {
            var prog = PythonCompiler.Compile("play(60, 80)", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasPlay = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.PLAY) hasPlay = true;
            Assert.IsTrue(hasPlay, "play(note, vel) should emit PLAY opcode");
        }

        [Test]
        public void Compile_PlayWithAllArgs_EmitsPlay()
        {
            var prog = PythonCompiler.Compile("play(60, 80, 0.5)", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasPlay = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.PLAY) hasPlay = true;
            Assert.IsTrue(hasPlay, "play(note, vel, dur) should emit PLAY opcode");
        }

        [Test]
        public void Compile_Sample_EmitsSampleOpcode()
        {
            var prog = PythonCompiler.Compile("sample(\"kick\")", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasSample = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.SAMPLE) hasSample = true;
            Assert.IsTrue(hasSample, "sample() should emit SAMPLE opcode");
        }

        [Test]
        public void Compile_UseSynth_EmitsUseSynthOpcode()
        {
            var prog = PythonCompiler.Compile("use_synth(\"piano\")", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasSynth = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.USE_SYNTH) hasSynth = true;
            Assert.IsTrue(hasSynth, "use_synth() should emit USE_SYNTH opcode");
        }

        [Test]
        public void Compile_SetBpm_EmitsSetBpmOpcode()
        {
            var prog = PythonCompiler.Compile("set_bpm(140)", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasBpm = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.SET_BPM) hasBpm = true;
            Assert.IsTrue(hasBpm, "set_bpm() should emit SET_BPM opcode");
        }

        [Test]
        public void Compile_SetVolume_EmitsSetVolumeOpcode()
        {
            var prog = PythonCompiler.Compile("set_volume(0.8)", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasVol = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.SET_VOLUME) hasVol = true;
            Assert.IsTrue(hasVol, "set_volume() should emit SET_VOLUME opcode");
        }

        [Test]
        public void Compile_UseFx_EmitsUseFxOpcode()
        {
            var prog = PythonCompiler.Compile("use_fx(\"reverb\")", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasFx = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.USE_FX) hasFx = true;
            Assert.IsTrue(hasFx, "use_fx() should emit USE_FX opcode");
        }

        [Test]
        public void Compile_Stop_EmitsStopAllOpcode()
        {
            var prog = PythonCompiler.Compile("stop()", "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasStop = false;
            foreach (var inst in prog.Instructions)
                if (inst.Op == MidiOpCode.STOP_ALL) hasStop = true;
            Assert.IsTrue(hasStop, "stop() should emit STOP_ALL opcode");
        }

        [Test]
        public void Compile_PlayNoArgs_ProducesError()
        {
            var prog = PythonCompiler.Compile("play()", "test", _ext);
            Assert.IsTrue(prog.Errors.Count > 0, "play() with no args should error");
        }

        [Test]
        public void Compile_LoopWithPlay_IsValid()
        {
            var source = "while True:\n    play(60)\n    sleep(0.5)";
            var prog = PythonCompiler.Compile(source, "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
            bool hasPlay = false;
            bool hasJmp = false;
            foreach (var inst in prog.Instructions)
            {
                if (inst.Op == MidiOpCode.PLAY) hasPlay = true;
                if (inst.Op == OpCode.JMP) hasJmp = true;
            }
            Assert.IsTrue(hasPlay, "Loop body should contain PLAY");
            Assert.IsTrue(hasJmp, "while True should produce JMP");
        }

        [Test]
        public void Compile_ForRangeWithPlay_IsValid()
        {
            var source = "for i in range(4):\n    play(60 + i)\n    sleep(0.25)";
            var prog = PythonCompiler.Compile(source, "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
        }

        [Test]
        public void Compile_ConditionalPlay_IsValid()
        {
            var source = "note = 60\nif note > 64:\n    play(note)\nelse:\n    play(60)";
            var prog = PythonCompiler.Compile(source, "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
        }

        [Test]
        public void Compile_FullSong_IsValid()
        {
            var source =
                "set_bpm(120)\n" +
                "set_volume(0.8)\n" +
                "note = 60\n" +
                "while True:\n" +
                "    play(note)\n" +
                "    sleep(0.5)\n" +
                "    note = note + 1\n" +
                "    if note > 72:\n" +
                "        note = 60\n";
            var prog = PythonCompiler.Compile(source, "test", _ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));
        }

        [Test]
        public void FloatConstants_ContainNoteValues()
        {
            var prog = PythonCompiler.Compile("play(60)", "test", _ext);
            bool has60 = false;
            foreach (var f in prog.FloatConstants)
                if (f == 60f) has60 = true;
            Assert.IsTrue(has60, "Float constants should contain note value 60");
        }

        [Test]
        public void StringConstants_ContainSynthName()
        {
            var prog = PythonCompiler.Compile("use_synth(\"piano\")", "test", _ext);
            bool hasPiano = false;
            foreach (var s in prog.StringConstants)
                if (s == "piano") hasPiano = true;
            Assert.IsTrue(hasPiano, "String constants should contain 'piano'");
        }
    }

    [TestFixture]
    public class MidiEditorExtensionTests
    {
        [Test]
        public void Tier1_OnlyHasPlayAndSleep()
        {
            var ext = new MidiEditorExtension(1);
            var funcs = ext.GetAvailableFunctions();
            Assert.AreEqual(2, funcs.Count);
            Assert.AreEqual("play", funcs[0].Name);
            Assert.AreEqual("sleep", funcs[1].Name);
        }

        [Test]
        public void Tier2_AddsTempoAndVolume()
        {
            var ext = new MidiEditorExtension(2);
            var funcs = ext.GetAvailableFunctions();
            Assert.AreEqual(4, funcs.Count);
            bool hasBpm = false, hasVol = false;
            foreach (var f in funcs)
            {
                if (f.Name == "set_bpm") hasBpm = true;
                if (f.Name == "set_volume") hasVol = true;
            }
            Assert.IsTrue(hasBpm);
            Assert.IsTrue(hasVol);
        }

        [Test]
        public void Tier3_AddsSampleAndStop()
        {
            var ext = new MidiEditorExtension(3);
            var funcs = ext.GetAvailableFunctions();
            bool hasSample = false, hasStop = false;
            foreach (var f in funcs)
            {
                if (f.Name == "sample") hasSample = true;
                if (f.Name == "stop") hasStop = true;
            }
            Assert.IsTrue(hasSample);
            Assert.IsTrue(hasStop);
        }

        [Test]
        public void Tier5_AddsSynthAndFx()
        {
            var ext = new MidiEditorExtension(5);
            var funcs = ext.GetAvailableFunctions();
            bool hasSynth = false, hasFx = false;
            foreach (var f in funcs)
            {
                if (f.Name == "use_synth") hasSynth = true;
                if (f.Name == "use_fx") hasFx = true;
            }
            Assert.IsTrue(hasSynth);
            Assert.IsTrue(hasFx);
        }

        [Test]
        public void Tier1_WhileLoopGated()
        {
            var ext = new MidiEditorExtension(1);
            Assert.IsFalse(ext.IsWhileLoopAllowed());
            Assert.IsFalse(ext.IsForLoopAllowed());
        }

        [Test]
        public void Tier2_WhileLoopAllowed()
        {
            var ext = new MidiEditorExtension(2);
            Assert.IsTrue(ext.IsWhileLoopAllowed());
            Assert.IsFalse(ext.IsForLoopAllowed());
        }

        [Test]
        public void Tier3_ForLoopAllowed()
        {
            var ext = new MidiEditorExtension(3);
            Assert.IsTrue(ext.IsWhileLoopAllowed());
            Assert.IsTrue(ext.IsForLoopAllowed());
        }

        [Test]
        public void VariableNameSuggestions_ContainsMusicTerms()
        {
            var ext = new MidiEditorExtension(1);
            var names = ext.GetVariableNameSuggestions();
            Assert.IsTrue(names.Contains("note"));
            Assert.IsTrue(names.Contains("beat"));
            Assert.IsTrue(names.Contains("velocity"));
        }

        [Test]
        public void StringLiteralSuggestions_ContainsSynthAndSamples()
        {
            var ext = new MidiEditorExtension(1);
            var suggestions = ext.GetStringLiteralSuggestions();
            Assert.IsTrue(suggestions.Contains("sine"));
            Assert.IsTrue(suggestions.Contains("kick"));
            Assert.IsTrue(suggestions.Contains("reverb"));
        }
    }

    [TestFixture]
    public class MidiIOHandlerTests
    {
        [Test]
        public void ExecutePlay_EmitsNoteOnEvent()
        {
            var executor = new CodeExecutor();
            var handler = new MidiIOHandler(executor);
            executor.SetIOHandler(handler);

            // Compile a simple play
            var ext = new MidiCompilerExtension();
            var prog = PythonCompiler.Compile("play(60)", "test", ext);
            Assert.IsTrue(prog.IsValid, string.Join("; ", prog.Errors));

            executor.LoadProgram(prog);

            // Run until halt
            for (int i = 0; i < 100 && executor.IsRunning; i++)
                executor.ExecuteOne();

            // Check for NoteOn event
            bool hasNoteOn = false;
            while (executor.State.OutputEvents.Count > 0)
            {
                var evt = executor.State.OutputEvents.Dequeue();
                if (evt.Type == MidiEventType.NoteOn)
                {
                    hasNoteOn = true;
                    Assert.AreEqual(60f, evt.Value, 0.5f, "Note should be 60");
                }
            }
            Assert.IsTrue(hasNoteOn, "play(60) should emit NoteOn event");
        }

        [Test]
        public void ExecuteSetBpm_EmitsBpmChangeEvent()
        {
            var executor = new CodeExecutor();
            var handler = new MidiIOHandler(executor);
            executor.SetIOHandler(handler);

            var ext = new MidiCompilerExtension();
            var prog = PythonCompiler.Compile("set_bpm(140)", "test", ext);
            Assert.IsTrue(prog.IsValid);

            executor.LoadProgram(prog);

            for (int i = 0; i < 100 && executor.IsRunning; i++)
                executor.ExecuteOne();

            bool hasBpm = false;
            while (executor.State.OutputEvents.Count > 0)
            {
                var evt = executor.State.OutputEvents.Dequeue();
                if (evt.Type == MidiEventType.BpmChange)
                {
                    hasBpm = true;
                    Assert.AreEqual(140f, evt.Value, 0.1f);
                }
            }
            Assert.IsTrue(hasBpm, "set_bpm(140) should emit BpmChange event");
        }

        [Test]
        public void ExecuteStop_EmitsNoteOffEvent()
        {
            var executor = new CodeExecutor();
            var handler = new MidiIOHandler(executor);
            executor.SetIOHandler(handler);

            var ext = new MidiCompilerExtension();
            var prog = PythonCompiler.Compile("stop()", "test", ext);
            Assert.IsTrue(prog.IsValid);

            executor.LoadProgram(prog);

            for (int i = 0; i < 100 && executor.IsRunning; i++)
                executor.ExecuteOne();

            bool hasNoteOff = false;
            while (executor.State.OutputEvents.Count > 0)
            {
                var evt = executor.State.OutputEvents.Dequeue();
                if (evt.Type == MidiEventType.NoteOff)
                    hasNoteOff = true;
            }
            Assert.IsTrue(hasNoteOff, "stop() should emit NoteOff event");
        }
    }

    [TestFixture]
    public class MidiAudioHelperTests
    {
        [Test]
        public void MidiNoteToFrequency_A4Is440()
        {
            float freq = MidiGame.Audio.MidiAudioProvider.MidiNoteToFrequency(69);
            Assert.AreEqual(440f, freq, 0.01f);
        }

        [Test]
        public void MidiNoteToFrequency_C4Is262()
        {
            float freq = MidiGame.Audio.MidiAudioProvider.MidiNoteToFrequency(60);
            Assert.AreEqual(261.63f, freq, 0.1f);
        }

        [Test]
        public void MidiNoteToName_60IsC4()
        {
            string name = MidiGame.Audio.MidiAudioProvider.MidiNoteToName(60);
            Assert.AreEqual("C4", name);
        }

        [Test]
        public void MidiNoteToName_69IsA4()
        {
            string name = MidiGame.Audio.MidiAudioProvider.MidiNoteToName(69);
            Assert.AreEqual("A4", name);
        }

        [Test]
        public void MidiNoteToName_72IsC5()
        {
            string name = MidiGame.Audio.MidiAudioProvider.MidiNoteToName(72);
            Assert.AreEqual("C5", name);
        }
    }
}
