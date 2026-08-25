using System.Collections;
using System.Linq;
using DiveProtocol.Doors;
using DiveProtocol.RoomVisibility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class L01DoorwayCollisionPlayModeTests
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
                SceneNames.Level01Drainage,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(SceneNames.Level01Drainage);
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => SceneTestUtility.Cleanup(_saveDirectory);

        [UnityTest]
        public IEnumerator Room3DoorwayBlocksClosedAndClearsWhenOpen()
        {
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            CharacterController controller = player.GetComponent<CharacterController>();
            DoorController door = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(candidate => candidate.name == "PB_D_R03_R04");
            Collider doorCollider = door.GetComponent<Collider>();
            Assert.That(player, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(doorCollider, Is.Not.Null);

            foreach (RoomVolume volume in Object.FindObjectsByType<RoomVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Assert.That(volume.BoxCollider.isTrigger, Is.True, $"Room volume '{volume.name}' must not block player movement.");
            }

            Vector3 closedDoorCenter = doorCollider.bounds.center;
            Vector3 traversalDirection = door.transform.forward;
            traversalDirection.y = 0f;
            traversalDirection.Normalize();
            Vector3 capsuleRootPosition = new Vector3(closedDoorCenter.x, player.transform.position.y, closedDoorCenter.z) - traversalDirection * 1.25f;

            door.SetOpenImmediate(false);
            Physics.SyncTransforms();
            Collider[] closedHits = GetBlockingHits(player, controller, capsuleRootPosition, traversalDirection, 1.8f);
            Assert.That(closedHits, Has.Some.Matches<Collider>(collider => collider == doorCollider));

            door.SetOpenImmediate(true);
            Physics.SyncTransforms();
            Collider[] openHits = GetBlockingHits(player, controller, capsuleRootPosition, traversalDirection, 1.8f);
            Assert.That(openHits, Is.Empty, $"Open doorway still blocked by: {string.Join(", ", openHits.Select(collider => RoomVisibilityUtility.GetHierarchyPath(collider.transform)))}");

            Renderer[] doorwayWalls = FindRoom3DoorwayWalls();
            RoomVisibilitySceneData data = Object.FindFirstObjectByType<RoomVisibilitySceneData>();
            RoomVisibilityRoomEntry room3 = data.Rooms.Single(room => room.RoomId == "ROOM_3");
            RoomVisibilityRoomEntry room4 = data.Rooms.Single(room => room.RoomId == "ROOM_4");
            Assert.That(doorwayWalls, Has.Length.EqualTo(2));
            Assert.That(room3.Renderers.Any(renderer => renderer == doorwayWalls[0]), Is.True);
            Assert.That(room3.Renderers.Any(renderer => renderer == doorwayWalls[1]), Is.True);
            Assert.That(room4.Renderers.Any(renderer => renderer == doorwayWalls[0]), Is.False);
            Assert.That(room4.Renderers.Any(renderer => renderer == doorwayWalls[1]), Is.False);

            PlacePlayerInRoom3(player, controller);
            yield return null;

            RoomVisibilityManager manager = Object.FindFirstObjectByType<RoomVisibilityManager>();
            Assert.That(manager.ActiveRooms.Select(room => room.RoomId), Does.Contain("ROOM_3"));
            Assert.That(doorwayWalls, Has.None.Matches<Renderer>(renderer => renderer.forceRenderingOff));
        }

        [UnityTest]
        public IEnumerator AllL01DoorControllersMoveTheirBlockingLeafWhenOpened()
        {
            DoorController[] doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(doors, Has.Length.EqualTo(10));

            foreach (DoorController door in doors)
            {
                Collider blockingCollider = door.GetComponent<Collider>();
                Assert.That(blockingCollider, Is.Not.Null, $"Door '{door.name}' has no blocking leaf collider.");
                Assert.That(blockingCollider.isTrigger, Is.False, $"Door '{door.name}' blocking collider must remain physical while closed.");

                door.SetOpenImmediate(false);
                Quaternion closedRotation = door.HingePivot.localRotation;
                door.SetOpenImmediate(true);
                Physics.SyncTransforms();

                Assert.That(door.IsOpen, Is.True, $"Door '{door.name}' did not enter Open state.");
                Assert.That(Quaternion.Angle(closedRotation, door.HingePivot.localRotation), Is.GreaterThan(45f), $"Door '{door.name}' did not rotate its collider hierarchy open.");
            }

            yield return null;
        }

        private static Collider[] GetBlockingHits(
            PlayerMovement player,
            CharacterController controller,
            Vector3 rootPosition,
            Vector3 direction,
            float distance)
        {
            float bottomOffset = controller.bounds.min.y - player.transform.position.y;
            float topOffset = controller.bounds.max.y - player.transform.position.y;
            float radius = Mathf.Min(controller.bounds.extents.x, controller.bounds.extents.z) * 0.9f;
            Vector3 point1 = rootPosition + Vector3.up * (bottomOffset + radius);
            Vector3 point2 = rootPosition + Vector3.up * (topOffset - radius);

            return Physics.CapsuleCastAll(
                    point1,
                    point2,
                    radius,
                    direction,
                    distance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                .Where(hit =>
                    hit.collider != null &&
                    !hit.collider.transform.IsChildOf(player.transform) &&
                    hit.collider.bounds.max.y > rootPosition.y + 0.1f)
                .OrderBy(hit => hit.distance)
                .Select(hit => hit.collider)
                .Distinct()
                .ToArray();
        }

        private static Renderer[] FindRoom3DoorwayWalls()
        {
            return Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(renderer =>
                {
                    string path = RoomVisibilityUtility.GetHierarchyPath(renderer.transform);
                    return path == "_Rooms/ROOM_03/Geometry/PB_R04_4_Wall_N_02 (4)" ||
                           path == "_Rooms/ROOM_03/Geometry/PB_R04_4_Wall_N_02 (6)";
                })
                .ToArray();
        }

        private static void PlacePlayerInRoom3(PlayerMovement player, CharacterController controller)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            player.transform.position = new Vector3(11.55f, 0.03f, 3.78f);
            Physics.SyncTransforms();
            controller.enabled = wasEnabled;
            player.ResetVerticalVelocity();
        }
    }
}
