using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Trigger-compatible abnormal environment data for Humus Abnormal Metabolism.
    /// </summary>
    public sealed class AbnormalZone : MonoBehaviour
    {
        [SerializeField] private bool countsAsAbnormalEnvironment = true;
        [SerializeField, Min(0f)] private float healPerSecond = 1f;
        [SerializeField, Min(0.01f)] private float weaponSpreadMultiplier = 1.25f;

        public bool CountsAsAbnormalEnvironment => countsAsAbnormalEnvironment;
        public float HealPerSecond => healPerSecond;
        public float WeaponSpreadMultiplier => weaponSpreadMultiplier;
    }
}
