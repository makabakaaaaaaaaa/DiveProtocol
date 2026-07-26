using System.Collections;
using DiveProtocol;
using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol.Enemies
{
    /// <summary>
    /// Bridges enemy gameplay state to a visual Animator without changing movement, damage, or attack logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimatorBridge : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Animator on the current visual model. If empty, the bridge searches child objects.")]
        [SerializeField] private Animator _animator;

        [Tooltip("NavMeshAgent on the enemy logic root. Used only to read velocity.")]
        [SerializeField] private NavMeshAgent _agent;

        [Tooltip("Health component on the enemy logic root. Used only to mirror hit and death animation state.")]
        [SerializeField] private HealthComponent _health;

        [Header("Animator Parameters")]
        [SerializeField] private string _moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string _attackTriggerParameter = "Attack";
        [SerializeField] private string _hitTriggerParameter = "Hit";
        [SerializeField] private string _deadBoolParameter = "Dead";

        [Header("Tuning")]
        [SerializeField, Min(0f)] private float _moveSpeedDampTime = 0.1f;
        [SerializeField] private bool _disableAnimatorRootMotion = true;
        [SerializeField] private bool _refreshAnimatorOnStart = true;

        private int _moveSpeedHash;
        private int _attackTriggerHash;
        private int _hitTriggerHash;
        private int _deadBoolHash;

        private bool _hasMoveSpeedParameter;
        private bool _hasAttackTriggerParameter;
        private bool _hasHitTriggerParameter;
        private bool _hasDeadBoolParameter;
        private bool _hasLoggedMissingAnimator;
        private bool _hasLoggedMissingAgent;
        private bool _hasLoggedMissingHealth;
        private bool _subscribedToHealth;

        private void Reset()
        {
            AutoBindCoreReferences();
            RefreshAnimatorReference();
        }

        private void Awake()
        {
            AutoBindCoreReferences();

            if (!_refreshAnimatorOnStart)
            {
                RefreshAnimatorReference();
            }
        }

        private void OnEnable()
        {
            SubscribeToHealth();
            RefreshDeadState();
        }

        private IEnumerator Start()
        {
            if (_refreshAnimatorOnStart)
            {
                yield return null;
                RefreshAnimatorReference();
            }

            RefreshDeadState();
        }

        private void OnDisable()
        {
            UnsubscribeFromHealth();
        }

        private void Update()
        {
            UpdateMoveSpeed();
            RefreshDeadState();
        }

        /// <summary>
        /// Re-finds the visual Animator. Call this after a runtime visual model is spawned or replaced.
        /// </summary>
        public void RefreshAnimatorReference()
        {
            EnemyRandomVisualSelector visualSelector = GetComponent<EnemyRandomVisualSelector>();
            if (visualSelector != null && visualSelector.CurrentAnimator != null)
            {
                _animator = visualSelector.CurrentAnimator;
            }
            else if (_animator == null || !_animator.transform.IsChildOf(transform))
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            if (_animator == null)
            {
                LogMissingAnimatorOnce();
                RefreshParameterHashes();
                return;
            }

            if (_disableAnimatorRootMotion)
            {
                _animator.applyRootMotion = false;
            }

            RefreshParameterHashes();
            RefreshDeadState();
        }

        /// <summary>
        /// Triggers the configured attack animation parameter.
        /// </summary>
        public void PlayAttack()
        {
            if (!CanUseAnimatorParameter(_hasAttackTriggerParameter))
            {
                return;
            }

            _animator.ResetTrigger(_attackTriggerHash);
            _animator.SetTrigger(_attackTriggerHash);
        }

        /// <summary>
        /// Triggers the configured hit reaction animation parameter.
        /// </summary>
        public void PlayHit()
        {
            if (!CanUseAnimatorParameter(_hasHitTriggerParameter))
            {
                return;
            }

            if (_hasDeadBoolParameter && _animator.GetBool(_deadBoolHash))
            {
                return;
            }

            _animator.ResetTrigger(_hitTriggerHash);
            _animator.SetTrigger(_hitTriggerHash);
        }

        /// <summary>
        /// Sets the configured death animation bool.
        /// </summary>
        public void SetDead(bool dead)
        {
            if (!CanUseAnimatorParameter(_hasDeadBoolParameter))
            {
                return;
            }

            _animator.SetBool(_deadBoolHash, dead);
        }

        /// <summary>
        /// Mirrors the current HealthComponent alive/dead state into the Animator, if HealthComponent is present.
        /// </summary>
        public void RefreshDeadState()
        {
            if (_health == null)
            {
                LogMissingHealthOnce();
                return;
            }

            SetDead(!_health.IsAlive);
        }

        private void AutoBindCoreReferences()
        {
            if (_agent == null)
            {
                _agent = GetComponent<NavMeshAgent>();
            }

            if (_health == null)
            {
                _health = GetComponent<HealthComponent>();
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void SubscribeToHealth()
        {
            if (_subscribedToHealth)
            {
                return;
            }

            if (_health == null)
            {
                AutoBindCoreReferences();
            }

            if (_health == null)
            {
                LogMissingHealthOnce();
                return;
            }

            _health.Damaged += HandleDamaged;
            _health.Died += HandleDied;
            _subscribedToHealth = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (!_subscribedToHealth || _health == null)
            {
                _subscribedToHealth = false;
                return;
            }

            _health.Damaged -= HandleDamaged;
            _health.Died -= HandleDied;
            _subscribedToHealth = false;
        }

        private void HandleDamaged(HealthComponent healthComponent, DamageInfo damageInfo)
        {
            if (healthComponent != _health || !_health.IsAlive)
            {
                return;
            }

            PlayHit();
        }

        private void HandleDied(HealthComponent healthComponent)
        {
            if (healthComponent == _health)
            {
                SetDead(true);
            }
        }

        private void UpdateMoveSpeed()
        {
            if (_animator == null)
            {
                LogMissingAnimatorOnce();
                return;
            }

            if (!_hasMoveSpeedParameter)
            {
                return;
            }

            float speed = 0f;
            if (_agent != null && _agent.enabled)
            {
                speed = _agent.velocity.magnitude;
            }
            else
            {
                LogMissingAgentOnce();
            }

            _animator.SetFloat(_moveSpeedHash, speed, _moveSpeedDampTime, Time.deltaTime);
        }

        private void RefreshParameterHashes()
        {
            _moveSpeedHash = Animator.StringToHash(_moveSpeedParameter);
            _attackTriggerHash = Animator.StringToHash(_attackTriggerParameter);
            _hitTriggerHash = Animator.StringToHash(_hitTriggerParameter);
            _deadBoolHash = Animator.StringToHash(_deadBoolParameter);

            _hasMoveSpeedParameter = HasParameter(_moveSpeedHash, AnimatorControllerParameterType.Float);
            _hasAttackTriggerParameter = HasParameter(_attackTriggerHash, AnimatorControllerParameterType.Trigger);
            _hasHitTriggerParameter = HasParameter(_hitTriggerHash, AnimatorControllerParameterType.Trigger);
            _hasDeadBoolParameter = HasParameter(_deadBoolHash, AnimatorControllerParameterType.Bool);
        }

        private bool HasParameter(int parameterHash, AnimatorControllerParameterType expectedType)
        {
            if (_animator == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.nameHash == parameterHash && parameter.type == expectedType)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanUseAnimatorParameter(bool hasParameter)
        {
            if (_animator == null)
            {
                LogMissingAnimatorOnce();
                return false;
            }

            return hasParameter;
        }

        private void LogMissingAnimatorOnce()
        {
            if (_hasLoggedMissingAnimator)
            {
                return;
            }

            Debug.LogWarning($"{nameof(EnemyAnimatorBridge)} on {name} could not find an Animator in child visuals.", this);
            _hasLoggedMissingAnimator = true;
        }

        private void LogMissingAgentOnce()
        {
            if (_hasLoggedMissingAgent)
            {
                return;
            }

            Debug.LogWarning($"{nameof(EnemyAnimatorBridge)} on {name} could not find an enabled NavMeshAgent. MoveSpeed will stay at 0.", this);
            _hasLoggedMissingAgent = true;
        }

        private void LogMissingHealthOnce()
        {
            if (_hasLoggedMissingHealth)
            {
                return;
            }

            Debug.LogWarning($"{nameof(EnemyAnimatorBridge)} on {name} could not find a HealthComponent. Hit/death animation must be triggered externally.", this);
            _hasLoggedMissingHealth = true;
        }
    }
}
