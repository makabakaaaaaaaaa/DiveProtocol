using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Tracks consecutive hits against the same target for Optic Nerve joint rupture.
    /// </summary>
    public sealed class JointRuptureTracker
    {
        private Component _lastTarget;
        private int _consecutiveHits;

        public bool RegisterHit(IDamageable target)
        {
            Component targetComponent = target as Component;
            if (targetComponent == null)
            {
                _lastTarget = null;
                _consecutiveHits = 0;
                return false;
            }

            if (_lastTarget == targetComponent)
            {
                _consecutiveHits++;
            }
            else
            {
                _lastTarget = targetComponent;
                _consecutiveHits = 1;
            }

            if (_consecutiveHits < 2)
            {
                return false;
            }

            _consecutiveHits = 0;
            return true;
        }

        public void Reset()
        {
            _lastTarget = null;
            _consecutiveHits = 0;
        }
    }
}
