using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DiveProtocol.UI
{
    /// <summary>Owns the baseline sector status for each gameplay scene without coupling it to HUD rendering.</summary>
    public static class LevelStatusCatalog
    {
        private static readonly LevelStatusData Unknown = new(
            string.Empty,
            "--",
            "FACILITY ACCESS",
            LevelStatusData.AlertLevel.Low);

        private static readonly Dictionary<string, LevelStatusData> ByScene = new(StringComparer.Ordinal)
        {
            [SceneNames.Level01Drainage] = new(
                SceneNames.Level01Drainage,
                "D-01",
                "DRAINAGE ACCESS",
                LevelStatusData.AlertLevel.Low),
            [SceneNames.Level02Containment] = new(
                SceneNames.Level02Containment,
                "D-02",
                "CONTAINMENT",
                LevelStatusData.AlertLevel.Medium),
            [SceneNames.Level03MaintenanceTransfer] = new(
                SceneNames.Level03MaintenanceTransfer,
                "D-03",
                "MAINTENANCE TRANSFER",
                LevelStatusData.AlertLevel.High),
            [SceneNames.Level04FacilityCore] = new(
                SceneNames.Level04FacilityCore,
                "D-04",
                "FACILITY CORE",
                LevelStatusData.AlertLevel.Critical)
        };

        /// <summary>Gets immutable baseline HUD status data for a loaded gameplay scene.</summary>
        public static LevelStatusData GetForScene(string sceneName)
        {
            return ByScene.TryGetValue(sceneName ?? string.Empty, out LevelStatusData data)
                ? data
                : Unknown;
        }

        /// <summary>Gets the baseline sector status for Unity's active scene.</summary>
        public static LevelStatusData GetForActiveScene()
        {
            return GetForScene(SceneManager.GetActiveScene().name);
        }
    }
}
