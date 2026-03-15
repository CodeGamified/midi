// Copyright CodeGamified 2025-2026
// MIT License — MIDI Code Game
using CodeGamified.Persistence;
using UnityEngine;

namespace MidiGame.Persistence
{
    /// <summary>
    /// Autosave manager for MIDI — persists player compositions via the Persistence framework.
    /// Uses MemoryGitProvider by default (in-memory). Swap to LocalGitProvider
    /// when .data/ submodule is configured.
    /// </summary>
    public class MidiPersistenceManager : PersistenceBehaviour
    {
        EntityStore<MidiScriptData> _compositionStore;
        string _playerId = "local";
        string _lastSavedSource;
        string _currentSource;
        string _currentName = "my_song";
        int _currentTier;
        float _currentBpm = 120f;

        public void Initialize(IGitRepository repo)
        {
            base.Initialize(repo);
            _compositionStore = new EntityStore<MidiScriptData>(
                repo, new MidiScriptSerializer(), "compositions");

            // Try to load existing composition
            var existing = _compositionStore.Load(_playerId, _currentName);
            if (existing != null && !string.IsNullOrEmpty(existing.source))
            {
                _currentSource = existing.source;
                _currentName = existing.name;
                _currentTier = existing.tier;
                _currentBpm = existing.bpm;
                _lastSavedSource = existing.source;
                Debug.Log("[Persistence] Loaded saved composition");
            }
        }

        /// <summary>Update the current composition state for next save.</summary>
        public void SetCurrentComposition(string source, string name, int tier, float bpm)
        {
            _currentSource = source;
            _currentName = name ?? "my_song";
            _currentTier = tier;
            _currentBpm = bpm;
            MarkDirty();
        }

        /// <summary>Returns the last-loaded source, or null if none.</summary>
        public string LoadedSource => _lastSavedSource != null ? _currentSource : null;
        public string LoadedName => _currentName;
        public int LoadedTier => _currentTier;

        protected override GitResult PerformSave(IGitRepository repo)
        {
            if (string.IsNullOrEmpty(_currentSource)) return GitResult.Ok();
            if (_currentSource == _lastSavedSource) return GitResult.Ok();

            var data = new MidiScriptData
            {
                name = _currentName,
                source = _currentSource,
                tier = _currentTier,
                bpm = _currentBpm
            };

            var result = _compositionStore.Save(_playerId, data.name, data, $"autosave: {data.name}");
            if (result.Success)
                _lastSavedSource = _currentSource;

            return result;
        }
    }
}
