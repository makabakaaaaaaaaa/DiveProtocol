using DiveProtocol.Builds;
using NUnit.Framework;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class RunBuildStateTests
    {
        [Test]
        public void GrantIsDeduplicatedAndClearRemovesEveryUpgrade()
        {
            var state = new RunBuildState();

            Assert.That(state.GrantUpgrade(BuildUpgradeId.RedMarrow_Overdraft), Is.True);
            Assert.That(state.GrantUpgrade(BuildUpgradeId.RedMarrow_Overdraft), Is.False);
            Assert.That(state.OwnedUpgrades, Has.Count.EqualTo(1));

            state.Clear();

            Assert.That(state.OwnedUpgrades, Is.Empty);
            Assert.That(state.HasClaimedSelectionNode("node-a"), Is.False);
        }

        [Test]
        public void FixedSelectionNodeCanOnlyBeClaimedOncePerRun()
        {
            var state = new RunBuildState();

            Assert.That(state.TryClaimSelectionNode("node-a"), Is.True);
            Assert.That(state.TryClaimSelectionNode("node-a"), Is.False);
            Assert.That(state.HasClaimedSelectionNode("node-a"), Is.True);

            state.Clear();

            Assert.That(state.HasClaimedSelectionNode("node-a"), Is.False);
        }
    }
}
