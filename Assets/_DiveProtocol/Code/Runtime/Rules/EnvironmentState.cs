using System;

namespace DiveProtocol
{
    /// <summary>Seed-derived environment parameters for the current run.</summary>
    [Serializable]
    public sealed class EnvironmentState
    {
        private EnvironmentState(CorpseActivity corpseActivity, ResourceDensity resourceDensity)
        {
            CorpseActivity = corpseActivity;
            ResourceDensity = resourceDensity;
        }

        public CorpseActivity CorpseActivity { get; }
        public ResourceDensity ResourceDensity { get; }

        /// <summary>Creates deterministic environment values without touching Unity's global random state.</summary>
        public static EnvironmentState CreateFromSeed(int seed)
        {
            var random = new Random(seed);
            var corpseActivity = (CorpseActivity)random.Next(0, 2);
            var resourceDensity = (ResourceDensity)random.Next(0, 3);
            return new EnvironmentState(corpseActivity, resourceDensity);
        }
    }
}
