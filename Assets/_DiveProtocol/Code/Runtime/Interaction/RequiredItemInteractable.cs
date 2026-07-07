using System.Collections.Generic;
using DiveProtocol.Inventory;
using UnityEngine;
using UnityEngine.Events;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// Completes a generic interaction when the player satisfies all configured item requirements.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RequiredItemInteractable : InteractableBase
    {
        [Header("Requirements")]
        [SerializeField] private List<ItemRequirement> requirements = new();

        [Header("Prompt")]
        [SerializeField] private string readyPrompt = "Activate";
        [SerializeField] private string missingPrompt = "Requires {0}";
        [SerializeField] private bool logSuccessfulCompletion = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onCompleted;

        public bool IsCompleted { get; private set; }
        public IReadOnlyList<ItemRequirement> Requirements => requirements;

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) &&
                   !IsCompleted &&
                   HasValidRequirements();
        }

        public override string GetInteractionPrompt(GameObject interactor)
        {
            PlayerItemInventory inventory = FindInventory(interactor);
            if (ItemRequirementUtility.AreAllMet(inventory, requirements))
            {
                return string.IsNullOrWhiteSpace(readyPrompt)
                    ? "Activate"
                    : readyPrompt;
            }

            string missingSummary =
                ItemRequirementUtility.BuildMissingSummary(inventory, requirements);

            if (string.IsNullOrWhiteSpace(missingSummary))
            {
                missingSummary = "Item";
            }

            string format = string.IsNullOrWhiteSpace(missingPrompt)
                ? "Requires {0}"
                : missingPrompt;

            return string.Format(format, missingSummary);
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            PlayerItemInventory inventory = FindInventory(interactor);
            if (inventory == null)
            {
                Debug.LogError(
                    "[RequiredItem] Cannot complete interaction because no PlayerItemInventory was found on the player.",
                    this);
                return;
            }

            if (!ItemRequirementUtility.TryConsumeRequiredItems(inventory, requirements))
            {
                return;
            }

            IsCompleted = true;

            if (logSuccessfulCompletion)
            {
                Debug.Log("[RequiredItem] Requirements completed.", this);
            }

            onCompleted?.Invoke();
        }

        private bool HasValidRequirements()
        {
            if (requirements == null)
            {
                return false;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                ItemRequirement requirement = requirements[i];
                if (requirement != null && requirement.IsValid)
                {
                    return true;
                }
            }

            return false;
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
    }
}
