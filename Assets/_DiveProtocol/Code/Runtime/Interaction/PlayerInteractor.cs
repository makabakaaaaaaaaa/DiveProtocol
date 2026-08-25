using System;
using DiveProtocol.Gameplay;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// 妫€娴嬬帺瀹堕檮杩戝彲浠ヤ氦浜掔殑瀵硅薄锛屽苟鍦ㄦ寜涓婨鏃舵墽琛屼氦浜掋€?
    /// 鎸傚湪鐜╁Prefab鏍瑰璞′笂銆?
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Header("Detection")]

        [Tooltip("Center point used for interaction detection. Defaults to this transform.")]
        [SerializeField]
        private Transform interactionOrigin;

        [Tooltip("Maximum interaction distance.")]
        [SerializeField, Min(0.1f)]
        private float interactionRadius = 1.5f;

        [Tooltip("Only objects on these layers are considered interactable.")]
        [SerializeField]
        private LayerMask interactableLayers = ~0;

        [Header("Optional Facing Filter")]

        [Tooltip("When enabled, only objects in front of the player can be interacted with.")]
        [SerializeField]
        private bool requireFacing;

        [Tooltip("Minimum facing dot product required when the facing filter is enabled.")]
        [SerializeField, Range(-1f, 1f)]
        private float minimumFacingDot = 0f;

        [Header("Debug")]

        [Tooltip("Show interaction detection radius when the player is selected.")]
        [SerializeField]
        private bool showDebugGizmo = true;

        private readonly Collider[] overlapResults = new Collider[16];

        private InteractableBase currentInteractable;

        /// <summary>
        /// 褰撳墠璺濈鐜╁鏈€杩戙€佸彲浠ヤ氦浜掔殑瀵硅薄銆?
        /// </summary>
        public InteractableBase CurrentInteractable =>
            currentInteractable;

        /// <summary>Returns the configured range used by the shared interaction key.</summary>
        public float InteractionRadius => interactionRadius;

        /// <summary>
        /// 浠ュ悗浜や簰鎻愮ずUI鍙互璁㈤槄杩欎釜浜嬩欢銆?
        /// </summary>
        public event Action<InteractableBase>
            CurrentInteractableChanged;

        private void Awake()
        {
            if (interactionOrigin == null)
            {
                interactionOrigin = transform;
            }
        }

        private void Update()
        {
            if (GameplayInputLock.IsLocked)
            {
                SetCurrentInteractable(null);
                return;
            }

            RefreshCurrentInteractable();

            if (!ReadInteractPressed())
            {
                return;
            }

            if (currentInteractable == null)
            {
                return;
            }

            if (!currentInteractable.CanInteract(gameObject))
            {
                return;
            }

            currentInteractable.Interact(gameObject);
        }

        /// <summary>
        /// 鎼滅储鑼冨洿鍐呮渶鎺ヨ繎鐜╁鐨勬湁鏁堜氦浜掑璞°€?
        /// </summary>
        private void RefreshCurrentInteractable()
        {
            Transform origin = interactionOrigin != null
                ? interactionOrigin
                : transform;

            int hitCount = Physics.OverlapSphereNonAlloc(
                origin.position,
                interactionRadius,
                overlapResults,
                interactableLayers,
                QueryTriggerInteraction.Collide);

            InteractableBase bestCandidate = null;
            float bestDistanceSquared =
                float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = overlapResults[i];

                if (hitCollider == null)
                {
                    continue;
                }

                InteractableBase candidate =
                    hitCollider.GetComponentInParent<
                        InteractableBase>();

                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.CanInteract(gameObject))
                {
                    continue;
                }

                Vector3 closestPoint =
                    hitCollider.ClosestPoint(origin.position);

                Vector3 direction =
                    closestPoint - origin.position;

                if (requireFacing &&
                    !IsInsideFacingRange(origin, direction))
                {
                    continue;
                }

                float distanceSquared =
                    direction.sqrMagnitude;

                if (distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bestCandidate = candidate;
                bestDistanceSquared = distanceSquared;
            }

            SetCurrentInteractable(bestCandidate);
        }

        private bool IsInsideFacingRange(
            Transform origin,
            Vector3 directionToCandidate)
        {
            Vector3 flatDirection =
                directionToCandidate;

            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            Vector3 flatForward = origin.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            flatDirection.Normalize();
            flatForward.Normalize();

            float dot = Vector3.Dot(
                flatForward,
                flatDirection);

            return dot >= minimumFacingDot;
        }

        private void SetCurrentInteractable(
            InteractableBase newInteractable)
        {
            if (currentInteractable == newInteractable)
            {
                return;
            }

            currentInteractable = newInteractable;

            CurrentInteractableChanged?.Invoke(
                currentInteractable);
        }

        /// <summary>
        /// 璇诲彇鏈抚鏄惁鍒氬垰鎸変笅E銆?
        /// </summary>
        private static bool ReadInteractPressed()
        {
            return Keyboard.current != null &&
                   Keyboard.current.eKey.wasPressedThisFrame;
        }

        private void OnDisable()
        {
            SetCurrentInteractable(null);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            interactionRadius =
                Mathf.Max(0.1f, interactionRadius);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmo)
            {
                return;
            }

            Transform origin = interactionOrigin != null
                ? interactionOrigin
                : transform;

            Gizmos.DrawWireSphere(
                origin.position,
                interactionRadius);
        }
#endif
    }
}
