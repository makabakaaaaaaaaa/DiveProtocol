using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Runtime-owned upgrades for one player in one run.
    /// </summary>
    [Serializable]
    public sealed class PlayerBuildState
    {
        [SerializeField] private List<BuildUpgradeId> ownedUpgrades = new();

        public event Action<BuildUpgradeId> UpgradeGranted;
        public event Action BuildsReset;

        public IReadOnlyCollection<BuildUpgradeId> OwnedUpgrades => ownedUpgrades;

        public bool HasUpgrade(BuildUpgradeId id)
        {
            return ownedUpgrades.Contains(id);
        }

        public bool HasBranchCore(BuildBranch branch)
        {
            for (int i = 0; i < ownedUpgrades.Count; i++)
            {
                if (!BuildCatalog.TryGet(ownedUpgrades[i], out BuildUpgradeDefinition definition))
                {
                    continue;
                }

                if (definition.Branch == branch && definition.IsCore)
                {
                    return true;
                }
            }

            return false;
        }

        public void GrantUpgrade(BuildUpgradeId id)
        {
            if (HasUpgrade(id))
            {
                return;
            }

            if (BuildCatalog.TryGet(id, out BuildUpgradeDefinition definition) &&
                !definition.IsCore &&
                !HasBranchCore(definition.Branch))
            {
                Debug.LogWarning($"[Builds] Granting component '{id}' before its branch core is owned.");
            }

            ownedUpgrades.Add(id);
            UpgradeGranted?.Invoke(id);
        }

        public void ClearAllUpgrades()
        {
            if (ownedUpgrades.Count == 0)
            {
                return;
            }

            ownedUpgrades.Clear();
            BuildsReset?.Invoke();
        }
    }
}
