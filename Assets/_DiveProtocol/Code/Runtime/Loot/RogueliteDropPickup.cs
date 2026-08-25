using DiveProtocol.Builds;
using DiveProtocol.Interaction;
using UnityEngine;

namespace DiveProtocol.Loot
{
    /// <summary>
    /// Run-local healing, ammo, or minor-build reward handled by the shared player interaction flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RogueliteDropPickup : InteractableBase
    {
        private static readonly BuildChoiceProvider ChoiceProvider = new();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const string InteractableLayerName = "Interactable";

        [SerializeField] private DropItemType _dropType;
        [SerializeField, Min(1)] private int _amount = 1;

        private MaterialPropertyBlock _propertyBlock;
        private bool _hasBeenCollected;

        public DropItemType DropType => _dropType;
        public int Amount => _amount;
        public override string InteractionPrompt => _dropType switch
        {
            DropItemType.HealingDrop => "Pick up Medkit",
            DropItemType.AmmoDrop => "Pick up Ammo",
            DropItemType.RandomBuildDrop => "Collect Mutation",
            _ => "Pick Up"
        };

        private void Awake()
        {
            EnsureTriggerCollider(assignInteractableLayer: true);
            ApplyVisualColor();
        }

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) &&
                   !_hasBeenCollected &&
                   interactor != null;
        }

        public override void Interact(GameObject interactor)
        {
            if (CanInteract(interactor))
            {
                TryCollect(interactor);
            }
        }

        /// <summary>Attempts to collect this drop for a player root. Returns true only when an effect was applied.</summary>
        public bool TryCollect(GameObject player)
        {
            if (_hasBeenCollected || player == null)
            {
                return false;
            }

            bool wasApplied;
            string feedback;
            switch (_dropType)
            {
                case DropItemType.HealingDrop:
                    wasApplied = TryApplyHealing(player);
                    feedback = $"+{_amount} HP";
                    break;
                case DropItemType.AmmoDrop:
                    wasApplied = TryApplyAmmo(player);
                    feedback = $"+{_amount} Ammo";
                    break;
                case DropItemType.RandomBuildDrop:
                    wasApplied = TryApplyRandomMinorBuild(player, out string upgradeName);
                    feedback = $"NEW MUTATION\n{upgradeName}";
                    break;
                default:
                    wasApplied = false;
                    feedback = null;
                    break;
            }
            if (!wasApplied)
            {
                return false;
            }

            _hasBeenCollected = true;
            Debug.Log($"[Loot] {feedback}", this);
            Destroy(gameObject);
            return true;
        }

        /// <summary>Sets the type and amount for an instantiated drop or prefab setup tool.</summary>
        public void Configure(DropItemType dropType, int amount)
        {
            _dropType = dropType;
            _amount = Mathf.Max(1, amount);
            ApplyVisualColor();
        }

        private bool TryApplyHealing(GameObject player)
        {
            HealthComponent health = FindPlayerComponent<HealthComponent>(player);
            return health != null && health.Heal(_amount) > 0f;
        }

        private bool TryApplyAmmo(GameObject player)
        {
            PlayerHitscanWeapon weapon = FindPlayerComponent<PlayerHitscanWeapon>(player);
            return weapon != null && weapon.TryAddAmmo(_amount);
        }

        private bool TryApplyRandomMinorBuild(GameObject player, out string upgradeName)
        {
            upgradeName = null;
            if (!AppRoot.TryGetInstance(out AppRoot appRoot) ||
                appRoot.RunManager.CurrentRun == null ||
                !appRoot.RunManager.CurrentRun.IsActive)
            {
                return false;
            }

            PlayerBuildController controller = FindPlayerComponent<PlayerBuildController>(player);
            if (controller == null)
            {
                return false;
            }

            RunState runState = appRoot.RunManager.CurrentRun;
            BuildUpgradeDefinition upgrade = ChoiceProvider.GetRandomMinorUpgrade(
                runState.BuildState.OwnedUpgrades,
                runState.Seed ^ Time.frameCount ^ GetInstanceID());
            if (upgrade == null || !BuildRunBridge.GrantUpgrade(runState, controller, upgrade.Id))
            {
                return false;
            }

            upgradeName = upgrade.DisplayName;
            Debug.Log($"[Loot] Collected minor build: {upgrade.DisplayName}.", this);
            return true;
        }

        private void EnsureTriggerCollider(bool assignInteractableLayer)
        {
            if (assignInteractableLayer)
            {
                int interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
                if (interactableLayer >= 0)
                {
                    gameObject.layer = interactableLayer;
                }
            }

            Collider pickupCollider = GetComponent<Collider>();
            if (pickupCollider != null)
            {
                pickupCollider.isTrigger = true;
            }
        }

        private void ApplyVisualColor()
        {
            Color color = _dropType switch
            {
                DropItemType.HealingDrop => new Color(0.80f, 0.10f, 0.12f, 1f),
                DropItemType.AmmoDrop => new Color(0.95f, 0.72f, 0.08f, 1f),
                DropItemType.RandomBuildDrop => new Color(0.04f, 0.05f, 0.06f, 1f),
                _ => Color.white
            };

            _propertyBlock ??= new MaterialPropertyBlock();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                if (renderer.sharedMaterial.HasProperty(BaseColorId))
                {
                    _propertyBlock.SetColor(BaseColorId, color);
                }
                else if (renderer.sharedMaterial.HasProperty(ColorId))
                {
                    _propertyBlock.SetColor(ColorId, color);
                }

                renderer.SetPropertyBlock(_propertyBlock);
                _propertyBlock.Clear();
            }
        }

        private static T FindPlayerComponent<T>(GameObject player)
            where T : Component
        {
            if (player.TryGetComponent(out T component))
            {
                return component;
            }

            component = player.GetComponentInChildren<T>(true);
            return component != null ? component : player.GetComponentInParent<T>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _amount = Mathf.Max(1, _amount);
            EnsureTriggerCollider(assignInteractableLayer: false);
        }
#endif
    }
}
