using UnityEngine;
using UnityEngine.Events;

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

        [Header("Events")]
        [SerializeField]
        private UnityEvent onAttack;

        private IDamageable _targetDamageable;
        private float _nextAttackTime;
        private bool _attackEnabled = true;

        private void Awake()
        {
            CacheTargetDamageable();
        }

        private void Update()
        {
            TryAttack();
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
            Vector3 hitDirection = offset.sqrMagnitude > 0.0001f
                ? offset.normalized
                : transform.forward;

            _targetDamageable.TakeDamage(new DamageInfo(
                damage,
                gameObject,
                target.position,
                hitDirection));

            onAttack?.Invoke();
            return true;
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
        }
#endif
    }
}
