using System.Collections.Generic;
using DiveProtocol.Interaction;
using DiveProtocol.Inventory;
using UnityEngine;

namespace DiveProtocol.Doors
{
    /// <summary>
    /// Player interaction adapter that applies door access rules before requesting door motion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorInteractable : InteractableBase
    {
        [Header("Door")]
        [SerializeField] private DoorController doorController;

        [Header("Access")]
        [SerializeField] private DoorAccessMode accessMode = DoorAccessMode.Unlocked;
        [SerializeField] private bool startUnlocked;

        [Header("Item Requirements")]
        [SerializeField] private List<ItemRequirement> itemRequirements = new();

        [Header("Legacy Single Requirement")]
        [SerializeField] private string requiredItemId;
        [SerializeField] private string requiredItemDisplayName;
        [SerializeField] private bool consumeRequiredItem;
        [SerializeField] private Transform oneWayUnlockSideMarker;

        [Header("One Way Side")]
        [Tooltip("Optional explicit side reference. If unset, One Way Unlock Side Marker is used for backward compatibility.")]
        [SerializeField] private Transform allowedSideReference;
        [SerializeField, Range(-1f, 1f)] private float allowedSideDotThreshold;
        [SerializeField] private bool invertAllowedSide;

        [Header("Prompt")]
        [SerializeField] private string openPrompt = "Open Door";
        [SerializeField] private string closePrompt = "Close Door";
        [SerializeField] private string lockedPrompt = "Locked";
        [SerializeField] private string missingItemPrompt = "Requires {0}";
        [SerializeField] private string unlockPrompt = "Unlock Door";
        [SerializeField] private string wrongSidePrompt = "Locked from the other side";

        private bool _isUnlocked;
        private bool _isExternallyLocked;
        private bool _loggedInvalidRequiredItem;
        private bool _loggedInvalidOneWayMarker;

        public DoorAccessMode AccessMode => accessMode;
        public bool IsUnlocked => accessMode == DoorAccessMode.Unlocked || _isUnlocked;
        public bool IsExternallyLocked => _isExternallyLocked;

        public override string InteractionPrompt
        {
            get
            {
                if (doorController != null && doorController.IsOpen)
                {
                    return GetSafePrompt(closePrompt, "Close Door");
                }

                return GetSafePrompt(openPrompt, "Open Door");
            }
        }

        private void Awake()
        {
            ResolveDoorController();
            InitializeAccessState();
        }

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) &&
                   doorController != null &&
                   !doorController.IsMoving;
        }

        public override string GetInteractionPrompt(GameObject interactor)
        {
            if (_isExternallyLocked)
            {
                return GetSafePrompt(lockedPrompt, "Locked");
            }

            if (IsUnlocked)
            {
                return InteractionPrompt;
            }

            switch (accessMode)
            {
                case DoorAccessMode.RequiresItem:
                    if (!HasAnyValidItemRequirement())
                    {
                        return GetSafePrompt(lockedPrompt, "Locked");
                    }

                    return InteractorMeetsItemRequirements(interactor)
                        ? GetSafePrompt(unlockPrompt, "Unlock Door")
                        : FormatMissingItemPrompt(interactor);

                case DoorAccessMode.OneWayLatch:
                    if (!HasValidOneWayMarker())
                    {
                        return GetSafePrompt(lockedPrompt, "Locked");
                    }

                    return IsInteractorOnOneWayUnlockSide(interactor)
                        ? GetSafePrompt(unlockPrompt, "Unlock Door")
                        : GetSafePrompt(wrongSidePrompt, "Locked from the other side");

                case DoorAccessMode.Unlocked:
                default:
                    return InteractionPrompt;
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (_isExternallyLocked)
            {
                return;
            }

            if (!IsUnlocked && !TryUnlock(interactor))
            {
                return;
            }

            doorController.Toggle();
        }

        /// <summary>
        /// Applies or clears an external access override without changing permanent unlock state.
        /// </summary>
        public void SetExternalLock(bool locked)
        {
            _isExternallyLocked = locked;
        }

        /// <summary>
        /// Permanently unlocks this door for the current scene lifetime.
        /// </summary>
        public void UnlockPermanently()
        {
            _isUnlocked = true;
        }

        private void ResolveDoorController()
        {
            if (doorController != null)
            {
                return;
            }

            doorController = GetComponent<DoorController>();
            if (doorController != null)
            {
                return;
            }

            doorController = GetComponentInParent<DoorController>();
        }

        private void InitializeAccessState()
        {
            _isUnlocked = accessMode == DoorAccessMode.Unlocked || startUnlocked;

            if (accessMode == DoorAccessMode.RequiresItem &&
                !HasAnyValidItemRequirement())
            {
                LogInvalidRequiredItemOnce();
            }

            if (accessMode == DoorAccessMode.OneWayLatch &&
                !HasValidOneWayMarker())
            {
                LogInvalidOneWayMarkerOnce();
            }
        }

        private bool TryUnlock(GameObject interactor)
        {
            switch (accessMode)
            {
                case DoorAccessMode.RequiresItem:
                    return TryUnlockWithRequiredItem(interactor);

                case DoorAccessMode.OneWayLatch:
                    return TryUnlockOneWayLatch(interactor);

                case DoorAccessMode.Unlocked:
                default:
                    _isUnlocked = true;
                    return true;
            }
        }

        private bool TryUnlockWithRequiredItem(GameObject interactor)
        {
            if (!HasAnyValidItemRequirement())
            {
                LogInvalidRequiredItemOnce();
                return false;
            }

            PlayerItemInventory inventory = FindInventory(interactor);
            if (inventory == null)
            {
                return false;
            }

            if (HasValidItemRequirementList())
            {
                if (!ItemRequirementUtility.TryConsumeRequiredItems(inventory, itemRequirements))
                {
                    return false;
                }
            }
            else
            {
                if (!inventory.HasItem(requiredItemId, 1))
                {
                    return false;
                }

                if (consumeRequiredItem &&
                    !inventory.TryConsumeItem(requiredItemId, 1))
                {
                    return false;
                }
            }

            _isUnlocked = true;
            return true;
        }

        private bool TryUnlockOneWayLatch(GameObject interactor)
        {
            if (!HasValidOneWayMarker())
            {
                LogInvalidOneWayMarkerOnce();
                return false;
            }

            if (!IsInteractorOnOneWayUnlockSide(interactor))
            {
                return false;
            }

            _isUnlocked = true;
            return true;
        }

        private bool InteractorMeetsItemRequirements(GameObject interactor)
        {
            PlayerItemInventory inventory = FindInventory(interactor);
            if (inventory == null)
            {
                return false;
            }

            if (HasValidItemRequirementList())
            {
                return ItemRequirementUtility.AreAllMet(inventory, itemRequirements);
            }

            return inventory.HasItem(requiredItemId, 1);
        }

        private static PlayerItemInventory FindInventory(GameObject interactor)
        {
            if (interactor == null)
            {
                return null;
            }

            if (interactor.TryGetComponent(out PlayerItemInventory inventory))
            {
                return inventory;
            }

            inventory = interactor.GetComponentInChildren<PlayerItemInventory>(true);
            if (inventory != null)
            {
                return inventory;
            }

            return interactor.GetComponentInParent<PlayerItemInventory>();
        }

        private bool IsInteractorOnOneWayUnlockSide(GameObject interactor)
        {
            if (interactor == null ||
                !HasValidOneWayMarker())
            {
                return false;
            }

            Transform sideReference = GetOneWaySideReference();
            Vector3 allowedDirection = sideReference.forward;
            if (invertAllowedSide)
            {
                allowedDirection = -allowedDirection;
            }

            Vector3 interactorDirection =
                interactor.transform.position - sideReference.position;

            allowedDirection.y = 0f;
            interactorDirection.y = 0f;

            if (allowedDirection.sqrMagnitude < 0.0001f ||
                interactorDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            allowedDirection.Normalize();
            interactorDirection.Normalize();

            return Vector3.Dot(allowedDirection, interactorDirection) >=
                   allowedSideDotThreshold;
        }

        private bool HasValidRequiredItemId()
        {
            return !string.IsNullOrWhiteSpace(requiredItemId);
        }

        private bool HasAnyValidItemRequirement()
        {
            return HasValidItemRequirementList() || HasValidRequiredItemId();
        }

        private bool HasValidItemRequirementList()
        {
            if (itemRequirements == null)
            {
                return false;
            }

            for (int i = 0; i < itemRequirements.Count; i++)
            {
                ItemRequirement requirement = itemRequirements[i];
                if (requirement != null && requirement.IsValid)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasValidOneWayMarker()
        {
            Transform sideReference = GetOneWaySideReference();
            if (sideReference == null)
            {
                return false;
            }

            Vector3 referenceForward = sideReference.forward;
            referenceForward.y = 0f;
            return referenceForward.sqrMagnitude >= 0.0001f;
        }

        private Transform GetOneWaySideReference()
        {
            return allowedSideReference != null
                ? allowedSideReference
                : oneWayUnlockSideMarker;
        }

        private string FormatMissingItemPrompt(GameObject interactor)
        {
            string missingSummary = null;

            if (HasValidItemRequirementList())
            {
                PlayerItemInventory inventory = FindInventory(interactor);
                missingSummary = ItemRequirementUtility.BuildMissingSummary(
                    inventory,
                    itemRequirements);
            }

            if (!string.IsNullOrWhiteSpace(missingSummary))
            {
                return string.Format(GetSafePrompt(missingItemPrompt, "Requires {0}"), missingSummary);
            }

            string itemName = string.IsNullOrWhiteSpace(requiredItemDisplayName)
                ? requiredItemId?.Trim()
                : requiredItemDisplayName.Trim();

            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = "Item";
            }

            string format = string.IsNullOrWhiteSpace(missingItemPrompt)
                ? "Requires {0}"
                : missingItemPrompt;

            return string.Format(format, itemName);
        }

        private static string GetSafePrompt(string prompt, string fallback)
        {
            return string.IsNullOrWhiteSpace(prompt)
                ? fallback
                : prompt;
        }

        private void LogInvalidRequiredItemOnce()
        {
            if (_loggedInvalidRequiredItem)
            {
                return;
            }

            _loggedInvalidRequiredItem = true;
            Debug.LogError(
                "[Door] DoorInteractable is set to RequiresItem but Required Item Id is empty.",
                this);
        }

        private void LogInvalidOneWayMarkerOnce()
        {
            if (_loggedInvalidOneWayMarker)
            {
                return;
            }

            _loggedInvalidOneWayMarker = true;
            Debug.LogError(
                "[Door] DoorInteractable is set to OneWayLatch but no valid one-way side reference is assigned.",
                this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            allowedSideDotThreshold = Mathf.Clamp(
                allowedSideDotThreshold,
                -1f,
                1f);

            if (string.IsNullOrWhiteSpace(openPrompt))
            {
                openPrompt = "Open Door";
            }

            if (string.IsNullOrWhiteSpace(closePrompt))
            {
                closePrompt = "Close Door";
            }

            if (string.IsNullOrWhiteSpace(lockedPrompt))
            {
                lockedPrompt = "Locked";
            }

            if (string.IsNullOrWhiteSpace(missingItemPrompt))
            {
                missingItemPrompt = "Requires {0}";
            }

            if (string.IsNullOrWhiteSpace(unlockPrompt))
            {
                unlockPrompt = "Unlock Door";
            }

            if (string.IsNullOrWhiteSpace(wrongSidePrompt))
            {
                wrongSidePrompt = "Locked from the other side";
            }
        }
#endif
    }
}
