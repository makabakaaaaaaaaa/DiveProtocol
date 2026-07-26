using System;
using UnityEngine;

namespace DiveProtocol.Enemies.CorpseReanimation
{
    /// <summary>
    /// Shared numeric defaults for corpse reanimation behaviour.
    /// </summary>
    [Serializable]
    public sealed class CorpseReanimationSettings
    {
        [SerializeField, Min(0.1f)] private float detectionRadius = 2.5f;
        [SerializeField, Range(0f, 1f)] private float baseReanimationChance = 0.15f;
        [SerializeField, Range(0f, 1f)] private float activityChanceMultiplier = 0.45f;
        [SerializeField, Min(0f)] private float reanimationDelay = 0.6f;
        [SerializeField] private bool rollOnlyOnce = true;
        [SerializeField] private bool reanimateOnPlayerTouch;

        public float DetectionRadius => detectionRadius;
        public float BaseReanimationChance => baseReanimationChance;
        public float ActivityChanceMultiplier => activityChanceMultiplier;
        public float ReanimationDelay => reanimationDelay;
        public bool RollOnlyOnce => rollOnlyOnce;
        public bool ReanimateOnPlayerTouch => reanimateOnPlayerTouch;

        public float CalculateChance(float corpseActivity)
        {
            return Mathf.Clamp01(baseReanimationChance + Mathf.Clamp01(corpseActivity) * activityChanceMultiplier);
        }
    }
}
