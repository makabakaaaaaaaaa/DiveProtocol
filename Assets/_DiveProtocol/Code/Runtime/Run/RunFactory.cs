using System;
using DiveProtocol.Builds;

namespace DiveProtocol
{
    /// <summary>Constructs complete, independent state graphs for new runs.</summary>
    public static class RunFactory
    {
        public const string InitialLevelId = "L01_Drainage";
        public const int InitialLevelIndex = 0;

        public static RunState Create(int seed, GameConfig gameConfig)
        {
            if (gameConfig == null)
            {
                throw new ArgumentNullException(nameof(gameConfig));
            }

            var player = new PlayerRuntimeState(
                gameConfig.InitialMaxHealth,
                gameConfig.InitialLoadedAmmo,
                gameConfig.InitialReserveAmmo);
            var environment = EnvironmentState.CreateFromSeed(seed);
            var inventory = new InventoryState();
            var score = new ScoreState();
            var buildState = new RunBuildState();

            return new RunState(
                seed,
                Guid.NewGuid().ToString("N"),
                InitialLevelId,
                InitialLevelIndex,
                DateTime.UtcNow,
                player,
                environment,
                inventory,
                score,
                buildState);
        }
    }
}
