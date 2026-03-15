// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using CodeGamified.Persistence;
using UnityEngine;

namespace MidiGame.Persistence
{
    /// <summary>
    /// Serializable entity for a saved MIDI composition.
    /// Stored in .data/compositions/ via the Persistence framework.
    /// </summary>
    [System.Serializable]
    public class MidiScriptData
    {
        public string name;
        public string source;
        public int tier;
        public float bpm;
    }

    /// <summary>
    /// JSON serializer for MidiScriptData.
    /// </summary>
    public class MidiScriptSerializer : IEntitySerializer<MidiScriptData>
    {
        public int SchemaVersion => 1;

        public string Serialize(MidiScriptData entity)
        {
            return JsonUtility.ToJson(entity, true);
        }

        public MidiScriptData Deserialize(string json)
        {
            return JsonUtility.FromJson<MidiScriptData>(json);
        }
    }
}
