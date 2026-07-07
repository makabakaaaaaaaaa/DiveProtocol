#if UNITY_EDITOR || DEVELOPMENT_BUILD
using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class DebugOverlayTests
    {
        [TestCase(1920, 1080)]
        [TestCase(640, 360)]
        [TestCase(240, 160)]
        public void CalculatedWindowAlwaysFitsInsideGameView(int screenWidth, int screenHeight)
        {
            var window = DebugOverlay.CalculateWindowRect(new Rect(9999f, 9999f, 420f, 680f), screenWidth, screenHeight);

            Assert.That(window.width, Is.GreaterThan(0f));
            Assert.That(window.height, Is.GreaterThan(0f));
            Assert.That(window.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(window.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(window.xMax, Is.LessThanOrEqualTo(screenWidth));
            Assert.That(window.yMax, Is.LessThanOrEqualTo(screenHeight));
            Assert.That(window.width, Is.LessThanOrEqualTo(520f));
        }
    }
}
#endif
