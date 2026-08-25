using System.Collections;
using System.Linq;
using System.Reflection;
using DiveProtocol.RoomVisibility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiveProtocol.Tests.PlayMode
{
    public sealed class RoomVisibilityTransitionDiagnosticsPlayModeTests
    {
        private string _saveDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!SceneTestUtility.SystemScenesAreAvailable(out var reason)) Assert.Ignore(reason);
            _saveDirectory = SceneTestUtility.CreateTemporarySaveDirectory();
            yield return SceneTestUtility.LoadBootstrap(_saveDirectory);
        }

        [UnityTearDown]
        public IEnumerator TearDown() => SceneTestUtility.Cleanup(_saveDirectory);

        [UnityTest]
        public IEnumerator L02ToL01PlayerRemainsGroundedAfterSpawn()
        {
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);

            SceneTransitionProfile profile = CreateL02ToL01Profile();
            Assert.That(SceneTransitionService.BeginTransition(profile), Is.True);
            yield return SceneTestUtility.WaitForScene(SceneNames.Level01Drainage);
            yield return null;
            Object.Destroy(profile);

            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            RoomVisibilityManager manager = Object.FindFirstObjectByType<RoomVisibilityManager>();
            RoomVisibilitySceneData data = Object.FindFirstObjectByType<RoomVisibilitySceneData>();
            Assert.That(player, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Rooms.Count, Is.EqualTo(8));

            CharacterController controller = player.GetComponent<CharacterController>();
            Vector3 detectionPoint = controller != null ? controller.bounds.center : player.transform.position;
            Assert.That(manager.ActiveRooms.Select(room => room.RoomId), Does.Contain("ROOM_1"));
            Assert.That(data.Rooms.Single(room => room.RoomId == "ROOM_1").ContainsWorldPoint(detectionPoint), Is.True);
            Assert.That(HasSupportingCollider(detectionPoint), Is.True);
            Assert.That(
                data.Rooms.SelectMany(room => room.Renderers).Where(renderer => renderer != null),
                Has.None.Matches<Renderer>(RoomVisibilityUtility.IsGameplayObject));

            yield return AssertPlayerRemainsGrounded(player, 2f);
        }

        [UnityTest]
        public IEnumerator DirectL01LoadPlayerRemainsGroundedAfterSpawn()
        {
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);

            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                SceneNames.Level01Drainage,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return SceneTestUtility.WaitForScene(SceneNames.Level01Drainage);
            yield return AssertPlayerRemainsGrounded(Object.FindFirstObjectByType<PlayerMovement>(), 0.5f);
        }

        [UnityTest]
        public IEnumerator L02PlayerSpawnRemainsGrounded()
        {
            Object.FindFirstObjectByType<MainMenuController>().NewRun();
            yield return SceneTestUtility.WaitForScene(SceneNames.Level02Containment);
            yield return AssertPlayerRemainsGrounded(Object.FindFirstObjectByType<PlayerMovement>(), 0.5f);
        }

        private static IEnumerator AssertPlayerRemainsGrounded(PlayerMovement player, float duration)
        {
            Assert.That(player, Is.Not.Null);
            float minimumY = player.transform.position.y;
            float endTime = Time.realtimeSinceStartup + duration;
            while (Time.realtimeSinceStartup < endTime)
            {
                minimumY = Mathf.Min(minimumY, player.transform.position.y);
                yield return null;
            }

            Assert.That(minimumY, Is.GreaterThan(-0.01f));
            AssertPlayerIsGroundedNearSpawn(player, minimumY);
        }

        private static void AssertPlayerIsGroundedNearSpawn(PlayerMovement player, float minimumY)
        {
            Assert.That(player, Is.Not.Null);
            CharacterController controller = player.GetComponent<CharacterController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(player.transform.position.y, Is.GreaterThan(-0.1f));
            Assert.That(controller.isGrounded, Is.True);
            Assert.That(controller.collisionFlags & CollisionFlags.Below, Is.Not.EqualTo(CollisionFlags.None));
            Debug.Log($"[PlayerGroundingTest] Scene={player.gameObject.scene.name} Position={player.transform.position} MinimumY={minimumY:F3} Grounded={controller.isGrounded} CollisionFlags={controller.collisionFlags}");
        }

        private static bool HasSupportingCollider(Vector3 point)
        {
            return Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Any(collider =>
                    !collider.isTrigger &&
                    point.x >= collider.bounds.min.x && point.x <= collider.bounds.max.x &&
                    point.z >= collider.bounds.min.z && point.z <= collider.bounds.max.z &&
                    collider.bounds.max.y <= point.y + 0.1f && collider.bounds.max.y >= point.y - 5f);
        }

        private static SceneTransitionProfile CreateL02ToL01Profile()
        {
            SceneTransitionProfile profile = ScriptableObject.CreateInstance<SceneTransitionProfile>();
            SetPrivateField(profile, "loadingSceneName", "SCN_Loading");
            SetPrivateField(profile, "targetSceneName", SceneNames.Level01Drainage);
            SetPrivateField(profile, "minimumDisplaySeconds", 0f);
            return profile;
        }

        private static void SetPrivateField<T>(SceneTransitionProfile profile, string fieldName, T value)
        {
            FieldInfo field = typeof(SceneTransitionProfile).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected SceneTransitionProfile field '{fieldName}'.");
            field.SetValue(profile, value);
        }
    }
}
