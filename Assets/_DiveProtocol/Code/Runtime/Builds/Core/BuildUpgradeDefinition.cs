using System;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Pure C# definition data for one build upgrade.
    /// </summary>
    [Serializable]
    public sealed class BuildUpgradeDefinition
    {
        public BuildUpgradeDefinition(
            BuildUpgradeId id,
            BuildBranch branch,
            BuildUpgradeKind kind,
            string displayName,
            string shortDescription,
            string longDescription,
            int tier,
            params BuildUpgradeId[] prerequisites)
        {
            Id = id;
            Branch = branch;
            Kind = kind;
            DisplayName = displayName;
            ShortDescription = shortDescription;
            LongDescription = longDescription;
            Tier = tier;
            Prerequisites = prerequisites ?? Array.Empty<BuildUpgradeId>();
        }

        public BuildUpgradeId Id { get; }
        public BuildBranch Branch { get; }
        public BuildUpgradeKind Kind { get; }
        public string DisplayName { get; }
        public string ShortDescription { get; }
        public string LongDescription { get; }
        public int Tier { get; }
        public bool IsCore => Kind == BuildUpgradeKind.Core;
        public BuildUpgradeId[] Prerequisites { get; }
    }
}
