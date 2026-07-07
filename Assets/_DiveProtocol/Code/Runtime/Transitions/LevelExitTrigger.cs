using DiveProtocol.Interaction;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Trigger-based level exit that starts a configured loading-scene transition when the player enters.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelExitTrigger : MonoBehaviour
    {
        [SerializeField]
        private SceneTransitionProfile transitionProfile;

        [SerializeField]
        private bool triggerOnlyOnce = true;

        private bool _hasTriggered;
        private bool _hasLoggedMissingProfile;

        private void OnTriggerEnter(Collider other)
        {
            if (other == null ||
                (triggerOnlyOnce && _hasTriggered) ||
                other.GetComponentInParent<PlayerInteractor>() == null)
            {
                return;
            }

            BeginTransition();
        }

        /// <summary>
        /// Begins the configured transition, allowing future UnityEvent callers such as boss death events.
        /// </summary>
        public void BeginTransition()
        {
            if (triggerOnlyOnce && _hasTriggered)
            {
                return;
            }

            if (transitionProfile == null)
            {
                LogMissingProfileOnce();
                return;
            }

            if (!SceneTransitionService.BeginTransition(transitionProfile))
            {
                return;
            }

            _hasTriggered = true;

            if (triggerOnlyOnce && TryGetComponent(out Collider triggerCollider))
            {
                triggerCollider.enabled = false;
            }
        }

        private void LogMissingProfileOnce()
        {
            if (_hasLoggedMissingProfile)
            {
                return;
            }

            _hasLoggedMissingProfile = true;
            Debug.LogError(
                $"[Transition] {nameof(LevelExitTrigger)} on '{name}' requires a SceneTransitionProfile.",
                this);
        }
    }
}
