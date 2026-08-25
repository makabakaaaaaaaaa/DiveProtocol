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
        private bool _firstHitCriticalAvailable;

        public GameObject MarkedBy => _markedBy;
        public bool IsMarked => Time.time < _expiresAtTime;

        public void Mark(GameObject source, float durationSeconds)
        {
            _markedBy = source;
            _expiresAtTime = Time.time + Mathf.Max(0.01f, durationSeconds);
            _firstHitCriticalAvailable = true;
        }

        public void Clear()
        {
            _expiresAtTime = 0f;
            _markedBy = null;
            _firstHitCriticalAvailable = false;
        }

        public bool IsMarkedBy(GameObject source)
        {
            return IsMarked && _markedBy == source;
        }

        /// <summary>Consumes the one opening critical reserved for this source's current mark.</summary>
        public bool TryConsumeFirstHitCritical(GameObject source)
        {
            if (!IsMarkedBy(source) || !_firstHitCriticalAvailable)
            {
                return false;
            }

            _firstHitCriticalAvailable = false;
            return true;
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
