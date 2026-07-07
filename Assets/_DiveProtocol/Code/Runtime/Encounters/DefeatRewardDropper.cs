using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol.Encounters
{
    /// <summary>
    /// Handles the one-time result of a whitebox or future combat defeat, such as revealing a reward pickup.
    /// </summary>
    public sealed class DefeatRewardDropper : MonoBehaviour
    {
        [Header("Reward")]
        [SerializeField]
        private GameObject rewardObject;

        [SerializeField]
        private GameObject[] deactivateOnDefeat;

        [SerializeField]
        private bool forceRewardInactiveOnAwake = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onDefeated;

        private bool _hasLoggedMissingReward;

        /// <summary>
        /// Gets whether this dropper has already processed its defeat result.
        /// </summary>
        public bool IsDefeated { get; private set; }

        private void Awake()
        {
            if (rewardObject == null)
            {
                LogMissingRewardOnce();
                return;
            }

            if (forceRewardInactiveOnAwake)
            {
                rewardObject.SetActive(false);
            }
        }

        /// <summary>
        /// Applies the defeat result once by hiding configured objects, revealing the reward, and invoking events.
        /// </summary>
        /// <returns>True when the defeat result was applied for the first time; otherwise false.</returns>
        public bool HandleDefeated()
        {
            if (IsDefeated)
            {
                return false;
            }

            IsDefeated = true;

            if (deactivateOnDefeat != null)
            {
                for (int i = 0; i < deactivateOnDefeat.Length; i++)
                {
                    GameObject target = deactivateOnDefeat[i];
                    if (target == null)
                    {
                        continue;
                    }

                    target.SetActive(false);
                }
            }

            if (rewardObject != null)
            {
                rewardObject.SetActive(true);
            }
            else
            {
                LogMissingRewardOnce();
            }

            onDefeated?.Invoke();
            return true;
        }

        private void LogMissingRewardOnce()
        {
            if (_hasLoggedMissingReward)
            {
                return;
            }

            _hasLoggedMissingReward = true;
            Debug.LogWarning(
                $"[Encounter] {nameof(DefeatRewardDropper)} on '{name}' has no reward object assigned.",
                this);
        }
    }
}
