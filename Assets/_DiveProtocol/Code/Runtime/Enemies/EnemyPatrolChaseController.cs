using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol
{
    /// <summary>
    /// Controls a pre-placed map enemy that patrols, detects a supplied player target, chases, and delegates attacks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPatrolChaseController : MonoBehaviour, IEnemyAwarenessAudioState
    {
        [Header("References")]
        [Tooltip("Agent used for patrol and chase movement.")]
        [SerializeField]
        private NavMeshAgent agent;

        [Tooltip("Contact attack component that receives the player target only while chasing.")]
        [SerializeField]
        private EnemyContactAttack contactAttack;

        [Header("Patrol")]
        [Tooltip("Ordered patrol points. Null entries are skipped.")]
        [SerializeField]
        private Transform[] patrolPoints;

        [SerializeField]
        private bool loopPatrol = true;

        [SerializeField, Min(0f)]
        private float patrolWaitSeconds = 1f;

        [Header("Detection")]
        [SerializeField, Min(0.1f)]
        private float detectionRadius = 6f;

        [SerializeField, Min(0.1f)]
        private float loseTargetRadius = 9f;

        [Header("Movement")]
        [SerializeField, Min(0.02f)]
        private float repathIntervalSeconds = 0.2f;

        [SerializeField]
        private bool startPatrolling = true;

        private int _patrolIndex;
        private float _waitUntilTime;
        private float _nextChaseRepathTime;
        private bool _brainEnabled = true;
        private bool _isWaitingAtPatrolPoint;
        private bool _hasPatrolDestination;
        private bool _hasLoggedMissingAgent;
        private bool _hasLoggedAgentDisabled;
        private bool _hasLoggedNotOnNavMesh;
        private bool _hasLoggedStoppingDistanceWarning;
        private HealthComponent _health;

        public Transform PlayerTarget { get; private set; }
        public bool IsPatrolling { get; private set; }
        public bool IsChasing { get; private set; }
        public bool IsPlayerDetectedForAudio => IsChasing && PlayerTarget != null && !IsDeadForAudio;
        public bool IsDeadForAudio => _health != null && !_health.IsAlive;

        private void Awake()
        {
            ResolveReferences();
            ClampInspectorValues();
            WarnAboutStoppingDistanceOnce();
        }

        private void OnEnable()
        {
            if (_brainEnabled && startPatrolling)
            {
                BeginPatrol();
            }
        }

        private void Update()
        {
            if (!_brainEnabled)
            {
                return;
            }

            if (!IsAgentUsable())
            {
                return;
            }

            UpdateDetection();

            if (IsChasing)
            {
                UpdateChase();
                return;
            }

            UpdatePatrol();
        }

        private void OnDisable()
        {
            StopMovement();
            ClearAttackTarget();
            IsPatrolling = false;
            IsChasing = false;
            _isWaitingAtPatrolPoint = false;
            _hasPatrolDestination = false;
        }

        /// <summary>
        /// Supplies the runtime player target used for detection and chase.
        /// </summary>
        public void SetPlayerTarget(Transform target)
        {
            PlayerTarget = target;
        }

        /// <summary>
        /// Clears the runtime player target and returns to patrol if enabled.
        /// </summary>
        public void ClearPlayerTarget()
        {
            PlayerTarget = null;
            StopChase();

            if (_brainEnabled && startPatrolling)
            {
                BeginPatrol();
            }
        }

        /// <summary>
        /// Enables or disables this enemy brain without destroying the enemy.
        /// </summary>
        public void SetBrainEnabled(bool enabled)
        {
            _brainEnabled = enabled;

            if (enabled)
            {
                if (startPatrolling && !IsChasing)
                {
                    BeginPatrol();
                }

                return;
            }

            StopMovement();
            ClearAttackTarget();
            IsPatrolling = false;
            IsChasing = false;
            _isWaitingAtPatrolPoint = false;
            _hasPatrolDestination = false;
        }

        private void UpdateDetection()
        {
            if (PlayerTarget == null)
            {
                if (IsChasing)
                {
                    StopChase();
                }

                return;
            }

            float distanceSquared = (PlayerTarget.position - transform.position).sqrMagnitude;

            if (!IsChasing)
            {
                if (distanceSquared <= detectionRadius * detectionRadius)
                {
                    StartChase();
                }

                return;
            }

            if (distanceSquared > loseTargetRadius * loseTargetRadius)
            {
                StopChase();
                BeginPatrol();
            }
        }

        private void UpdateChase()
        {
            if (PlayerTarget == null || Time.time < _nextChaseRepathTime)
            {
                return;
            }

            _nextChaseRepathTime = Time.time + repathIntervalSeconds;
            agent.isStopped = false;
            agent.SetDestination(PlayerTarget.position);
        }

        private void UpdatePatrol()
        {
            if (!startPatrolling || !HasAnyPatrolPoint())
            {
                IsPatrolling = false;
                StopMovement();
                return;
            }

            IsPatrolling = true;
            ClearAttackTarget();

            if (_isWaitingAtPatrolPoint)
            {
                if (Time.time < _waitUntilTime)
                {
                    return;
                }

                _isWaitingAtPatrolPoint = false;
                AdvancePatrolIndex();
                _hasPatrolDestination = false;
            }

            Transform point = GetCurrentValidPatrolPoint();
            if (point == null)
            {
                IsPatrolling = false;
                StopMovement();
                return;
            }

            if (!_hasPatrolDestination)
            {
                agent.isStopped = false;
                agent.SetDestination(point.position);
                _hasPatrolDestination = true;
            }

            if (!HasArrived())
            {
                return;
            }

            if (!loopPatrol && IsLastValidPatrolPoint(_patrolIndex))
            {
                IsPatrolling = false;
                StopMovement();
                return;
            }

            _isWaitingAtPatrolPoint = true;
            _waitUntilTime = Time.time + patrolWaitSeconds;
            StopMovement();
        }

        private void StartChase()
        {
            IsChasing = true;
            IsPatrolling = false;
            _isWaitingAtPatrolPoint = false;
            _hasPatrolDestination = false;
            _nextChaseRepathTime = 0f;

            if (contactAttack != null)
            {
                contactAttack.SetTarget(PlayerTarget);
            }
        }

        private void StopChase()
        {
            if (!IsChasing)
            {
                return;
            }

            IsChasing = false;
            ClearAttackTarget();
            StopMovement();
        }

        private void BeginPatrol()
        {
            if (!HasAnyPatrolPoint())
            {
                IsPatrolling = false;
                return;
            }

            IsPatrolling = true;
            _isWaitingAtPatrolPoint = false;
            _hasPatrolDestination = false;
            EnsurePatrolIndexIsValid();
        }

        private void AdvancePatrolIndex()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }

            int startIndex = _patrolIndex;
            do
            {
                _patrolIndex++;

                if (_patrolIndex >= patrolPoints.Length)
                {
                    _patrolIndex = loopPatrol ? 0 : patrolPoints.Length - 1;
                    if (!loopPatrol)
                    {
                        return;
                    }
                }
            }
            while (patrolPoints[_patrolIndex] == null && _patrolIndex != startIndex);
        }

        private Transform GetCurrentValidPatrolPoint()
        {
            EnsurePatrolIndexIsValid();

            if (patrolPoints == null ||
                patrolPoints.Length == 0 ||
                _patrolIndex < 0 ||
                _patrolIndex >= patrolPoints.Length)
            {
                return null;
            }

            return patrolPoints[_patrolIndex];
        }

        private void EnsurePatrolIndexIsValid()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                _patrolIndex = 0;
                return;
            }

            _patrolIndex = Mathf.Clamp(_patrolIndex, 0, patrolPoints.Length - 1);
            if (patrolPoints[_patrolIndex] != null)
            {
                return;
            }

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    _patrolIndex = i;
                    return;
                }
            }
        }

        private bool HasAnyPatrolPoint()
        {
            if (patrolPoints == null)
            {
                return false;
            }

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsLastValidPatrolPoint(int index)
        {
            if (patrolPoints == null)
            {
                return true;
            }

            for (int i = index + 1; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasArrived()
        {
            if (agent.pathPending)
            {
                return false;
            }

            float arrivalDistance = Mathf.Max(agent.stoppingDistance, 0.05f);
            return agent.remainingDistance <= arrivalDistance;
        }

        private void StopMovement()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        private void ClearAttackTarget()
        {
            if (contactAttack != null)
            {
                contactAttack.ClearTarget();
            }
        }

        private bool IsAgentUsable()
        {
            if (agent == null)
            {
                LogMissingAgentOnce();
                return false;
            }

            if (!agent.enabled)
            {
                LogAgentDisabledOnce();
                return false;
            }

            if (!agent.isOnNavMesh)
            {
                LogNotOnNavMeshOnce();
                return false;
            }

            return true;
        }

        private void ResolveReferences()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (contactAttack == null)
            {
                contactAttack = GetComponent<EnemyContactAttack>();
            }

            if (_health == null)
            {
                _health = GetComponent<HealthComponent>();
            }
        }

        private void ClampInspectorValues()
        {
            patrolWaitSeconds = Mathf.Max(0f, patrolWaitSeconds);
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
            loseTargetRadius = Mathf.Max(detectionRadius, loseTargetRadius);
            repathIntervalSeconds = Mathf.Max(0.02f, repathIntervalSeconds);
        }

        private void WarnAboutStoppingDistanceOnce()
        {
            if (_hasLoggedStoppingDistanceWarning || agent == null || contactAttack == null)
            {
                return;
            }

            _hasLoggedStoppingDistanceWarning = true;
            Debug.Log(
                "[Enemy] Verify this enemy's NavMeshAgent Stopping Distance is lower than EnemyContactAttack Attack Range.",
                this);
        }

        private void LogMissingAgentOnce()
        {
            if (_hasLoggedMissingAgent)
            {
                return;
            }

            _hasLoggedMissingAgent = true;
            Debug.LogWarning($"[Enemy] {nameof(EnemyPatrolChaseController)} on '{name}' requires a NavMeshAgent.", this);
        }

        private void LogAgentDisabledOnce()
        {
            if (_hasLoggedAgentDisabled)
            {
                return;
            }

            _hasLoggedAgentDisabled = true;
            Debug.LogWarning($"[Enemy] NavMeshAgent on '{name}' is disabled.", this);
        }

        private void LogNotOnNavMeshOnce()
        {
            if (_hasLoggedNotOnNavMesh)
            {
                return;
            }

            _hasLoggedNotOnNavMesh = true;
            Debug.LogWarning(
                $"[Enemy] NavMeshAgent on '{name}' is not on a NavMesh. Bake the scene NavMesh and place the enemy on it.",
                this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampInspectorValues();
        }
#endif
    }
}
