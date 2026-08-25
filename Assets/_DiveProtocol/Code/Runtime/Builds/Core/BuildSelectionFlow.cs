using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Opens one run-scoped build offer and commits its selected upgrade through the run bridge.
    /// </summary>
    public static class BuildSelectionFlow
    {
        private static readonly BuildChoiceProvider ChoiceProvider = new();

        /// <summary>Compatibility entry point for debug callers; uses the active scene's configured node.</summary>
        public static bool TryOpen(
            GameObject interactor,
            int choiceCount,
            Action selectionCompleted = null)
        {
            return LevelBuildSelectionCatalog.TryGetForActiveScene(out LevelBuildSelectionDefinition definition) &&
                   TryOpen(interactor, definition, choiceCount, selectionCompleted);
        }

        public static bool TryOpen(
            GameObject interactor,
            LevelBuildSelectionDefinition definition,
            int choiceCount,
            Action selectionCompleted = null)
        {
            if (interactor == null ||
                !AppRoot.TryGetInstance(out AppRoot appRoot) ||
                appRoot.RunManager.CurrentRun == null ||
                !appRoot.RunManager.CurrentRun.IsActive)
            {
                return false;
            }

            RunState runState = appRoot.RunManager.CurrentRun;
            PlayerBuildController playerBuild = BuildRunBridge.EnsureAndSync(
                interactor.transform,
                runState);
            if (playerBuild == null)
            {
                return false;
            }

            if (definition == null || runState.BuildState.HasClaimedSelectionNode(definition.NodeId))
            {
                return false;
            }

            IReadOnlyList<BuildUpgradeDefinition> choices = ChoiceProvider.GetChoices(
                runState.BuildState.OwnedUpgrades,
                definition,
                runState.Seed,
                Mathf.Max(1, choiceCount));
            if (choices.Count == 0)
            {
                return false;
            }

            return BuildSelectionUI.GetOrCreate().Show(choices, selectedId =>
            {
                if (!BuildRunBridge.GrantUpgrade(runState, playerBuild, selectedId))
                {
                    return false;
                }

                if (!runState.BuildState.TryClaimSelectionNode(definition.NodeId))
                {
                    return false;
                }

                selectionCompleted?.Invoke();
                return true;
            });
        }
    }
}
