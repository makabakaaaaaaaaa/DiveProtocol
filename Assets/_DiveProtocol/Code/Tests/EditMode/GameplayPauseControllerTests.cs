using DiveProtocol.Gameplay;
using DiveProtocol.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class GameplayPauseControllerTests
    {
        [TearDown]
        public void TearDown()
        {
            GameplayInputLock.ClearAll();
            GameplayPauseController.RestoreGlobalPauseSideEffects();
        }

        [Test]
        public void ForceResumeHidesMenuAndRestoresGlobalState()
        {
            var fixture = CreateFixture();
            try
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;

                fixture.Controller.ForceResume();

                Assert.That(fixture.MenuRoot.activeSelf, Is.False);
                Assert.That(fixture.Blocker.activeSelf, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(AudioListener.pause, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        [Test]
        public void PauseAndResumeAreIdempotent()
        {
            var fixture = CreateFixture();
            try
            {
                fixture.Controller.PauseGame();
                fixture.Controller.PauseGame();

                Assert.That(fixture.Controller.IsPaused, Is.True);
                Assert.That(fixture.MenuRoot.activeSelf, Is.True);
                Assert.That(fixture.Blocker.activeSelf, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0f));
                Assert.That(GameplayInputLock.IsLocked, Is.True);

                fixture.Controller.ResumeGame();
                fixture.Controller.ResumeGame();

                Assert.That(fixture.Controller.IsPaused, Is.False);
                Assert.That(fixture.MenuRoot.activeSelf, Is.False);
                Assert.That(fixture.Blocker.activeSelf, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(GameplayInputLock.IsLocked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        private static PauseFixture CreateFixture()
        {
            var root = new GameObject("PauseFixture");
            var menuRoot = new GameObject("PauseMenuRoot");
            var blocker = new GameObject("PauseBlocker");
            var pauseButtonObject = new GameObject("PauseButton");
            var resumeButtonObject = new GameObject("ResumeButton");

            menuRoot.transform.SetParent(root.transform);
            blocker.transform.SetParent(root.transform);
            pauseButtonObject.transform.SetParent(root.transform);
            resumeButtonObject.transform.SetParent(root.transform);

            var pauseButton = pauseButtonObject.AddComponent<Button>();
            var resumeButton = resumeButtonObject.AddComponent<Button>();
            var controller = root.AddComponent<GameplayPauseController>();

            var serializedObject = new SerializedObject(controller);
            serializedObject.FindProperty("_pauseMenuRoot").objectReferenceValue = menuRoot;
            serializedObject.FindProperty("_pauseBlocker").objectReferenceValue = blocker;
            serializedObject.FindProperty("_pauseButton").objectReferenceValue = pauseButton;
            serializedObject.FindProperty("_resumeButton").objectReferenceValue = resumeButton;
            serializedObject.FindProperty("_pauseAudio").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            controller.ForceResume();

            return new PauseFixture(root, controller, menuRoot, blocker);
        }

        private readonly struct PauseFixture
        {
            public PauseFixture(
                GameObject root,
                GameplayPauseController controller,
                GameObject menuRoot,
                GameObject blocker)
            {
                Root = root;
                Controller = controller;
                MenuRoot = menuRoot;
                Blocker = blocker;
            }

            public GameObject Root { get; }
            public GameplayPauseController Controller { get; }
            public GameObject MenuRoot { get; }
            public GameObject Blocker { get; }
        }
    }
}
