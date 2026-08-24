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

            var choices = provider.GetChoices(state.OwnedUpgrades, 0, 1234, 3);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    BuildUpgradeId.RedMarrow_Overdraft,
                    BuildUpgradeId.OpticNerve_Calibration,
                    BuildUpgradeId.Humus_Sympathy
                },
                choices.Select(choice => choice.Id));
        }
    }
}
