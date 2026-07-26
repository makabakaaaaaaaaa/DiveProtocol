using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Temporarily scales a NavMeshAgent speed and restores it reliably.
    /// </summary>
    public sealed class TemporaryEnemySlow : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private float _baseSpeed;
        private float _restoreAtTime;
        private bool _active;

        public static void Apply(GameObject target, float speedMultiplier, float durationSeconds)
        {
            if (target == null)
            {
                return;
            }

            TemporaryEnemySlow slow = target.GetComponent<TemporaryEnemySlow>();
            if (slow == null)
            {
                slow = target.AddComponent<TemporaryEnemySlow>();
            }

            slow.ApplyInternal(speedMultiplier, durationSeconds);
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnDisable()
        {
            RestoreSpeed();
        }

        private void Update()
        {
            if (_active && Time.time >= _restoreAtTime)
            {
                RestoreSpeed();
            }
        }

        private void ApplyInternal(float speedMultiplier, float durationSeconds)
        {
            if (_agent == null)
            {
                _agent = GetComponent<NavMeshAgent>();
            }

            if (_agent == null)
            {
                return;
            }

            if (!_active)
            {
                _baseSpeed = _agent.speed;
            }

            _active = true;
            _restoreAtTime = Time.time + Mathf.Max(0.01f, durationSeconds);
            _agent.speed = _baseSpeed * Mathf.Clamp(speedMultiplier, 0.01f, 1f);
        }

        private void RestoreSpeed()
        {
            if (!_active)
            {
                return;
            }

            if (_agent != null)
            {
                _agent.speed = _baseSpeed;
            }

            _active = false;
        }
    }
}
