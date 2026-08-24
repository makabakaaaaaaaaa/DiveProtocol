using System;
using System.Collections.Generic;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Run-owned build upgrades. This is the authoritative source across player respawns and level loads.
    /// </summary>
    [Serializable]
    public sealed class RunBuildState
    {
        private readonly List<BuildUpgradeId> _ownedUpgrades = new();

        public IReadOnlyCollection<BuildUpgradeId> OwnedUpgrades => _ownedUpgrades;

        public bool HasUpgrade(BuildUpgradeId id)
        {
            return _ownedUpgrades.Contains(id);
        }

        public bool HasBranchCore(BuildBranch branch)
        {
            for (int i = 0; i < _ownedUpgrades.Count; i++)
            {
                if (BuildCatalog.TryGet(_ownedUpgrades[i], out BuildUpgradeDefinition definition) &&
                    definition.Branch == branch &&
                    definition.IsCore)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds an upgrade once. Selection validation is performed by the caller.
        /// </summary>
        public bool GrantUpgrade(BuildUpgradeId id)
        {
            if (HasUpgrade(id) || !BuildCatalog.TryGet(id, out _))
            {
                return false;
            }

            _ownedUpgrades.Add(id);
            return true;
        }

        /// <summary>
        /// Removes every run-only upgrade.
        /// </summary>
        public void Clear()
        {
            _ownedUpgrades.Clear();
        }
    }
}
