using System;

namespace DiveProtocol
{
    /// <summary>Centralizes eligibility and temporary score-to-currency conversion.</summary>
    public static class MetaRewardCalculator
    {
        private const int _currencyScoreDivisor = 10;

        public static bool IsEligible(RunEndReason endReason)
        {
            return endReason == RunEndReason.DemoCompleted ||
                   endReason == RunEndReason.BossDefeated ||
                   endReason == RunEndReason.Extracted ||
                   endReason == RunEndReason.PlayerDied;
        }

        public static bool IsSuccessful(RunEndReason endReason)
        {
            return endReason == RunEndReason.DemoCompleted ||
                   endReason == RunEndReason.BossDefeated ||
                   endReason == RunEndReason.Extracted;
        }

        public static int CalculateCurrency(int totalScore)
        {
            return Math.Max(0, totalScore / _currencyScoreDivisor);
        }
    }
}
