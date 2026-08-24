using System.Collections.Generic;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Synchronizes the run-owned build state with a runtime player instance.
    /// </summary>
    public static class BuildRunBridge
    {
        public static PlayerBuildController EnsureAndSync(Transform player, RunState runState)
        {
            if (player == null)
            {
                return null;
            }

            PlayerBuildController controller = player.GetComponent<PlayerBuildController>();
            if (controller == null)
            {
                controller = player.gameObject.AddComponent<PlayerBuildController>();
            }

            Sync(runState, controller);
            return controller;
        }

        public static void Sync(RunState runState, PlayerBuildController controller)
        {
            if (controller == null)
            {
                return;
            }

            IReadOnlyCollection<BuildUpgradeId> upgrades = runState != null
                ? runState.BuildState.OwnedUpgrades
                : null;
            controller.ReplaceUpgrades(upgrades);
        }

        /// <summary>
        /// Grants one selected upgrade to the current run and its active player copy.
        /// </summary>
        public static bool GrantUpgrade(
            RunState runState,
            PlayerBuildController controller,
            BuildUpgradeId id)
        {
            if (runState == null || !runState.IsActive || !runState.BuildState.GrantUpgrade(id))
            {
                return false;
            }

            controller?.GrantUpgrade(id);
            return true;
        }
    }
}
