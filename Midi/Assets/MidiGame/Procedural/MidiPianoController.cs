// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Procedural;
using CodeGamified.Engine;
using MidiGame.Audio;

namespace MidiGame.Procedural
{
    /// <summary>
    /// Manages the procedural 3D piano and spawns rising note cubes
    /// when notes are played. Each note cube:
    ///   - Spawns at the correct key X position
    ///   - Is colored by velocity (dim→bright)
    ///   - Rises upward at a speed proportional to the note duration
    ///   - Fades out and self-destructs
    ///
    /// The piano keys pulse (emission flash) on note-on events via ProceduralVisualState.
    /// </summary>
    public class MidiPianoController : MonoBehaviour
    {
        // ── Assembled piano ─────────────────────────────────────
        AssemblyResult _piano;
        MidiPianoBlueprint _blueprint;
        ColorPalette _palette;

        // ── Note particle pool ──────────────────────────────────
        const int POOL_SIZE = 64;
        readonly NoteParticle[] _pool = new NoteParticle[POOL_SIZE];
        int _nextParticle;

        // ── Note colors by octave ───────────────────────────────
        static readonly Color[] OctaveColors =
        {
            new Color(0.8f, 0.2f, 0.2f),   // C2  red
            new Color(1.0f, 0.4f, 0.1f),   // C3  orange
            new Color(1.0f, 0.8f, 0.1f),   // C4  yellow
            new Color(0.2f, 1.0f, 0.3f),   // C5  green
            new Color(0.1f, 0.6f, 1.0f),   // C6  blue
            new Color(0.6f, 0.2f, 1.0f),   // C7  purple
            new Color(1.0f, 0.4f, 0.8f),   // C8  pink
        };

        // ── Config ──────────────────────────────────────────────
        const float NoteRiseSpeed = 2.5f;
        const float NoteLifetime = 3f;
        const float NoteWidth = 0.18f;
        const float NoteHeight = 0.08f;
        const float NoteSpawnY = 0.15f; // just above key surface

        struct NoteParticle
        {
            public GameObject GO;
            public Renderer Renderer;
            public float Lifetime;
            public float Age;
            public float Speed;
            public Color BaseColor;
            public bool Active;
        }

        // ═══════════════════════════════════════════════════════════
        //  INIT
        // ═══════════════════════════════════════════════════════════

        public void Initialize(MidiPianoBlueprint blueprint, ColorPalette palette)
        {
            _blueprint = blueprint;
            _palette = palette;

            // Build the piano via ProceduralAssembler
            _piano = ProceduralAssembler.BuildWithVisualState(blueprint, palette);
            if (_piano.Root != null)
                _piano.Root.transform.SetParent(transform, false);

            // Pre-allocate note particle pool
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");

            for (int i = 0; i < POOL_SIZE; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"NoteParticle_{i}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(NoteWidth, NoteHeight, 0.15f);
                go.SetActive(false);

                // Remove default collider
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Set up material
                var r = go.GetComponent<Renderer>();
                if (shader != null)
                    r.material = new Material(shader);
                r.material.EnableKeyword("_EMISSION");

                _pool[i] = new NoteParticle
                {
                    GO = go,
                    Renderer = r,
                    Active = false
                };
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  NOTE EVENTS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Called when a MIDI note plays. Spawns a rising cube and pulses the key.
        /// </summary>
        public void OnNoteOn(int midiNote, float velocity)
        {
            // Pulse the key
            string keyId = IsBlackKey(midiNote)
                ? $"black_{midiNote}"
                : $"white_{midiNote}";

            Color noteColor = GetNoteColor(midiNote, velocity);

            if (_piano.VisualState != null)
                _piano.VisualState.Pulse(keyId, noteColor * 3f, 0.15f);

            // Spawn rising note particle
            SpawnParticle(midiNote, velocity, noteColor);
        }

        // ═══════════════════════════════════════════════════════════
        //  UPDATE — animate particles
        // ═══════════════════════════════════════════════════════════

        void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < POOL_SIZE; i++)
            {
                ref var p = ref _pool[i];
                if (!p.Active) continue;

                p.Age += dt;
                if (p.Age >= p.Lifetime)
                {
                    p.GO.SetActive(false);
                    p.Active = false;
                    continue;
                }

                // Rise
                var pos = p.GO.transform.localPosition;
                pos.y += p.Speed * dt;
                p.GO.transform.localPosition = pos;

                // Fade out (alpha + emission decay)
                float t = p.Age / p.Lifetime;
                float alpha = 1f - t * t; // quadratic fade
                Color c = p.BaseColor;
                c.a = alpha;

                var mat = p.Renderer.material;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", c);
                else
                    mat.color = c;

                // Emission decays
                Color emission = p.BaseColor * (alpha * 2f);
                emission.a = 1f;
                mat.SetColor("_EmissionColor", emission);

                // Shrink slightly as it rises
                float scale = Mathf.Lerp(1f, 0.3f, t);
                p.GO.transform.localScale = new Vector3(
                    NoteWidth * scale, NoteHeight * scale, 0.15f * scale);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  INTERNALS
        // ═══════════════════════════════════════════════════════════

        void SpawnParticle(int midiNote, float velocity, Color color)
        {
            ref var p = ref _pool[_nextParticle];
            _nextParticle = (_nextParticle + 1) % POOL_SIZE;

            float keyX = _blueprint.GetKeyX(midiNote);
            float keyZ = IsBlackKey(midiNote) ? -0.2f : 0f;

            p.GO.transform.localPosition = new Vector3(keyX, NoteSpawnY, keyZ);
            p.GO.SetActive(true);
            p.Active = true;
            p.Age = 0f;
            p.Lifetime = NoteLifetime;
            p.Speed = NoteRiseSpeed;
            p.BaseColor = color;

            // Velocity → width: louder notes are wider
            float velNorm = Mathf.Clamp01(velocity / 127f);
            float width = Mathf.Lerp(NoteWidth * 0.5f, NoteWidth * 1.5f, velNorm);
            p.GO.transform.localScale = new Vector3(width, NoteHeight, 0.15f);

            // Set initial color + emission
            var mat = p.Renderer.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            mat.SetColor("_EmissionColor", color * 2f);
        }

        Color GetNoteColor(int midiNote, float velocity)
        {
            int octave = (midiNote / 12) - 2;
            octave = Mathf.Clamp(octave, 0, OctaveColors.Length - 1);
            Color baseColor = OctaveColors[octave];

            // Velocity modulates brightness
            float velNorm = Mathf.Clamp01(velocity / 127f);
            return Color.Lerp(baseColor * 0.3f, baseColor, velNorm);
        }

        static bool IsBlackKey(int midiNote)
        {
            int semi = midiNote % 12;
            return semi == 1 || semi == 3 || semi == 6 || semi == 8 || semi == 10;
        }
    }
}
