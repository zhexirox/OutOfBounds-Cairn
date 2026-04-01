using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(OutOfBounds.OutOfBoundsMod), "OutOfBounds", "1.0.0", "Zhexirox")]
[assembly: MelonGame("TheGameBakers", "Cairn")]

namespace OutOfBounds
{
    /// <summary>
    /// OutOfBounds - Removes map boundaries for free exploration in Cairn.
    ///
    /// Cairn uses invisible boundary zones (NoGoArea, NoGoAreasManager) that
    /// teleport the player back when they reach the edge of the playable area.
    /// This mod finds and deactivates those GameObjects so you can explore freely.
    ///
    /// HOW IT WORKS:
    /// ============================================================
    /// We scan all MonoBehaviours for components named "NoGoArea" or
    /// "NoGoAreasManager" and deactivate their GameObjects. We use IL2CPP
    /// reflection (GetIl2CppType().Name) instead of direct type references
    /// to avoid BadImageFormatException (see IL2CPP_PATTERNS.md).
    ///
    /// PERFORMANCE:
    /// ============================================================
    /// Cairn streams dozens of sub-scenes as you move. To avoid FPS drops,
    /// we batch scene loads: a scan runs only once, 0.5s after the LAST
    /// scene finishes loading. Already-known objects (tracked by instanceID)
    /// are skipped. This means a burst of 30 scene loads = 1 scan, not 30.
    ///
    /// Compatible with Cairn (Unity 6, IL2CPP) via MelonLoader.
    /// </summary>
    public class OutOfBoundsMod : MelonMod
    {
        // ================================================================
        // CONSTANTS
        // ================================================================

        /// <summary>
        /// Delay after the last scene load before scanning.
        /// Cairn loads many sub-scenes in bursts; this coalesces them into one scan.
        /// </summary>
        private const float SCAN_DELAY = 0.5f;

        // ================================================================
        // STATE
        // ================================================================

        /// <summary>All boundary GameObjects found so far (persists across scenes)</summary>
        private readonly List<GameObject> _boundaryObjects = new();

        /// <summary>Instance IDs of known boundary objects (avoids reprocessing)</summary>
        private readonly HashSet<int> _knownIds = new();

        /// <summary>Time at which the next scan should execute (-1 = no scan pending)</summary>
        private float _pendingScanTime = -1f;

        // ================================================================
        // MELONLOADER LIFECYCLE
        // ================================================================

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("========================================");
            LoggerInstance.Msg("   OutOfBounds v1.0.0");
            LoggerInstance.Msg("   Map boundaries removed");
            LoggerInstance.Msg("   Explore freely!");
            LoggerInstance.Msg("========================================");
        }

        /// <summary>
        /// Called each time a scene finishes loading.
        /// Cairn loads many sub-scenes per area, so we defer the actual scan
        /// to avoid running FindObjectsOfType dozens of times in a burst.
        /// </summary>
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _pendingScanTime = Time.time + SCAN_DELAY;
        }

        public override void OnUpdate()
        {
            // Run deferred scan once after the scene-load burst settles
            if (_pendingScanTime > 0f && Time.time >= _pendingScanTime)
            {
                _pendingScanTime = -1f;
                ScanAndDisableBoundaries();
            }
        }

        // ================================================================
        // CORE LOGIC
        // ================================================================

        /// <summary>
        /// Scans all loaded MonoBehaviours for boundary components and deactivates
        /// their GameObjects. Only processes objects not already tracked.
        /// </summary>
        private void ScanAndDisableBoundaries()
        {
            // Clean up references to destroyed objects
            _boundaryObjects.RemoveAll(go => go == null);
            _knownIds.RemoveWhere(id => !_boundaryObjects.Exists(go => go != null && go.GetInstanceID() == id));

            int newCount = 0;
            var allMonos = Object.FindObjectsOfType<MonoBehaviour>(true);

            foreach (var mono in allMonos)
            {
                var typeName = mono.GetIl2CppType().Name;
                if (typeName != "NoGoArea" && typeName != "NoGoAreasManager")
                    continue;

                var go = mono.gameObject;
                int id = go.GetInstanceID();

                // Skip already-known objects
                if (_knownIds.Contains(id))
                    continue;

                _knownIds.Add(id);
                _boundaryObjects.Add(go);
                go.SetActive(false);
                newCount++;
            }

            if (newCount > 0)
                LoggerInstance.Msg($"[OOB] Disabled {newCount} new boundary objects (total: {_boundaryObjects.Count})");
        }
    }
}
