using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Runtime payload emitted when the player hits a damageable target with a weapon.
    /// </summary>
    public readonly struct WeaponHitInfo
    {
        public WeaponHitInfo(
            PlayerHitscanWeapon weapon,
            IDamageable target,
            Collider hitCollider,
            float finalDamage,
            bool targetDied,
            Vector3 hitPoint,
            Vector3 shotDirection)
        {
            Weapon = weapon;
            Target = target;
            HitCollider = hitCollider;
            FinalDamage = finalDamage;
            TargetDied = targetDied;
            HitPoint = hitPoint;
            ShotDirection = shotDirection;
        }

        public PlayerHitscanWeapon Weapon { get; }
        public IDamageable Target { get; }
        public Collider HitCollider { get; }
        public float FinalDamage { get; }
        public bool TargetDied { get; }
        public Vector3 HitPoint { get; }
        public Vector3 ShotDirection { get; }
    }
}
