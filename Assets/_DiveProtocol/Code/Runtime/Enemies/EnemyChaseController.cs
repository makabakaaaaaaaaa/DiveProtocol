using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol
{
    /// <summary>
    /// Minimal whitebox enemy chase controller driven by a supplied target transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyChaseController : MonoBehaviour, IEnemyAwarenessAudioState
    {
        [Header("Agent")]
        [Tooltip("NavMeshAgent used to move this enemy.")]
        [SerializeField]
        private NavMeshAgent agent;

        [SerializeField, Min(0f)]
        private float stoppingDistance = 1.5f;

        [SerializeField, Min(0.02f)]
        private float repathIntervalSeconds = 0.2f;

        [SerializeField]
        private bool stopWhenTargetDead = true;

        private IDamageable _targetDamageable;
        private float _nextRepathTime;
        private bool _movementEnabled = true;
        private bool _hasLoggedMissingAgent;
        private bool _hasLoggedNotOnNavMesh;
        private HealthComponent _health;

        public Transform Target { get; private set; }
        public bool HasTarget => Target != null;
        public bool IsPlayerDetectedForAudio => HasTarget && !IsDeadForAudio;
        public bool IsDeadForAudio => _health != null && !_health.IsAlive;

        private void Awake()
        {
            ResolveAgent();
            _health = GetComponent<HealthComponent>();

            if (agent != null)
            {
                agent.stoppingDistance = stoppingDistance;
            }
        }

        private void Update()
        {
            if (!_movementEnabled || Target == null)
            {
                StopAgent();
                return;
            }

            if (stopWhenTargetDead &&
                _targetDamageable != null &&
                !_targetDamageable.IsAlive)
            {
                StopAgent();
                return;
            }

            if (agent == null)
            {
                LogMissingAgentOnce();
                return;
            }

            if (!agent.isOnNavMesh)
            {
                LogNotOnNavMeshOnce();
                return;
            }

            if (Time.time < _nextRepathTime)
            {
                return;
            }

            _nextRepathTime = Time.time + repathIntervalSeconds;
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
            agent.SetDestination(Target.position);
        }

        /// <summary>
        /// Supplies the target this enemy should chase.
        /// </summary>
        public void SetTarget(Transform target)
        {
            Target = target;
            _targetDamageable = target != null
                ? target.GetComponentInParent<IDamageable>()
                : null;
            _nextRepathTime = 0f;
        }

        /// <summary>
        /// Clears the target and stops the agent.
        /// </summary>
        public void ClearTarget()
        {
            Target = null;
            _targetDamageable = null;
            StopAgent();
        }

        /// <summary>
        /// Enables or disables chase movement without disabling the component.
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;

            if (!enabled)
            {
                StopAgent();
            }
        }

        private void StopAgent()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void ResolveAgent()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }

        private void LogMissingAgentOnce()
        {
            if (_hasLoggedMissingAgent)
            {
                return;
            }

            _hasLoggedMissingAgent = true;
            Debug.LogWarning($"[Enemy] {nameof(EnemyChaseController)} on '{name}' has no NavMeshAgent.", this);
        }

        private void LogNotOnNavMeshOnce()
        {
            if (_hasLoggedNotOnNavMesh)
            {
                return;
            }

            _hasLoggedNotOnNavMesh = true;
            Debug.LogWarning(
                $"[Enemy] NavMeshAgent on '{name}' is not on a NavMesh. Bake or place the enemy on a NavMesh.",
                this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            repathIntervalSeconds = Mathf.Max(0.02f, repathIntervalSeconds);
        }
#endif
    }
}
