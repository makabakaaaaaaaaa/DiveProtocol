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

        /// <summary>Returns unowned branch components eligible for a random enemy minor-build drop.</summary>
        public IReadOnlyList<BuildUpgradeDefinition> GetMinorUpgradeCandidates(
            IReadOnlyCollection<BuildUpgradeId> ownedUpgrades)
        {
            if (ownedUpgrades == null || !HasAnyCore(ownedUpgrades))
            {
                return Array.Empty<BuildUpgradeDefinition>();
            }

            BuildBranch branch = GetOwnedCoreBranch(ownedUpgrades);
            return BuildCatalog.AllDefinitions
                .Where(definition =>
                    !ownedUpgrades.Contains(definition.Id) &&
                    definition.Branch == branch &&
                    IsMinorDropEligible(definition))
                .ToArray();
        }

        /// <summary>Chooses one unowned, non-core, non-awakening component for a random enemy drop.</summary>
        public BuildUpgradeDefinition GetRandomMinorUpgrade(
            IReadOnlyCollection<BuildUpgradeId> ownedUpgrades,
            int seed)
        {
            IReadOnlyList<BuildUpgradeDefinition> candidates = GetMinorUpgradeCandidates(ownedUpgrades);
            if (candidates.Count == 0)
            {
                return null;
            }

            var random = new System.Random(seed);
            return candidates[random.Next(candidates.Count)];
        }

        /// <summary>Generates a fixed level-node offer for the branch already chosen in this run.</summary>
        public IReadOnlyList<BuildUpgradeDefinition> GetChoices(
            IReadOnlyCollection<BuildUpgradeId> ownedUpgrades,
            LevelBuildSelectionDefinition definition,
            int seed,
            int choiceCount = 3)
        {
            if (definition == null)
            {
                return Array.Empty<BuildUpgradeDefinition>();
            }

            if (ownedUpgrades == null || !HasAnyCore(ownedUpgrades))
            {
                return GetCoreChoices(ownedUpgrades, choiceCount);
            }

            BuildBranch branch = GetOwnedCoreBranch(ownedUpgrades);
            var choices = new List<BuildUpgradeDefinition>();
            AddFromPool(choices, definition.PrimaryPool, ownedUpgrades, branch, seed, definition.ProgressionIndex, choiceCount);
            AddFromPool(choices, definition.FallbackPool, ownedUpgrades, branch, seed ^ 0x5F3759DF, definition.ProgressionIndex, choiceCount);
            return choices;
        }

        private static bool HasAnyCore(IReadOnlyCollection<BuildUpgradeId> ownedUpgrades)
        {
            return HasBranchCore(ownedUpgrades, BuildBranch.RedMarrow) ||
                   HasBranchCore(ownedUpgrades, BuildBranch.OpticNerve) ||
                   HasBranchCore(ownedUpgrades, BuildBranch.HumusSymbiosis);
        }

        private static IReadOnlyList<BuildUpgradeDefinition> GetCoreChoices(
            IReadOnlyCollection<BuildUpgradeId> ownedUpgrades,
            int choiceCount)
        {
            var choices = new List<BuildUpgradeDefinition>();
            for (int index = 0; index < CoreChoices.Length && choices.Count < Mathf.Max(1, choiceCount); index++)
            {
                if (ownedUpgrades == null || !ownedUpgrades.Contains(CoreChoices[index]))
                {
                    choices.Add(BuildCatalog.Get(CoreChoices[index]));
                }
            }

            return choices;
        }

        private static BuildBranch GetOwnedCoreBranch(IReadOnlyCollection<BuildUpgradeId> ownedUpgrades)
        {
            foreach (BuildBranch branch in (BuildBranch[])Enum.GetValues(typeof(BuildBranch)))
            {
                if (HasBranchCore(ownedUpgrades, branch)) return branch;
            }

            return BuildBranch.RedMarrow;
        }

        private static void AddFromPool(
            List<BuildUpgradeDefinition> destination,
            IReadOnlyList<BuildUpgradeId> pool,
            IReadOnlyCollection<BuildUpgradeId> ownedUpgrades,
            BuildBranch branch,
            int seed,
            int levelIndex,
            int choiceCount)
        {
            if (pool == null || destination.Count >= choiceCount) return;

            var candidates = new List<BuildUpgradeDefinition>();
            foreach (BuildUpgradeId id in pool)
            {
                if (ownedUpgrades.Contains(id) || !BuildCatalog.TryGet(id, out BuildUpgradeDefinition definition) ||
                    definition.Branch != branch || destination.Any(choice => choice.Id == id))
                {
                    continue;
                }

                candidates.Add(definition);
            }

            Shuffle(candidates, seed, levelIndex);
            AddUntilFull(destination, candidates, choiceCount);
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

        private static bool IsMinorDropEligible(BuildUpgradeDefinition definition)
        {
            if (definition == null || definition.Kind != BuildUpgradeKind.Component || definition.Tier is < 1 or > 2)
            {
                return false;
            }

            // The current weapon has no reload action yet, so this modifier cannot create an immediate pickup effect.
            return definition.Id != BuildUpgradeId.OpticNerve_CalmShot;
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
