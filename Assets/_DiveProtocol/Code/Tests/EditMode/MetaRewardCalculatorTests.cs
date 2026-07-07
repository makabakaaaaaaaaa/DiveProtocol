using NUnit.Framework;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class MetaRewardCalculatorTests
    {
        [TestCase(100, 10)]
        [TestCase(0, 0)]
        [TestCase(-100, 0)]
        public void CurrencyConversionIsDeterministicAndNonNegative(int score, int expected)
        {
            Assert.That(MetaRewardCalculator.CalculateCurrency(score), Is.EqualTo(expected));
            Assert.That(MetaRewardCalculator.CalculateCurrency(score), Is.EqualTo(expected));
        }

        [TestCase(RunEndReason.DemoCompleted, true, true)]
        [TestCase(RunEndReason.BossDefeated, true, true)]
        [TestCase(RunEndReason.Extracted, true, true)]
        [TestCase(RunEndReason.PlayerDied, true, false)]
        [TestCase(RunEndReason.Aborted, false, false)]
        public void EligibilityAndSuccessMatchRules(RunEndReason reason, bool eligible, bool successful)
        {
            Assert.That(MetaRewardCalculator.IsEligible(reason), Is.EqualTo(eligible));
            Assert.That(MetaRewardCalculator.IsSuccessful(reason), Is.EqualTo(successful));
        }
    }
}
