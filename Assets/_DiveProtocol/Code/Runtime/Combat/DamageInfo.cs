using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Lightweight runtime damage payload shared by players, enemies, bosses, and future systems.
    /// </summary>
    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            GameObject source = null,
            Vector3 hitPoint = default,
            Vector3 hitDirection = default)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitDirection { get; }
    }
}
