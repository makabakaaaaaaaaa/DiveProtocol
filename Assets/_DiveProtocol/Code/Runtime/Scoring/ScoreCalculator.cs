using System;

namespace DiveProtocol
{
    /// <summary>Centralizes temporary score weights until formal balancing exists.</summary>
    public static class ScoreCalculator
    {
        private const int _levelCompletedPoints = 1000;
        private const int _enemyDefeatedPoints = 100;
        private const int _roomDiscoveredPoints = 25;
        private const int _newItemDiscoveredPoints = 100;
        private const int _bossDefeatedPoints = 5000;
        private const int _correctRuleJudgementPoints = 250;

        public static int Calculate(ScoreState score)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            var total =
                (long)score.LevelsCompleted * _levelCompletedPoints +
                (long)score.EnemiesDefeated * _enemyDefeatedPoints +
                (long)score.RoomsDiscovered * _roomDiscoveredPoints +
                (long)score.NewItemsDiscovered * _newItemDiscoveredPoints +
                (long)score.BossesDefeated * _bossDefeatedPoints +
                (long)score.RuleJudgementsCorrect * _correctRuleJudgementPoints +
                score.BonusScore;

            return (int)Math.Min(total, int.MaxValue);
        }
    }
}
