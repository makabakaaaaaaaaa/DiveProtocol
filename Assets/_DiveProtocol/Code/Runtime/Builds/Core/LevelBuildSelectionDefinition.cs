using System;
using System.Collections.Generic;

namespace DiveProtocol.Builds
{
    /// <summary>Scene-independent rules for one fixed, run-scoped build selection node.</summary>
    public sealed class LevelBuildSelectionDefinition
    {
        public LevelBuildSelectionDefinition(
            string sceneName,
            string nodeId,
            int progressionIndex,
            BuildSelectionTier tier,
            IReadOnlyList<BuildUpgradeId> primaryPool,
            IReadOnlyList<BuildUpgradeId> fallbackPool = null)
        {
            SceneName = sceneName ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            ProgressionIndex = progressionIndex;
            Tier = tier;
            PrimaryPool = primaryPool ?? Array.Empty<BuildUpgradeId>();
            FallbackPool = fallbackPool ?? Array.Empty<BuildUpgradeId>();
        }

        public string SceneName { get; }
        public string NodeId { get; }
        public int ProgressionIndex { get; }
        public BuildSelectionTier Tier { get; }
        public IReadOnlyList<BuildUpgradeId> PrimaryPool { get; }
        public IReadOnlyList<BuildUpgradeId> FallbackPool { get; }
    }
}
