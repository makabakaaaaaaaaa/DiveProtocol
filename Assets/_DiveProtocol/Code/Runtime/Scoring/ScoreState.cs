using System;

namespace DiveProtocol
{
    /// <summary>Accumulates score inputs for one run.</summary>
    [Serializable]
    public sealed class ScoreState
    {
        public int LevelsCompleted { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public int RoomsDiscovered { get; private set; }
        public int NewItemsDiscovered { get; private set; }
        public int BossesDefeated { get; private set; }
        public int RuleJudgementsCorrect { get; private set; }
        public int RuleJudgementsTotal { get; private set; }
        public int BonusScore { get; private set; }
        public int TotalScore => ScoreCalculator.Calculate(this);

        public void RecordLevelCompleted(int count = 1) => LevelsCompleted = AddNonNegative(LevelsCompleted, count);
        public void RecordEnemyDefeated(int count = 1) => EnemiesDefeated = AddNonNegative(EnemiesDefeated, count);
        public void RecordRoomDiscovered(int count = 1) => RoomsDiscovered = AddNonNegative(RoomsDiscovered, count);
        public void RecordNewItemDiscovered(int count = 1) => NewItemsDiscovered = AddNonNegative(NewItemsDiscovered, count);
        public void RecordBossDefeated(int count = 1) => BossesDefeated = AddNonNegative(BossesDefeated, count);

        public void RecordRuleJudgement(bool wasCorrect)
        {
            RuleJudgementsTotal = AddNonNegative(RuleJudgementsTotal, 1);
            if (wasCorrect)
            {
                RuleJudgementsCorrect = AddNonNegative(RuleJudgementsCorrect, 1);
            }
        }

        public void AddBonusScore(int amount)
        {
            BonusScore = AddNonNegative(BonusScore, amount);
        }

        private static int AddNonNegative(int currentValue, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Score counters cannot be reduced below their recorded value.");
            }

            return (int)Math.Min((long)currentValue + amount, int.MaxValue);
        }
    }
}
