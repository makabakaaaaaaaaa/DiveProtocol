using DiveProtocol.Interaction;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Periodically applies Humus pollution damage around the player.
    /// </summary>
    public sealed class SymbiosisAuraDamage
    {
        private readonly Collider[] _hits = new Collider[32];
        private readonly Transform _owner;
        private readonly LayerMask _targetMask;
        private readonly float _radius;
        private float _nextTickTime;

        public SymbiosisAuraDamage(Transform owner, LayerMask targetMask, float radius)
        {
            _owner = owner;
            _targetMask = targetMask;
            _radius = radius;
        }

        public void Tick(int stacks, float intervalSeconds)
        {
            if (_owner == null || stacks <= 0 || Time.time < _nextTickTime)
            {
                return;
            }

            _nextTickTime = Time.time + Mathf.Max(0.1f, intervalSeconds);
            int hitCount = Physics.OverlapSphereNonAlloc(
                _owner.position,
                _radius,
                _hits,
                _targetMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hits[i];
                if (hit == null || hit.transform.root == _owner.root)
                {
                    continue;
                }

                if (hit.GetComponentInParent<PlayerInteractor>() != null)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                damageable.TakeDamage(new DamageInfo(
                    stacks,
                    _owner.gameObject,
                    hit.ClosestPoint(_owner.position),
                    hit.transform.position - _owner.position,
                    DamageType.Pollution));
            }
        }
    }
}
