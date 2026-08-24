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

        public static bool TryOpen(
            GameObject interactor,
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

            IReadOnlyList<BuildUpgradeDefinition> choices = ChoiceProvider.GetChoices(
                runState.BuildState.OwnedUpgrades,
                runState.CurrentLevelIndex,
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

                selectionCompleted?.Invoke();
                return true;
            });
        }
    }
}
