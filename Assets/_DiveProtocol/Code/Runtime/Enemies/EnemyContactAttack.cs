using UnityEngine;
using UnityEngine.Events;
using DiveProtocol.Builds;
using DiveProtocol.Enemies;

namespace DiveProtocol
{
    /// <summary>
    /// Minimal enemy melee/contact attack that damages a supplied target at cooldown intervals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyContactAttack : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        private Transform target;

        [Header("Attack")]
        [SerializeField, Min(0f)]
        private float attackRange = 1.5f;

        [SerializeField, Min(0.01f)]
        private float attackCooldownSeconds = 1f;

        [SerializeField, Min(0f)]
        private float damage = 15f;

        [Tooltip("Delay between starting the attack animation and applying damage.")]
        [SerializeField, Min(0f)]
        private float hitDelaySeconds = 0.35f;

        [Tooltip("When enabled, the target must still be in range at the hit frame to receive damage.")]
        [SerializeField]
        private bool requireTargetInRangeAtHit = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onAttack;

        private IDamageable _targetDamageable;
        private float _nextAttackTime;
        private bool _attackEnabled = true;
        private bool _attackInProgress;
        private Coroutine _attackCoroutine;
        private EnemyAnimatorBridge _animatorBridge;

        private void Awake()
        {
            CacheTargetDamageable();
            _animatorBridge = GetComponent<EnemyAnimatorBridge>();
        }

        private void Update()
        {
            TryAttack();
        }

        private void OnDisable()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            _attackInProgress = false;
        }

        /// <summary>
        /// Supplies the target this enemy should attack.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            CacheTargetDamageable();
        }

        /// <summary>
        /// Clears the current attack target.
        /// </summary>
        public void ClearTarget()
        {
            target = null;
            _targetDamageable = null;
        }

        /// <summary>
        /// Enables or disables attacking without disabling the component.
        /// </summary>
        public void SetAttackEnabled(bool enabled)
        {
            _attackEnabled = enabled;
        }

        /// <summary>
        /// Attempts one contact attack if the target is alive, in range, and off cooldown.
        /// </summary>
        public bool TryAttack()
        {
            if (!_attackEnabled ||
                _attackInProgress ||
                target == null ||
                _targetDamageable == null ||
                !_targetDamageable.IsAlive ||
                Time.time < _nextAttackTime)
            {
                return false;
            }

            Vector3 offset = target.position - transform.position;
            if (offset.sqrMagnitude > attackRange * attackRange)
            {
                return false;
            }

            _nextAttackTime = Time.time + attackCooldownSeconds;
            _attackInProgress = true;
            _animatorBridge ??= GetComponent<EnemyAnimatorBridge>();
            _animatorBridge?.PlayAttack();
            onAttack?.Invoke();

            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }

            _attackCoroutine = StartCoroutine(ApplyDamageAfterHitDelay());
            return true;
        }

        private System.Collections.IEnumerator ApplyDamageAfterHitDelay()
        {
            if (hitDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(hitDelaySeconds);
            }

            _attackCoroutine = null;

            if (!_attackEnabled ||
                target == null ||
                _targetDamageable == null ||
                !_targetDamageable.IsAlive)
            {
                _attackInProgress = false;
                yield break;
            }

            Vector3 offset = target.position - transform.position;
            if (requireTargetInRangeAtHit &&
                offset.sqrMagnitude > attackRange * attackRange)
            {
                _attackInProgress = false;
                yield break;
            }

            Vector3 hitDirection = offset.sqrMagnitude > 0.0001f
                ? offset.normalized
                : transform.forward;

            PlayerBuildController buildController = target.GetComponentInParent<PlayerBuildController>();
            if (buildController != null &&
                buildController.Symbiosis != null &&
                buildController.Symbiosis.TryTriggerPollutionCoat(gameObject))
            {
                _attackInProgress = false;
                yield break;
            }

            _targetDamageable.TakeDamage(new DamageInfo(
                damage,
                gameObject,
                target.position,
                hitDirection,
                DamageType.Contact));

            _attackInProgress = false;
        }

        private void CacheTargetDamageable()
        {
            _targetDamageable = target != null
                ? target.GetComponentInParent<IDamageable>()
                : null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            attackRange = Mathf.Max(0f, attackRange);
            attackCooldownSeconds = Mathf.Max(0.01f, attackCooldownSeconds);
            damage = Mathf.Max(0f, damage);
            hitDelaySeconds = Mathf.Max(0f, hitDelaySeconds);
        }
#endif
    }
}
