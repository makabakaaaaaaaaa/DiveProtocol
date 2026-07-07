using DiveProtocol.Interaction;
using DiveProtocol.Inventory;
using UnityEngine;

namespace DiveProtocol.Pickups
{
    /// <summary>
    /// Interactable whitebox resource pickup for ammo, healing, and future build parts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourcePickupInteractable : InteractableBase
    {
        private const string DefaultBuildPartItemId = "BUILD_PART";

        [Header("Resource")]
        [SerializeField] private ResourcePickupType pickupType;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private string buildPartItemId = DefaultBuildPartItemId;
        [SerializeField] private bool deactivateAfterPickup = true;
        [SerializeField] private string pickupPrompt;

        public ResourcePickupType PickupType => pickupType;
        public int Amount => amount;
        public string BuildPartItemId => buildPartItemId;
        public bool HasBeenPickedUp { get; private set; }

        public override string InteractionPrompt =>
            string.IsNullOrWhiteSpace(pickupPrompt) ? GetDefaultPrompt() : pickupPrompt;

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) &&
                   !HasBeenPickedUp &&
                   amount > 0;
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor) || interactor == null)
            {
                return;
            }

            bool wasApplied = pickupType switch
            {
                ResourcePickupType.Ammo => TryApplyAmmo(interactor),
                ResourcePickupType.Health => TryApplyHealth(interactor),
                ResourcePickupType.BuildPart => TryApplyBuildPart(interactor),
                _ => false
            };

            if (!wasApplied)
            {
                return;
            }

            HasBeenPickedUp = true;

            if (deactivateAfterPickup)
            {
                gameObject.SetActive(false);
            }
            else
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Sets the amount carried by this pickup. Used by loot drops after instantiation.
        /// </summary>
        public void SetAmount(int newAmount)
        {
            amount = Mathf.Max(1, newAmount);
        }

        private bool TryApplyAmmo(GameObject interactor)
        {
            PlayerHitscanWeapon weapon = FindPlayerComponent<PlayerHitscanWeapon>(interactor);
            if (weapon == null)
            {
                Debug.LogWarning("[Pickup] Ammo pickup requires PlayerHitscanWeapon on the player.", this);
                return false;
            }

            return weapon.TryAddAmmo(amount);
        }

        private bool TryApplyHealth(GameObject interactor)
        {
            HealthComponent health = FindPlayerComponent<HealthComponent>(interactor);
            if (health == null)
            {
                Debug.LogWarning("[Pickup] Health pickup requires HealthComponent on the player.", this);
                return false;
            }

            return health.Heal(amount) > 0f;
        }

        private bool TryApplyBuildPart(GameObject interactor)
        {
            PlayerItemInventory inventory = FindPlayerComponent<PlayerItemInventory>(interactor);
            if (inventory == null)
            {
                Debug.LogWarning("[Pickup] Build Part pickup requires PlayerItemInventory on the player.", this);
                return false;
            }

            string itemId = string.IsNullOrWhiteSpace(buildPartItemId)
                ? DefaultBuildPartItemId
                : buildPartItemId.Trim();

            return inventory.AddItem(itemId, amount);
        }

        private string GetDefaultPrompt()
        {
            return pickupType switch
            {
                ResourcePickupType.Ammo => "Pick up Ammo",
                ResourcePickupType.Health => "Pick up Medkit",
                ResourcePickupType.BuildPart => "Pick up Build Part",
                _ => "Pick Up"
            };
        }

        private static T FindPlayerComponent<T>(GameObject interactor)
            where T : Component
        {
            if (interactor.TryGetComponent(out T component))
            {
                return component;
            }

            component = interactor.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }

            return interactor.GetComponentInParent<T>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);

            if (string.IsNullOrWhiteSpace(buildPartItemId))
            {
                buildPartItemId = DefaultBuildPartItemId;
            }
        }
#endif
    }
}
