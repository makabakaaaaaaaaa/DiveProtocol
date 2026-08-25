using System.Linq;
using DiveProtocol.Builds;
using NUnit.Framework;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class BuildChoiceProviderTests
    {
        [Test]
        public void FirstOfferContainsTheThreeBuildCores()
        {
            var provider = new BuildChoiceProvider();
            var state = new RunBuildState();

            Assert.That(
                LevelBuildSelectionCatalog.TryGetForScene(
                    SceneNames.Level02Containment,
                    out LevelBuildSelectionDefinition firstNode),
                Is.True);

            var choices = provider.GetChoices(state.OwnedUpgrades, firstNode, 1234, 3);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    BuildUpgradeId.RedMarrow_Overdraft,
                    BuildUpgradeId.OpticNerve_Calibration,
                    BuildUpgradeId.Humus_Sympathy
                },
                choices.Select(choice => choice.Id));
        }

        [Test]
        public void FixedNodesUseTheOwnedBranchAndKeepAwakeningInTheFinalOffer()
        {
            var provider = new BuildChoiceProvider();
            var state = new RunBuildState();
            state.GrantUpgrade(BuildUpgradeId.RedMarrow_Overdraft);

            Assert.That(LevelBuildSelectionCatalog.TryGetForScene(SceneNames.Level01Drainage, out LevelBuildSelectionDefinition reinforcement), Is.True);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    BuildUpgradeId.RedMarrow_BloodBulletCompression,
                    BuildUpgradeId.RedMarrow_CoagulationReflex,
                    BuildUpgradeId.RedMarrow_OrganCollateral
                },
                provider.GetChoices(state.OwnedUpgrades, reinforcement, 42, 3).Select(choice => choice.Id));

            Assert.That(LevelBuildSelectionCatalog.TryGetForScene(SceneNames.Level04FacilityCore, out LevelBuildSelectionDefinition awakening), Is.True);
            var finalOffer = provider.GetChoices(state.OwnedUpgrades, awakening, 42, 3);
            Assert.That(finalOffer, Has.Count.EqualTo(3));
            Assert.That(finalOffer.Select(choice => choice.Id), Does.Contain(BuildUpgradeId.RedMarrow_SacrificeProtocol));
        }

        [Test]
        public void RandomMinorDropsOnlyUseUnownedComponentsFromTheChosenBranch()
        {
            var provider = new BuildChoiceProvider();
            var state = new RunBuildState();
            state.GrantUpgrade(BuildUpgradeId.Humus_Sympathy);

            var candidates = provider.GetMinorUpgradeCandidates(state.OwnedUpgrades);
            BuildUpgradeDefinition selected = provider.GetRandomMinorUpgrade(state.OwnedUpgrades, 99);

            Assert.That(candidates, Is.Not.Empty);
            Assert.That(candidates.All(candidate =>
                candidate.Branch == BuildBranch.HumusSymbiosis &&
                candidate.Kind == BuildUpgradeKind.Component &&
                candidate.Tier is >= 1 and <= 2), Is.True);
            Assert.That(selected, Is.Not.Null);
            Assert.That(candidates.Select(candidate => candidate.Id), Does.Contain(selected.Id));
        }
    }
}
