using DiveProtocol.Pickups;
using UnityEngine;

namespace DiveProtocol.Loot
{
    /// <summary>
    /// Drops simple run resources once when an enemy HealthComponent dies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLootDropper : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private HealthComponent healthComponent;

        [Header("Drops")]
        [SerializeField] private LootDropEntry[] drops;
        [SerializeField] private Transform dropOrigin;
        [SerializeField, Min(0f)] private float scatterRadius = 0.5f;
        [SerializeField] private bool dropOnlyOnce = true;

        private bool _hasDropped;
        private bool _isSubscribed;

        private void Awake()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponent<HealthComponent>();
            }

            if (healthComponent == null)
            {
                healthComponent = GetComponentInChildren<HealthComponent>(true);
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || healthComponent == null)
            {
                return;
            }

            healthComponent.Died += HandleDied;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || healthComponent == null)
            {
                return;
            }

            healthComponent.Died -= HandleDied;
            _isSubscribed = false;
        }

        private void HandleDied(HealthComponent deadHealth)
        {
            DropLoot();
        }

        /// <summary>
        /// Rolls and spawns configured drops. Safe to call from HealthComponent events.
        /// </summary>
        public void DropLoot()
        {
            if (dropOnlyOnce && _hasDropped)
            {
                return;
            }

            _hasDropped = true;

            if (drops == null || drops.Length == 0)
            {
                return;
            }

            Vector3 basePosition = dropOrigin != null ? dropOrigin.position : transform.position;

            for (int i = 0; i < drops.Length; i++)
            {
                LootDropEntry entry = drops[i];
                if (entry == null ||
                    entry.prefab == null ||
                    entry.chance <= 0f ||
                    Random.value > entry.chance)
                {
                    continue;
                }

                int minAmount = Mathf.Max(1, entry.minAmount);
                int maxAmount = Mathf.Max(minAmount, entry.maxAmount);
                int amount = Random.Range(minAmount, maxAmount + 1);
                Vector2 scatter = scatterRadius > 0f ? Random.insideUnitCircle * scatterRadius : Vector2.zero;
                Vector3 dropPosition = basePosition + new Vector3(scatter.x, 0.15f, scatter.y);

                GameObject droppedObject = Instantiate(
                    entry.prefab,
                    dropPosition,
                    Quaternion.identity);

                if (droppedObject.TryGetComponent(out ResourcePickupInteractable pickup))
                {
                    pickup.SetAmount(amount);
                }
                else
                {
                    ResourcePickupInteractable childPickup =
                        droppedObject.GetComponentInChildren<ResourcePickupInteractable>(true);
                    if (childPickup != null)
                    {
                        childPickup.SetAmount(amount);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            scatterRadius = Mathf.Max(0f, scatterRadius);

            if (drops == null)
            {
                return;
            }

            for (int i = 0; i < drops.Length; i++)
            {
                LootDropEntry entry = drops[i];
                if (entry == null)
                {
                    continue;
                }

                entry.chance = Mathf.Clamp01(entry.chance);
                entry.minAmount = Mathf.Max(1, entry.minAmount);
                entry.maxAmount = Mathf.Max(entry.minAmount, entry.maxAmount);
            }
        }
#endif
    }
}
