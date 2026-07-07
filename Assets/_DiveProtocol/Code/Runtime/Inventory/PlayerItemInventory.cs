using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiveProtocol.Inventory
{
    /// <summary>
    /// Runtime item inventory attached to the player for key items and small carried objects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerItemInventory : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemStack
        {
            [SerializeField] private string itemId;
            [SerializeField, Min(1)] private int amount = 1;

            public string ItemId => itemId;
            public int Amount => amount;

            public ItemStack(string itemId, int amount)
            {
                this.itemId = itemId;
                this.amount = amount;
            }

            internal void Set(string newItemId, int newAmount)
            {
                itemId = newItemId;
                amount = newAmount;
            }

            internal void Add(int addedAmount)
            {
                amount += addedAmount;
            }

            internal void Remove(int removedAmount)
            {
                amount -= removedAmount;
            }
        }

        [Header("Runtime Items")]
        [SerializeField] private List<ItemStack> items = new();

        /// <summary>
        /// Raised when an item's total count changes. Count is zero after full removal.
        /// </summary>
        public event Action<string, int> ItemCountChanged;

        /// <summary>
        /// Read-only view of the current runtime item stacks.
        /// </summary>
        public IReadOnlyList<ItemStack> Items => items;

        private void Awake()
        {
            SanitizeItems();
        }

        public bool HasItem(string itemId, int requiredAmount = 1)
        {
            return TryNormalizeItemId(itemId, out string normalizedId) &&
                   requiredAmount > 0 &&
                   GetItemCount(normalizedId) >= requiredAmount;
        }

        public int GetItemCount(string itemId)
        {
            if (!TryNormalizeItemId(itemId, out string normalizedId))
            {
                return 0;
            }

            int index = FindItemIndex(normalizedId);
            return index >= 0 ? items[index].Amount : 0;
        }

        public bool AddItem(string itemId, int amount = 1)
        {
            if (!TryNormalizeItemId(itemId, out string normalizedId) ||
                amount <= 0)
            {
                return false;
            }

            int index = FindItemIndex(normalizedId);
            int newAmount;

            if (index >= 0)
            {
                items[index].Add(amount);
                newAmount = items[index].Amount;
            }
            else
            {
                items.Add(new ItemStack(normalizedId, amount));
                newAmount = amount;
            }

            ItemCountChanged?.Invoke(normalizedId, newAmount);
            return true;
        }

        public bool TryConsumeItem(string itemId, int amount = 1)
        {
            if (!TryNormalizeItemId(itemId, out string normalizedId) ||
                amount <= 0)
            {
                return false;
            }

            int index = FindItemIndex(normalizedId);
            if (index < 0 || items[index].Amount < amount)
            {
                return false;
            }

            items[index].Remove(amount);
            int remainingAmount = items[index].Amount;

            if (remainingAmount <= 0)
            {
                items.RemoveAt(index);
                remainingAmount = 0;
            }

            ItemCountChanged?.Invoke(normalizedId, remainingAmount);
            return true;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            return TryConsumeItem(itemId, amount);
        }

        public void Clear()
        {
            if (items.Count == 0)
            {
                return;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                ItemStack stack = items[i];
                string removedId = stack.ItemId;
                items.RemoveAt(i);
                ItemCountChanged?.Invoke(removedId, 0);
            }
        }

        private void SanitizeItems()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                ItemStack stack = items[i];
                if (stack == null ||
                    !TryNormalizeItemId(stack.ItemId, out string normalizedId) ||
                    stack.Amount <= 0)
                {
                    items.RemoveAt(i);
                    continue;
                }

                stack.Set(normalizedId, stack.Amount);
            }

            for (int i = 0; i < items.Count; i++)
            {
                ItemStack current = items[i];

                for (int j = items.Count - 1; j > i; j--)
                {
                    ItemStack other = items[j];
                    if (!string.Equals(
                            current.ItemId,
                            other.ItemId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    current.Add(other.Amount);
                    items.RemoveAt(j);
                }
            }
        }

        private int FindItemIndex(string normalizedItemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ItemStack stack = items[i];
                if (stack == null)
                {
                    continue;
                }

                if (string.Equals(
                        stack.ItemId,
                        normalizedItemId,
                        StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryNormalizeItemId(string itemId, out string normalizedItemId)
        {
            normalizedItemId = itemId?.Trim();
            return !string.IsNullOrWhiteSpace(normalizedItemId);
        }
    }
}
