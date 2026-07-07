using NUnit.Framework;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class ScoreStateTests
    {
        [Test]
        public void InitialAndRecordedScoresAreDeterministic()
        {
            var score = new ScoreState();
            Assert.That(score.TotalScore, Is.Zero);
            score.RecordEnemyDefeated(2);
            score.RecordRoomDiscovered(3);
            score.RecordNewItemDiscovered();
            score.RecordBossDefeated();
            score.RecordRuleJudgement(true);
            score.AddBonusScore(7);

            var firstRead = score.TotalScore;
            var secondRead = score.TotalScore;
            Assert.That(firstRead, Is.EqualTo(secondRead));
            Assert.That(firstRead, Is.GreaterThan(0));
            Assert.That(score.EnemiesDefeated, Is.EqualTo(2));
            Assert.That(score.RoomsDiscovered, Is.EqualTo(3));
            Assert.That(score.NewItemsDiscovered, Is.EqualTo(1));
            Assert.That(score.BossesDefeated, Is.EqualTo(1));
        }

        [Test]
        public void NegativeCountsAreRejected()
        {
            var score = new ScoreState();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => score.RecordEnemyDefeated(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => score.AddBonusScore(-1));
            Assert.That(score.TotalScore, Is.Zero);
        }

        [Test]
        public void EqualInputsProduceEqualTotals()
        {
            var first = new ScoreState();
            var second = new ScoreState();
            first.RecordLevelCompleted();
            second.RecordLevelCompleted();
            first.RecordEnemyDefeated(4);
            second.RecordEnemyDefeated(4);
            Assert.That(first.TotalScore, Is.EqualTo(second.TotalScore));
        }
    }
}
