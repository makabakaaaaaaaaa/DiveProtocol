using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiveProtocol.RoomVisibility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class L01RoomVisibilityWallMembershipPlayModeTests
    {
        private readonly struct ExpectedMembership
        {
            public ExpectedMembership(string path, string visibleRoom, string excludedRoom)
            {
                Path = path;
                VisibleRoom = visibleRoom;
                ExcludedRoom = excludedRoom;
            }

            public string Path { get; }
            public string VisibleRoom { get; }
            public string ExcludedRoom { get; }
        }

        private static readonly ExpectedMembership[] ExpectedWalls =
        {
            new("_Rooms/ROOM_03/Geometry/PB_R04_4_Wall_N_02 (4)", "ROOM_3", "ROOM_4"),
            new("_Rooms/ROOM_03/Geometry/PB_R04_4_Wall_N_02 (6)", "ROOM_3", "ROOM_4"),
            new("_Rooms/ROOM_03/Geometry/PB_R03_3_Wall_N_01", "ROOM_3", "ROOM_1"),
            new("_Rooms/ROOM_03/Geometry/PB_R01_1_Wall_E_02", "ROOM_3", "ROOM_1"),
            new("_Rooms/ROOM_04/Geometry/PB_R04_4_Wall_E_02", "ROOM_4", "ROOM_1"),
            new("_Rooms/ROOM_04/Geometry/PB_R04_4_Wall_N_01", "ROOM_4", "ROOM_1"),
            new("_Rooms/ROOM_04/Geometry/PB_R04_4_Wall_E_01", "ROOM_4", "ROOM_2"),
            new("_Rooms/ROOM_05/Geometry/PB_R05_5_Wall_W_02", "ROOM_5", "ROOM_6"),
            new("_Rooms/ROOM_05/Geometry/PB_R05_5_Wall_W_01", "ROOM_5", "ROOM_6"),
            new("_Rooms/ROOM_05/Geometry/PB_R05_5_Wall_N_01", "ROOM_5", "ROOM_1"),
            new("_Rooms/ROOM_07/Geometry/PB_R07_7_Wall_E_01", "ROOM_7", "ROOM_3"),
            new("_Rooms/ROOM_02/Geometry/PB_R04_4_Wall_S_01 (1)", "ROOM_2", "ROOM_4")
        };

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
                SceneNames.Level01Drainage,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(SceneNames.Level01Drainage);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => SceneTestUtility.Cleanup(_saveDirectory);

        [UnityTest]
        public IEnumerator CorrectedWallsBelongToAndRenderInTheirActiveRoom()
        {
            RoomVisibilitySceneData data = Object.FindFirstObjectByType<RoomVisibilitySceneData>();
            RoomVisibilityManager manager = Object.FindFirstObjectByType<RoomVisibilityManager>();
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            CharacterController controller = player.GetComponent<CharacterController>();
            var renderersByPath = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .ToDictionary(renderer => RoomVisibilityUtility.GetHierarchyPath(renderer.transform), renderer => renderer);

            Assert.That(data, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);

            foreach (ExpectedMembership expected in ExpectedWalls)
            {
                Renderer renderer = renderersByPath[expected.Path];
                RoomVisibilityRoomEntry visibleRoom = data.Rooms.Single(room => room.RoomId == expected.VisibleRoom);
                RoomVisibilityRoomEntry excludedRoom = data.Rooms.Single(room => room.RoomId == expected.ExcludedRoom);
                Assert.That(visibleRoom.Renderers, Does.Contain(renderer), expected.Path);
                Assert.That(excludedRoom.Renderers, Has.None.EqualTo(renderer), expected.Path);
            }

            foreach (IGrouping<string, ExpectedMembership> group in ExpectedWalls.GroupBy(wall => wall.VisibleRoom))
            {
                RoomVisibilityRoomEntry room = data.Rooms.Single(entry => entry.RoomId == group.Key);
                RoomVolume volume = room.Volumes.First(volumeCandidate => volumeCandidate != null);
                Vector3 target = volume.BoxCollider.bounds.center;
                target.y = player.transform.position.y;
                bool wasEnabled = controller.enabled;
                controller.enabled = false;
                player.transform.position = target;
                Physics.SyncTransforms();
                controller.enabled = wasEnabled;
                player.ResetVerticalVelocity();
                yield return null;

                Assert.That(manager.ActiveRooms.Select(entry => entry.RoomId), Does.Contain(group.Key));
                foreach (ExpectedMembership expected in group)
                {
                    Assert.That(renderersByPath[expected.Path].forceRenderingOff, Is.False, expected.Path);
                }
            }
        }
    }
}
