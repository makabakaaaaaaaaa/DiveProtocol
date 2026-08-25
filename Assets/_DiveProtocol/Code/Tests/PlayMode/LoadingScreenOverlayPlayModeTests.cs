using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class LoadingScreenOverlayPlayModeTests
    {
        [UnityTest]
        public IEnumerator SharedOverlayShowsTerminalPresentationAndHidesCleanly()
        {
            LoadingScreenOverlayService.Show("TEST ROUTE", "TEST DESCRIPTION");
            yield return null;

            LoadingScreenOverlay overlay = LoadingScreenOverlay.Instance;
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.IsVisible, Is.True);
            Assert.That(overlay.name, Does.StartWith("LoadingScreenOverlay"));

            LoadingScreenOverlayService.SetProgress(0.5f);
            LoadingScreenOverlayService.Hide();
            yield return null;

            Assert.That(overlay.IsVisible, Is.True);

            yield return new WaitForSecondsRealtime(overlay.MinimumLoadingDuration + 0.1f);
            Assert.That(overlay.IsVisible, Is.False);
        }
    }
}
