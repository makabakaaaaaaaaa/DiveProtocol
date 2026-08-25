using UnityEngine;

namespace DiveProtocol.RoomVisibility
{
    /// <summary>Scene-authored room boundary used by room visibility baking and runtime detection.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RoomVolume : MonoBehaviour
    {
        [SerializeField] private string _roomId;

        public string RoomId => _roomId;
        public BoxCollider BoxCollider => GetComponent<BoxCollider>();

        /// <summary>Checks containment in the BoxCollider's own local space, including rotation and scale.</summary>
        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            BoxCollider box = BoxCollider;
            if (box == null)
            {
                return false;
            }

            Vector3 localPoint = transform.InverseTransformPoint(worldPoint) - box.center;
            Vector3 halfSize = box.size * 0.5f;
            return Mathf.Abs(localPoint.x) <= halfSize.x &&
                   Mathf.Abs(localPoint.y) <= halfSize.y &&
                   Mathf.Abs(localPoint.z) <= halfSize.z;
        }

        /// <summary>Assigns a stable ID without changing the authored volume transform or collider.</summary>
        public void SetRoomId(string roomId)
        {
            _roomId = roomId;
        }

        public void EnsureRoomIdFromName()
        {
            if (!string.IsNullOrWhiteSpace(_roomId))
            {
                return;
            }

            string normalizedName = name.Trim();
            int cloneSuffix = normalizedName.LastIndexOf(" (", System.StringComparison.Ordinal);
            if (cloneSuffix >= 0 && normalizedName.EndsWith(")", System.StringComparison.Ordinal))
            {
                normalizedName = normalizedName.Substring(0, cloneSuffix).Trim();
            }

            const string suffix = "_VOLUME";
            if (normalizedName.StartsWith("ROOM_", System.StringComparison.OrdinalIgnoreCase) &&
                normalizedName.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
            {
                _roomId = normalizedName.Substring(0, normalizedName.Length - suffix.Length).ToUpperInvariant();
            }
        }

        private void OnDrawGizmos()
        {
            BoxCollider box = BoxCollider;
            if (box == null)
            {
                return;
            }

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.TransformPoint(box.center), string.IsNullOrEmpty(_roomId) ? name : _roomId);
#endif
        }
    }
}
