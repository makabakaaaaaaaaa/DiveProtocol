using System.Collections;
using DiveProtocol.Builds;
using DiveProtocol.Interaction;
using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol.Enemies.CorpseReanimation
{
    /// <summary>
    /// Corpse enemy that remains inert before the first final-boss clear, then may reanimate in later runs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReanimatingCorpseEnemy : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float detectionRadius = 2.5f;
        [SerializeField, Range(0f, 1f)] private float baseReanimationChance = 0.15f;
        [SerializeField, Range(0f, 1f)] private float activityChanceMultiplier = 0.45f;
        [SerializeField] private bool rollOnlyOnce = true;
        [SerializeField] private bool reanimateOnPlayerTouch;
        [SerializeField] private LayerMask playerLayer = ~0;

        [Header("Visuals")]
        [SerializeField] private GameObject dormantVisualRoot;
        [SerializeField] private GameObject activeEnemyRoot;

        [Header("Dormant Disable")]
        [SerializeField] private Behaviour[] disabledWhileDormant;
        [SerializeField] private Collider[] collidersDisabledWhileDormant;
        [SerializeField] private bool disableNavMeshAgentWhileDormant = true;
        [SerializeField] private bool startDormant = true;

        [Header("Reanimation")]
        [SerializeField, Min(0f)] private float reanimationDelay = 0.6f;

        private readonly Collider[] _playerHits = new Collider[8];
        private NavMeshAgent _navMeshAgent;
        private bool _hasRolled;
        private bool _warnedNoAi;
        private Coroutine _reanimationRoutine;

        public CorpseReanimationState State { get; private set; } = CorpseReanimationState.Dormant;
        public float LastRollChance { get; private set; }
        public bool LastRollSucceeded { get; private set; }

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            AutoFillDormantBehavioursIfEmpty();

            if (startDormant)
            {
                EnterDormant();
            }
            else
            {
                EnterActive();
            }
        }

        private void Update()
        {
            if (State != CorpseReanimationState.Dormant)
            {
                return;
            }

            if (!CorpseReanimationMetaProgress.HasClearedFinalBossOnce)
            {
                return;
            }

            if (IsPlayerWithinDetectionRadius())
            {
                TryRollForReanimation();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!reanimateOnPlayerTouch || State != CorpseReanimationState.Dormant)
            {
                return;
            }

            if (!IsPlayerCollider(other))
            {
                return;
            }

            TryRollForReanimation();
        }

        /// <summary>
        /// Forces a dormant corpse to roll immediately, respecting meta progression and roll-only-once rules.
        /// </summary>
        public bool TryRollForReanimation()
        {
            if (State != CorpseReanimationState.Dormant)
            {
                return false;
            }

            if (!CorpseReanimationMetaProgress.HasClearedFinalBossOnce)
            {
                return false;
            }

            if (rollOnlyOnce && _hasRolled)
            {
                return false;
            }

            _hasRolled = true;
            CorpseReanimationEvents.RaiseCorpseReanimationRolled(this);

            LastRollChance = CalculateCurrentChance();
            LastRollSucceeded = Random.value <= LastRollChance;
            CorpseReanimationEvents.RaiseCorpseReanimationResult(
                this,
                LastRollChance,
                LastRollSucceeded);

            if (!LastRollSucceeded)
            {
                State = CorpseReanimationState.CheckedAndStill;
                return false;
            }

            BeginReanimation();
            return true;
        }

        /// <summary>
        /// Immediately disables this corpse reanimation controller.
        /// </summary>
        public void SetDisabled()
        {
            State = CorpseReanimationState.Disabled;
            StopReanimationRoutine();
            ApplyDormantState();
        }

        /// <summary>
        /// Resets this corpse to dormant and clears its local roll state.
        /// </summary>
        public void ResetToDormant()
        {
            _hasRolled = false;
            LastRollChance = 0f;
            LastRollSucceeded = false;
            StopReanimationRoutine();
            EnterDormant();
        }

        public float CalculateCurrentChance()
        {
            return Mathf.Clamp01(
                baseReanimationChance +
                CorpseActivityProvider.CurrentActivity * activityChanceMultiplier);
        }

        private void BeginReanimation()
        {
            State = CorpseReanimationState.Reanimating;
            StopReanimationRoutine();

            if (reanimationDelay <= 0f)
            {
                EnterActive();
                return;
            }

            _reanimationRoutine = StartCoroutine(ReanimationRoutine());
        }

        private IEnumerator ReanimationRoutine()
        {
            yield return new WaitForSeconds(reanimationDelay);
            _reanimationRoutine = null;
            EnterActive();
        }

        private void EnterDormant()
        {
            State = CorpseReanimationState.Dormant;
            ApplyDormantState();
        }

        private void EnterActive()
        {
            State = CorpseReanimationState.Active;

            if (dormantVisualRoot != null)
            {
                dormantVisualRoot.SetActive(false);
            }

            if (activeEnemyRoot != null)
            {
                activeEnemyRoot.SetActive(true);
            }

            SetDormantBehavioursEnabled(true);
            SetDormantCollidersEnabled(true);
            SetNavMeshAgentDormant(false);
            CorpseReanimationEvents.RaiseCorpseReanimated(this);
            NotifyBuildSystems();
        }

        private void ApplyDormantState()
        {
            if (dormantVisualRoot != null)
            {
                dormantVisualRoot.SetActive(true);
            }

            if (activeEnemyRoot != null)
            {
                activeEnemyRoot.SetActive(false);
            }

            SetDormantBehavioursEnabled(false);
            SetDormantCollidersEnabled(false);
            SetNavMeshAgentDormant(true);
        }

        private void SetDormantBehavioursEnabled(bool enabled)
        {
            if (disabledWhileDormant == null)
            {
                return;
            }

            for (int i = 0; i < disabledWhileDormant.Length; i++)
            {
                Behaviour behaviour = disabledWhileDormant[i];
                if (behaviour != null && behaviour != this)
                {
                    behaviour.enabled = enabled;
                }
            }
        }

        private void SetDormantCollidersEnabled(bool enabled)
        {
            if (collidersDisabledWhileDormant == null)
            {
                return;
            }

            for (int i = 0; i < collidersDisabledWhileDormant.Length; i++)
            {
                Collider targetCollider = collidersDisabledWhileDormant[i];
                if (targetCollider != null)
                {
                    targetCollider.enabled = enabled;
                }
            }
        }

        private void SetNavMeshAgentDormant(bool dormant)
        {
            if (_navMeshAgent == null || !disableNavMeshAgentWhileDormant)
            {
                return;
            }

            if (dormant)
            {
                if (_navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.isStopped = true;
                    _navMeshAgent.ResetPath();
                }

                _navMeshAgent.enabled = false;
                return;
            }

            _navMeshAgent.enabled = true;
        }

        private bool IsPlayerWithinDetectionRadius()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRadius,
                _playerHits,
                playerLayer,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                if (IsPlayerCollider(_playerHits[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPlayerCollider(Collider candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            return candidate.GetComponentInParent<PlayerInteractor>() != null ||
                   candidate.GetComponentInParent<PlayerMovement>() != null ||
                   candidate.GetComponentInParent<CharacterController>() != null;
        }

        private void NotifyBuildSystems()
        {
            // Future SceneEnemyTargetRelay / player binding can call SymbiosisController directly.
            // This event path keeps corpse reanimation decoupled from player discovery.
        }

        private void AutoFillDormantBehavioursIfEmpty()
        {
            if (disabledWhileDormant != null && disabledWhileDormant.Length > 0)
            {
                return;
            }

            Behaviour[] behaviours = GetComponents<Behaviour>();
            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (ShouldAutoDisable(behaviours[i]))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                LogNoAiWarningOnce();
                return;
            }

            disabledWhileDormant = new Behaviour[count];
            int index = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (ShouldAutoDisable(behaviours[i]))
                {
                    disabledWhileDormant[index++] = behaviours[i];
                }
            }
        }

        private bool ShouldAutoDisable(Behaviour behaviour)
        {
            return behaviour != null &&
                   behaviour != this &&
                   (behaviour is EnemyChaseController ||
                    behaviour is EnemyPatrolChaseController ||
                    behaviour is EnemyContactAttack);
        }

        private void LogNoAiWarningOnce()
        {
            if (_warnedNoAi)
            {
                return;
            }

            _warnedNoAi = true;
            Debug.LogWarning(
                "[Corpse] No known enemy AI behaviours were auto-detected. Assign disabledWhileDormant manually if this corpse has custom AI.",
                this);
        }

        private void StopReanimationRoutine()
        {
            if (_reanimationRoutine == null)
            {
                return;
            }

            StopCoroutine(_reanimationRoutine);
            _reanimationRoutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
            baseReanimationChance = Mathf.Clamp01(baseReanimationChance);
            activityChanceMultiplier = Mathf.Clamp01(activityChanceMultiplier);
            reanimationDelay = Mathf.Max(0f, reanimationDelay);
        }
#endif
    }
}
