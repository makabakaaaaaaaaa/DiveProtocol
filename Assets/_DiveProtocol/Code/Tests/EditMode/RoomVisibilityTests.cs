using DiveProtocol.RoomVisibility;
using NUnit.Framework;
using UnityEngine;

namespace DiveProtocol.Tests.EditMode
{
    public sealed class RoomVisibilityTests
    {
        [Test]
        public void RotatedRoomVolumeContainsPointsInItsOwnLocalSpace()
        {
            var roomObject = new GameObject("ROOM_1_VOLUME");
            try
            {
                roomObject.transform.SetPositionAndRotation(new Vector3(3f, 2f, -1f), Quaternion.Euler(0f, 35f, 0f));
                BoxCollider box = roomObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0.5f, 0f, 0f);
                box.size = new Vector3(4f, 3f, 2f);
                RoomVolume volume = roomObject.AddComponent<RoomVolume>();
                volume.EnsureRoomIdFromName();

                Assert.That(volume.RoomId, Is.EqualTo("ROOM_1"));
                Assert.That(volume.ContainsWorldPoint(roomObject.transform.TransformPoint(box.center)), Is.True);
                Assert.That(volume.ContainsWorldPoint(roomObject.transform.TransformPoint(new Vector3(8f, 0f, 0f))), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(roomObject);
            }
        }

        [Test]
        public void ActiveRoomSetKeepsBothOverlapRoomsAndRetainsTheLastValidSetForAnEmptyGap()
        {
            var roomOneObject = new GameObject("ROOM_1_VOLUME");
            var roomTwoObject = new GameObject("ROOM_2_VOLUME");
            try
            {
                roomOneObject.AddComponent<BoxCollider>();
                roomTwoObject.AddComponent<BoxCollider>();
                RoomVolume roomOne = roomOneObject.AddComponent<RoomVolume>();
                RoomVolume roomTwo = roomTwoObject.AddComponent<RoomVolume>();
                roomOne.EnsureRoomIdFromName();
                roomTwo.EnsureRoomIdFromName();
                var entryOne = new RoomVisibilityRoomEntry(new[] { roomOne });
                var entryTwo = new RoomVisibilityRoomEntry(new[] { roomTwo });
                var state = new RoomVisibilityActiveRoomSet();

                Assert.That(state.Update(new[] { entryOne }), Is.True);
                Assert.That(state.Current, Is.EquivalentTo(new[] { entryOne }));
                Assert.That(state.Update(new[] { entryOne, entryTwo }), Is.True);
                Assert.That(state.Current, Is.EquivalentTo(new[] { entryOne, entryTwo }));
                Assert.That(state.Update(System.Array.Empty<RoomVisibilityRoomEntry>()), Is.False);
                Assert.That(state.Current, Is.EquivalentTo(new[] { entryOne, entryTwo }));
                Assert.That(state.Update(new[] { entryTwo }), Is.True);
                Assert.That(state.Current, Is.EquivalentTo(new[] { entryTwo }));
            }
            finally
            {
                Object.DestroyImmediate(roomOneObject);
                Object.DestroyImmediate(roomTwoObject);
            }
        }

        [Test]
        public void RoomVolumeNormalizesWhitespaceBeforeCloneSuffix()
        {
            var roomObject = new GameObject("ROOM_2_VOLUME  (1)");
            try
            {
                roomObject.AddComponent<BoxCollider>();
                RoomVolume volume = roomObject.AddComponent<RoomVolume>();

                volume.EnsureRoomIdFromName();

                Assert.That(volume.RoomId, Is.EqualTo("ROOM_2"));
            }
            finally
            {
                Object.DestroyImmediate(roomObject);
            }
        }

    }
}
