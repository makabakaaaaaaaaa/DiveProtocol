using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Deterministic build choice generator for future three-choice UI.
    /// </summary>
    public sealed class BuildChoiceProvider
    {
        private static readonly BuildUpgradeId[] CoreChoices =
        {
            BuildUpgradeId.RedMarrow_Overdraft,
            BuildUpgradeId.OpticNerve_Calibration,
            BuildUpgradeId.Humus_Sympathy
        };

        public IReadOnlyList<BuildUpgradeDefinition> GetChoices(
            PlayerBuildState state,
            int levelIndex,
            int seed,
            int choiceCount = 3)
        {
            return GetChoices(
                state != null ? state.OwnedUpgrades : null,
                levelIndex,
                seed,
                choiceCount);
        }

        public IReadOnlyList<BuildUpgradeDefinition> GetChoices(
            IReadOnlyCollection<BuildUpgradeId> ownedUpgrades,
            int levelIndex,
            int seed,
            int choiceCount = 3)
        {
            List<BuildUpgradeDefinition> choices = new();
            choiceCount = Mathf.Max(1, choiceCount);

            if (ownedUpgrades == null || !HasAnyCore(ownedUpgrades))
            {
                for (int i = 0; i < CoreChoices.Length && choices.Count < choiceCount; i++)
                {
                    if (ownedUpgrades == null || !ownedUpgrades.Contains(CoreChoices[i]))
                    {
                        choices.Add(BuildCatalog.Get(CoreChoices[i]));
                    }
                }

                return choices;
            }

            List<BuildUpgradeDefinition> candidates = new();
            List<BuildUpgradeDefinition> offBranchCandidates = new();
            for (int i = 0; i < BuildCatalog.AllDefinitions.Count; i++)
            {
                BuildUpgradeDefinition definition = BuildCatalog.AllDefinitions[i];
                if (ownedUpgrades.Contains(definition.Id))
                {
                    continue;
                }

                if (definition.IsCore)
                {
                    offBranchCandidates.Add(definition);
                    continue;
                }

                if (HasBranchCore(ownedUpgrades, definition.Branch))
                {
                    candidates.Add(definition);
                }
                else
                {
                    offBranchCandidates.Add(definition);
                }
            }

            Shuffle(candidates, seed, levelIndex);
            Shuffle(offBranchCandidates, seed ^ 0x5F3759DF, levelIndex);

            AddUntilFull(choices, candidates, choiceCount);
            AddUntilFull(choices, offBranchCandidates, choiceCount);

            if (choices.Count < choiceCount)
            {
                Debug.LogWarning($"[Builds] Only generated {choices.Count} build choices.");
            }

            return choices;
        }

        private static bool HasAnyCore(IReadOnlyCollection<BuildUpgradeId> ownedUpgrades)
        {
            return HasBranchCore(ownedUpgrades, BuildBranch.RedMarrow) ||
                   HasBranchCore(ownedUpgrades, BuildBranch.OpticNerve) ||
                   HasBranchCore(ownedUpgrades, BuildBranch.HumusSymbiosis);
        }

        private static bool HasBranchCore(
            IReadOnlyCollection<BuildUpgradeId> ownedUpgrades,
            BuildBranch branch)
        {
            foreach (BuildUpgradeId id in ownedUpgrades)
            {
                if (BuildCatalog.TryGet(id, out BuildUpgradeDefinition definition) &&
                    definition.Branch == branch &&
                    definition.IsCore)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddUntilFull(
            List<BuildUpgradeDefinition> destination,
            List<BuildUpgradeDefinition> source,
            int choiceCount)
        {
            for (int i = 0; i < source.Count && destination.Count < choiceCount; i++)
            {
                destination.Add(source[i]);
            }
        }

        private static void Shuffle(
            List<BuildUpgradeDefinition> list,
            int seed,
            int levelIndex)
        {
            System.Random random = new(seed + levelIndex * 1009);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
