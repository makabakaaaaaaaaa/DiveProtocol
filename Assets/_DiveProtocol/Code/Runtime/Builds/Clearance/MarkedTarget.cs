using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Runtime marker applied by Optic Nerve calibration.
    /// </summary>
    public sealed class MarkedTarget : MonoBehaviour
    {
        private GameObject _markedBy;
        private float _expiresAtTime;

        public GameObject MarkedBy => _markedBy;
        public bool IsMarked => Time.time < _expiresAtTime;

        public void Mark(GameObject source, float durationSeconds)
        {
            _markedBy = source;
            _expiresAtTime = Time.time + Mathf.Max(0.01f, durationSeconds);
        }

        public void Clear()
        {
            _expiresAtTime = 0f;
            _markedBy = null;
        }

        public bool IsMarkedBy(GameObject source)
        {
            return IsMarked && _markedBy == source;
        }

        private void Update()
        {
            if (_markedBy != null && !IsMarked)
            {
                Clear();
            }
        }
    }
}
