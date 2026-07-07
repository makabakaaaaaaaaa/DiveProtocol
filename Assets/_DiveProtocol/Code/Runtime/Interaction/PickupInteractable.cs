using System;
using DiveProtocol.Inventory;
using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// Adds an item stack to the interacting player's runtime inventory.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupInteractable : InteractableBase
    {
        [Header("Pickup")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private bool deactivateAfterPickup = true;
        [SerializeField] private bool logSuccessfulPickup = true;
        [SerializeField] private string pickupPrompt = "Pick Up";

        [Header("Events")]
        [SerializeField] private UnityEvent onPickedUp;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public int Amount => amount;
        public bool HasBeenPickedUp { get; private set; }

        /// <summary>
        /// Raised once after this pickup successfully adds its item to the interacting player's inventory.
        /// </summary>
        public event Action<PickupInteractable, GameObject> PickedUp;

        public override string InteractionPrompt =>
            string.IsNullOrWhiteSpace(pickupPrompt) ? "Pick Up" : pickupPrompt;

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) &&
                   !HasBeenPickedUp &&
                   TryNormalizeItemId(itemId, out _) &&
                   amount > 0;
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (interactor == null)
            {
                Debug.LogError("[Pickup] Cannot pick up item because the interactor is null.", this);
                return;
            }

            PlayerItemInventory inventory = FindInventory(interactor);
            if (inventory == null)
            {
                Debug.LogError(
                    "[Pickup] Cannot pick up item because no PlayerItemInventory was found on the player. " +
                    "Add PlayerItemInventory to the player root object.",
                    this);
                return;
            }

            if (!inventory.AddItem(itemId, amount))
            {
                Debug.LogError(
                    $"[Pickup] Failed to add item '{itemId}' x{amount} to PlayerItemInventory.",
                    this);
                return;
            }

            HasBeenPickedUp = true;
            PickedUp?.Invoke(this, interactor);
            onPickedUp?.Invoke();

            if (logSuccessfulPickup)
            {
                string shownName = string.IsNullOrWhiteSpace(displayName)
                    ? itemId.Trim()
                    : displayName.Trim();

                Debug.Log(
                    $"Picked up {shownName} x{amount} [{itemId.Trim()}]",
                    this);
            }

            if (deactivateAfterPickup)
            {
                gameObject.SetActive(false);
            }
            else
            {
                enabled = false;
            }
        }

        private static PlayerItemInventory FindInventory(GameObject interactor)
        {
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

        private static bool TryNormalizeItemId(string value, out string normalizedValue)
        {
            normalizedValue = value?.Trim();
            return !string.IsNullOrWhiteSpace(normalizedValue);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);

            if (string.IsNullOrWhiteSpace(pickupPrompt))
            {
                pickupPrompt = "Pick Up";
            }
        }
#endif
    }
}
