using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using DiveProtocol.Bosses;
using DiveProtocol.Doors;
using DiveProtocol.RoomVisibility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class L04RoomVisibilityIntegrationPlayModeTests
    {
        private string _saveDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!SceneTestUtility.SystemScenesAreAvailable(out string reason))
            {
                Assert.Ignore(reason);
            }

            _saveDirectory = SceneTestUtility.CreateTemporarySaveDirectory();
            yield return SceneTestUtility.LoadBootstrap(_saveDirectory);
            yield return SceneTestUtility.WaitForLoadingComplete();
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                SceneNames.Level03MaintenanceTransfer,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(SceneNames.Level03MaintenanceTransfer);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return SceneTestUtility.Cleanup(_saveDirectory);
        }

        [UnityTest]
        public IEnumerator L03TimerAndL04BossArenaRoomVisibilityAreConfigured()
        {
            SurvivalUnlockDoorInteractable timer = Object.FindFirstObjectByType<SurvivalUnlockDoorInteractable>();
            PlayerMovement l03Player = Object.FindFirstObjectByType<PlayerMovement>();
            Assert.That(timer, Is.Not.Null);
            Assert.That(l03Player, Is.Not.Null);
            Assert.That(timer.RemainingSeconds, Is.EqualTo(90f).Within(0.01f));
            Assert.That(timer.BeginSurvival(l03Player.gameObject), Is.True);

            Time.timeScale = 100f;
            float completionDeadline = Time.realtimeSinceStartup + 3f;
            while (!timer.IsUnlockCompleted && Time.realtimeSinceStartup < completionDeadline)
            {
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.That(timer.IsUnlockCompleted, Is.True, "L03 90-second survival countdown did not complete while scaled time advanced.");
            Assert.That(timer.RemainingSeconds, Is.EqualTo(0f).Within(0.01f));

            // Existing L04 test door is intentionally incomplete and logs during Awake; this test only covers visibility.
            LogAssert.Expect(LogType.Error, new Regex("AutomaticSlidingDoor on 'Door_AutomaticSliding_Test' requires both left and right door leaves"));
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                SceneNames.Level04FacilityCore,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(SceneNames.Level04FacilityCore);
            yield return null;

            RoomVisibilitySceneData data = Object.FindFirstObjectByType<RoomVisibilitySceneData>();
            RoomVisibilityManager manager = Object.FindFirstObjectByType<RoomVisibilityManager>();
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            CharacterController controller = player.GetComponent<CharacterController>();
            StationaryHalfBuriedBossController boss = Object.FindFirstObjectByType<StationaryHalfBuriedBossController>();

            Assert.That(data, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.gameObject.activeInHierarchy, Is.True);
            Assert.That(boss.GetComponentsInChildren<Collider>(true), Is.Not.Empty);
            Assert.That(boss.GetComponentInChildren<Animator>(true), Is.Not.Null);
            Assert.That(data.Rooms.Count, Is.EqualTo(2));
            Assert.That(data.Rooms.SelectMany(room => room.Volumes), Has.All.Matches<RoomVolume>(volume => volume.BoxCollider.isTrigger));

            RoomVisibilityRoomEntry entryRoom = data.Rooms.Single(room => room.RoomId == "ROOM_1");
            RoomVisibilityRoomEntry bossRoom = data.Rooms.Single(room => room.RoomId == "ROOM_2");
            Assert.That(bossRoom.GameplayVisualRenderers, Is.Not.Empty);

            PlacePlayerInRoom(player, controller, entryRoom);
            yield return null;
            Assert.That(manager.ActiveRooms, Does.Contain(entryRoom));
            Assert.That(bossRoom.GameplayVisualRenderers.All(renderer => renderer != null && renderer.forceRenderingOff), Is.True);
            Assert.That(boss.gameObject.activeInHierarchy, Is.True);

            PlacePlayerInRoom(player, controller, bossRoom);
            yield return null;
            Assert.That(manager.ActiveRooms, Does.Contain(bossRoom));
            Assert.That(bossRoom.GameplayVisualRenderers.Any(renderer => renderer != null && !renderer.forceRenderingOff), Is.True);
            Assert.That(boss.gameObject.activeInHierarchy, Is.True);
        }

        private static void PlacePlayerInRoom(PlayerMovement player, CharacterController controller, RoomVisibilityRoomEntry room)
        {
            RoomVolume volume = room.Volumes.First(candidate => candidate != null);
            float detectionOffset = controller.bounds.center.y - player.transform.position.y;
            Vector3 target = volume.BoxCollider.bounds.center;
            target.y -= detectionOffset;
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            player.transform.position = target;
            Physics.SyncTransforms();
            controller.enabled = wasEnabled;
            player.ResetVerticalVelocity();
        }
    }
}
