using System;
using UnityEngine;

namespace DiveProtocol.Inventory
{
    /// <summary>
    /// Serializable item requirement data used by doors, sockets, and generic interactables.
    /// </summary>
    [Serializable]
    public sealed class ItemRequirement
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int requiredAmount = 1;
        [SerializeField] private bool consumeOnComplete;

        public string ItemId => itemId?.Trim();

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName.Trim();
                }

                return ItemId;
            }
        }

        public int RequiredAmount => Mathf.Max(1, requiredAmount);
        public bool ConsumeOnComplete => consumeOnComplete;

        public bool IsValid => !string.IsNullOrWhiteSpace(ItemId);
    }
}
