using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Broad damage category used by optional build modifiers without forcing a full damage-type system.
    /// </summary>
    public enum DamageType
    {
        Normal,
        Gun,
        Melee,
        Contact,
        Environmental,
        Pollution,
        BloodCost
    }

    /// <summary>
    /// Lightweight runtime damage payload shared by players, enemies, bosses, and future systems.
    /// </summary>
    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            GameObject source = null,
            Vector3 hitPoint = default,
            Vector3 hitDirection = default,
            DamageType damageType = DamageType.Normal)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            DamageType = damageType;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitDirection { get; }
        public DamageType DamageType { get; }
    }
}
