using DiveProtocol.Builds;
using UnityEngine;

namespace DiveProtocol.Loot
{
    /// <summary>
    /// Rolls at most one Roguelite drop once when an enemy HealthComponent dies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLootDropper : MonoBehaviour
    {
        private static readonly BuildChoiceProvider ChoiceProvider = new();

        [Header("Health")]
        [SerializeField] private HealthComponent healthComponent;

        [Header("Roguelite Drop Prefabs")]
        [SerializeField] private GameObject healingDropPrefab;
        [SerializeField] private GameObject ammoDropPrefab;
        [SerializeField] private GameObject randomBuildDropPrefab;

        [Header("Normal Enemy Probabilities")]
        [SerializeField, Range(0f, 1f)] private float noDropChance = 0.30f;
        [SerializeField, Range(0f, 1f)] private float healingDropChance = 0.30f;
        [SerializeField, Range(0f, 1f)] private float ammoDropChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float randomBuildDropChance = 0.15f;

        [Header("Placement")]
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

            if (!TryRollDrop(Random.value, out DropItemType dropType))
            {
                return;
            }

            if (dropType == DropItemType.RandomBuildDrop && !HasAvailableMinorBuild())
            {
                return;
            }

            Vector3 basePosition = dropOrigin != null ? dropOrigin.position : transform.position;
            Vector2 scatter = scatterRadius > 0f ? Random.insideUnitCircle * scatterRadius : Vector2.zero;
            Vector3 dropPosition = basePosition + new Vector3(scatter.x, 0.15f, scatter.y);
            GameObject prefab = GetPrefab(dropType);
            if (prefab == null)
            {
                Debug.LogWarning($"[Loot] {name} rolled {dropType}, but no matching pickup prefab is configured.", this);
                return;
            }

            Instantiate(prefab, dropPosition, Quaternion.identity);
        }

        /// <summary>Maps a normalized roll to the normal-enemy 30/30/25/15 drop table.</summary>
        public bool TryRollDrop(float roll, out DropItemType dropType)
        {
            roll = Mathf.Clamp01(roll);
            float threshold = noDropChance;
            if (roll < threshold)
            {
                dropType = default;
                return false;
            }

            threshold += healingDropChance;
            if (roll < threshold)
            {
                dropType = DropItemType.HealingDrop;
                return true;
            }

            threshold += ammoDropChance;
            if (roll < threshold)
            {
                dropType = DropItemType.AmmoDrop;
                return true;
            }

            dropType = DropItemType.RandomBuildDrop;
            return roll < threshold + randomBuildDropChance;
        }

        /// <summary>Assigns the fixed normal-enemy table and its three configured pickup prefabs.</summary>
        public void ConfigureRogueliteDrops(
            GameObject healingPrefab,
            GameObject ammoPrefab,
            GameObject buildPrefab)
        {
            healingDropPrefab = healingPrefab;
            ammoDropPrefab = ammoPrefab;
            randomBuildDropPrefab = buildPrefab;
            noDropChance = 0.30f;
            healingDropChance = 0.30f;
            ammoDropChance = 0.25f;
            randomBuildDropChance = 0.15f;
        }

        private GameObject GetPrefab(DropItemType dropType)
        {
            return dropType switch
            {
                DropItemType.HealingDrop => healingDropPrefab,
                DropItemType.AmmoDrop => ammoDropPrefab,
                DropItemType.RandomBuildDrop => randomBuildDropPrefab,
                _ => null
            };
        }

        private static bool HasAvailableMinorBuild()
        {
            return AppRoot.TryGetInstance(out AppRoot appRoot) &&
                   appRoot.RunManager.CurrentRun != null &&
                   appRoot.RunManager.CurrentRun.IsActive &&
                   ChoiceProvider.GetMinorUpgradeCandidates(
                       appRoot.RunManager.CurrentRun.BuildState.OwnedUpgrades).Count > 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            scatterRadius = Mathf.Max(0f, scatterRadius);

            noDropChance = Mathf.Clamp01(noDropChance);
            healingDropChance = Mathf.Clamp01(healingDropChance);
            ammoDropChance = Mathf.Clamp01(ammoDropChance);
            randomBuildDropChance = Mathf.Clamp01(randomBuildDropChance);
        }
#endif
    }
}
