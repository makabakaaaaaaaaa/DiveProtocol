using DiveProtocol.UI;
using NUnit.Framework;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class LevelStatusCatalogTests
    {
        [TestCase(SceneNames.Level01Drainage, "D-01", LevelStatusData.AlertLevel.Low)]
        [TestCase(SceneNames.Level02Containment, "D-02", LevelStatusData.AlertLevel.Medium)]
        [TestCase(SceneNames.Level03MaintenanceTransfer, "D-03", LevelStatusData.AlertLevel.High)]
        [TestCase(SceneNames.Level04FacilityCore, "D-04", LevelStatusData.AlertLevel.Critical)]
        public void GameplayScenesExposeTheExpectedBaselineStatus(
            string sceneName,
            string expectedSector,
            LevelStatusData.AlertLevel expectedAlert)
        {
            LevelStatusData status = LevelStatusCatalog.GetForScene(sceneName);

            Assert.That(status.SectorCode, Is.EqualTo(expectedSector));
            Assert.That(status.CurrentAlertLevel, Is.EqualTo(expectedAlert));
        }
    }
}
