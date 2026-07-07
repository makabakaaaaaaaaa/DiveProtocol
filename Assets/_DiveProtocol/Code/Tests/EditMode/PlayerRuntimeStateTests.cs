using NUnit.Framework;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class PlayerRuntimeStateTests
    {
        [Test]
        public void ConstructorInitializesFullHealthAndRejectsInvalidValues()
        {
            var player = new PlayerRuntimeState(100, 6, 12);
            Assert.That(player.CurrentHealth, Is.EqualTo(player.MaxHealth));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PlayerRuntimeState(0, 0, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PlayerRuntimeState(1, -1, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PlayerRuntimeState(1, 0, -1));
        }

        [Test]
        public void HealthAndAmmoRemainWithinValidBounds()
        {
            var player = new PlayerRuntimeState(100, 2, 5);

            Assert.That(player.TakeDamage(150), Is.EqualTo(100));
            Assert.That(player.CurrentHealth, Is.Zero);
            Assert.That(player.Heal(250), Is.EqualTo(100));
            Assert.That(player.CurrentHealth, Is.EqualTo(100));
            Assert.That(player.TrySpendHealth(100), Is.False);
            Assert.That(player.TrySpendHealth(100, true), Is.True);

            Assert.That(player.TryConsumeLoadedAmmo(2), Is.True);
            Assert.That(player.TryConsumeLoadedAmmo(), Is.False);
            Assert.That(player.Reload(6), Is.EqualTo(5));
            Assert.That(player.LoadedAmmo, Is.EqualTo(5));
            Assert.That(player.ReserveAmmo, Is.Zero);
        }

        [Test]
        public void ReloadUsesOnlyNeededReserveAndNeverExceedsCapacity()
        {
            var player = new PlayerRuntimeState(100, 4, 20);
            Assert.That(player.Reload(6), Is.EqualTo(2));
            Assert.That(player.LoadedAmmo, Is.EqualTo(6));
            Assert.That(player.ReserveAmmo, Is.EqualTo(18));
            Assert.That(player.Reload(6), Is.Zero);
        }

        [Test]
        public void SeparatePlayersDoNotShareState()
        {
            var first = new PlayerRuntimeState(100, 6, 12);
            var second = new PlayerRuntimeState(100, 6, 12);
            first.TakeDamage(50);
            first.TryConsumeLoadedAmmo();
            Assert.That(second.CurrentHealth, Is.EqualTo(100));
            Assert.That(second.LoadedAmmo, Is.EqualTo(6));
        }
    }
}
