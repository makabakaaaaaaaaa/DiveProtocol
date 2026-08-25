using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DiveProtocol.Builds
{
    /// <summary>Central fixed-node configuration for the four playable level scenes.</summary>
    public static class LevelBuildSelectionCatalog
    {
        private static readonly BuildUpgradeId[] CorePool =
        {
            BuildUpgradeId.RedMarrow_Overdraft,
            BuildUpgradeId.OpticNerve_Calibration,
            BuildUpgradeId.Humus_Sympathy
        };

        private static readonly BuildUpgradeId[] ReinforcementPool =
        {
            BuildUpgradeId.RedMarrow_BloodBulletCompression,
            BuildUpgradeId.RedMarrow_CoagulationReflex,
            BuildUpgradeId.RedMarrow_OrganCollateral,
            BuildUpgradeId.OpticNerve_CalmShot,
            BuildUpgradeId.OpticNerve_JointRupture,
            BuildUpgradeId.OpticNerve_MarkRecycle,
            BuildUpgradeId.Humus_DeadMatterWhisper,
            BuildUpgradeId.Humus_PollutionCoat,
            BuildUpgradeId.Humus_CadaverDelay
        };

        private static readonly BuildUpgradeId[] UtilityPool =
        {
            BuildUpgradeId.RedMarrow_ExcessAdrenaline,
            BuildUpgradeId.RedMarrow_BloodEconomy,
            BuildUpgradeId.RedMarrow_LowHealthAmplifier,
            BuildUpgradeId.OpticNerve_SafeDistance,
            BuildUpgradeId.OpticNerve_MarkPersistence,
            BuildUpgradeId.OpticNerve_AimDiscipline,
            BuildUpgradeId.Humus_AbnormalMetabolism,
            BuildUpgradeId.Humus_ExpandedVessel,
            BuildUpgradeId.Humus_EnvironmentalTolerance
        };

        private static readonly BuildUpgradeId[] AwakeningPool =
        {
            BuildUpgradeId.RedMarrow_SacrificeProtocol,
            BuildUpgradeId.OpticNerve_PerfectPrediction,
            BuildUpgradeId.Humus_CompleteSymbiosis
        };

        // The project currently starts in Containment, then goes Drainage -> Maintenance -> Facility Core.
        private static readonly Dictionary<string, LevelBuildSelectionDefinition> ByScene = new(StringComparer.Ordinal)
        {
            [SceneNames.Level02Containment] = new(
                SceneNames.Level02Containment,
                "build-node-depth-1",
                0,
                BuildSelectionTier.Core,
                CorePool),
            [SceneNames.Level01Drainage] = new(
                SceneNames.Level01Drainage,
                "build-node-depth-2",
                1,
                BuildSelectionTier.Reinforcement,
                ReinforcementPool),
            [SceneNames.Level03MaintenanceTransfer] = new(
                SceneNames.Level03MaintenanceTransfer,
                "build-node-depth-3",
                2,
                BuildSelectionTier.Utility,
                UtilityPool),
            [SceneNames.Level04FacilityCore] = new(
                SceneNames.Level04FacilityCore,
                "build-node-depth-4",
                3,
                BuildSelectionTier.Awakening,
                AwakeningPool,
                UtilityPool)
        };

        public static bool TryGetForScene(string sceneName, out LevelBuildSelectionDefinition definition)
        {
            return ByScene.TryGetValue(sceneName ?? string.Empty, out definition);
        }

        public static bool TryGetForActiveScene(out LevelBuildSelectionDefinition definition)
        {
            return TryGetForScene(SceneManager.GetActiveScene().name, out definition);
        }

        /// <summary>Returns the no-zone Symbiosis regeneration baseline for a gameplay scene.</summary>
        public static float GetSymbiosisRegenerationPerSecond(string sceneName)
        {
            return sceneName switch
            {
                SceneNames.Level01Drainage => 1f,
                SceneNames.Level02Containment => 2f,
                SceneNames.Level03MaintenanceTransfer => 3f,
                SceneNames.Level04FacilityCore => 4f,
                _ => 0f
            };
        }
    }
}
