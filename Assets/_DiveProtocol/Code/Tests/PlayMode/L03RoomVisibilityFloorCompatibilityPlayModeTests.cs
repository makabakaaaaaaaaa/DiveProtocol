using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiveProtocol.RoomVisibility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class L03RoomVisibilityFloorCompatibilityPlayModeTests
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
            yield return new WaitForSecondsRealtime(0.25f);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => SceneTestUtility.Cleanup(_saveDirectory);

        [UnityTest]
        public IEnumerator RoomAndFloorVisibilityComposeForRenderersAndFloor02Lights()
        {
            RoomVisibilitySceneData data = Object.FindFirstObjectByType<RoomVisibilitySceneData>();
            RoomVisibilityManager roomManager = Object.FindFirstObjectByType<RoomVisibilityManager>();
            MultiFloorVisibilityController floorController = Object.FindFirstObjectByType<MultiFloorVisibilityController>();
            FloorRoomVisibilityLightDecalGate gate = Object.FindFirstObjectByType<FloorRoomVisibilityLightDecalGate>();
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            CharacterController characterController = player.GetComponent<CharacterController>();

            Assert.That(data, Is.Not.Null);
            Assert.That(roomManager, Is.Not.Null);
            Assert.That(floorController, Is.Not.Null);
            Assert.That(gate, Is.Not.Null);
            Assert.That(characterController, Is.Not.Null);
            Assert.That(data.Rooms.Count, Is.EqualTo(6));
            Assert.That(data.Rooms.SelectMany(room => room.Volumes), Has.All.Matches<RoomVolume>(volume => volume.BoxCollider.isTrigger));

            RoomVisibilityRoomEntry[] lowerRooms = data.Rooms.Where(IsLowerRoom).OrderBy(room => room.RoomId).ToArray();
            RoomVisibilityRoomEntry[] upperRooms = data.Rooms.Where(IsUpperRoom).OrderBy(room => room.RoomId).ToArray();
            Assert.That(lowerRooms, Has.Length.EqualTo(3));
            Assert.That(upperRooms, Has.Length.EqualTo(3));
            Assert.That(gate.ManagedFloor02Lights, Is.Not.Empty);

            floorController.ApplyState(FloorVisibilityState.Floor01Only, force: true);
            PlacePlayerInRoom(player, characterController, lowerRooms[0]);
            yield return null;

            AssertRoomVisible(roomManager, lowerRooms[0]);
            AssertRoomHidden(lowerRooms[1]);
            Assert.That(floorController.Floor02.Renderers, Has.All.Matches<Renderer>(renderer => renderer != null && !renderer.enabled));
            Assert.That(gate.ManagedFloor02Lights, Has.All.Matches<Light>(light => light != null && !light.enabled));

            PlacePlayerInRoom(player, characterController, lowerRooms[1]);
            yield return null;
            AssertRoomVisible(roomManager, lowerRooms[1]);
            AssertRoomHidden(lowerRooms[0]);

            RoomVisibilityRoomEntry upperRoom = upperRooms.First(room => room.Lights.Any(light => gate.ManagedFloor02Lights.Contains(light)));
            floorController.ApplyState(FloorVisibilityState.TransitionBoth, force: true);
            PlacePlayerInRoom(player, characterController, upperRoom);
            yield return null;

            AssertRoomVisible(roomManager, upperRoom);
            Renderer floor02Renderer = floorController.Floor02.Renderers.First(renderer => upperRoom.Renderers.Contains(renderer));
            Assert.That(floor02Renderer.enabled, Is.True);
            Assert.That(floor02Renderer.forceRenderingOff, Is.False);
            Light upperLight = upperRoom.Lights.First(light => gate.ManagedFloor02Lights.Contains(light));
            Assert.That(upperLight.enabled, Is.True);

            floorController.ApplyState(FloorVisibilityState.Floor02Only, force: true);
            yield return null;
            Assert.That(floor02Renderer.enabled, Is.True);
            Assert.That(floor02Renderer.forceRenderingOff, Is.False);
            Assert.That(upperLight.enabled, Is.True);

            floorController.ApplyState(FloorVisibilityState.Floor01Only, force: true);
            PlacePlayerInRoom(player, characterController, lowerRooms[0]);
            yield return null;
            Assert.That(floor02Renderer.enabled, Is.False);
            Assert.That(gate.ManagedFloor02Lights, Has.All.Matches<Light>(light => light != null && !light.enabled));
        }

        private static bool IsLowerRoom(RoomVisibilityRoomEntry room)
        {
            return room.Volumes.Any(volume => volume != null && volume.BoxCollider.bounds.max.y < 3.5f);
        }

        private static bool IsUpperRoom(RoomVisibilityRoomEntry room)
        {
            return room.Volumes.Any(volume => volume != null && volume.BoxCollider.bounds.min.y > 3.5f);
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

        private static void AssertRoomVisible(RoomVisibilityManager manager, RoomVisibilityRoomEntry room)
        {
            Assert.That(manager.ActiveRooms, Does.Contain(room));
            Renderer renderer = room.Renderers.First(candidate => candidate != null && candidate.enabled);
            Assert.That(renderer.forceRenderingOff, Is.False, renderer.name);
        }

        private static void AssertRoomHidden(RoomVisibilityRoomEntry room)
        {
            Renderer renderer = room.Renderers.First(candidate => candidate != null);
            Assert.That(renderer.forceRenderingOff, Is.True, renderer.name);
        }
    }
}
