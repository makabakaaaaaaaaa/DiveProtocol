using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DiveProtocol.Inventory
{
    /// <summary>
    /// Shared runtime helpers for checking and consuming item requirements.
    /// </summary>
    public static class ItemRequirementUtility
    {
        private sealed class RequirementSummary
        {
            public string ItemId;
            public string DisplayName;
            public int RequiredAmount;
            public int ConsumeAmount;
        }

        public static bool AreAllMet(
            PlayerItemInventory inventory,
            IReadOnlyList<ItemRequirement> requirements)
        {
            if (inventory == null ||
                !BuildSummaries(requirements, out List<RequirementSummary> summaries))
            {
                return false;
            }

            for (int i = 0; i < summaries.Count; i++)
            {
                RequirementSummary summary = summaries[i];
                if (!inventory.HasItem(summary.ItemId, summary.RequiredAmount))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryConsumeRequiredItems(
            PlayerItemInventory inventory,
            IReadOnlyList<ItemRequirement> requirements)
        {
            if (inventory == null ||
                !BuildSummaries(requirements, out List<RequirementSummary> summaries))
            {
                return false;
            }

            for (int i = 0; i < summaries.Count; i++)
            {
                RequirementSummary summary = summaries[i];
                if (!inventory.HasItem(summary.ItemId, summary.RequiredAmount))
                {
                    return false;
                }
            }

            List<RequirementSummary> consumedSummaries = new List<RequirementSummary>();

            for (int i = 0; i < summaries.Count; i++)
            {
                RequirementSummary summary = summaries[i];
                if (summary.ConsumeAmount <= 0)
                {
                    continue;
                }

                if (!inventory.TryConsumeItem(summary.ItemId, summary.ConsumeAmount))
                {
                    for (int rollbackIndex = 0; rollbackIndex < consumedSummaries.Count; rollbackIndex++)
                    {
                        RequirementSummary consumedSummary = consumedSummaries[rollbackIndex];
                        inventory.AddItem(consumedSummary.ItemId, consumedSummary.ConsumeAmount);
                    }

                    Debug.LogError(
                        $"[Inventory] Failed to consume required item '{summary.ItemId}' x{summary.ConsumeAmount} after requirements were already validated.");
                    return false;
                }

                consumedSummaries.Add(summary);
            }

            return true;
        }

        public static string BuildMissingSummary(
            PlayerItemInventory inventory,
            IReadOnlyList<ItemRequirement> requirements)
        {
            if (!BuildSummaries(requirements, out List<RequirementSummary> summaries))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < summaries.Count; i++)
            {
                RequirementSummary summary = summaries[i];
                int currentAmount = inventory != null
                    ? inventory.GetItemCount(summary.ItemId)
                    : 0;

                if (currentAmount >= summary.RequiredAmount)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(summary.DisplayName);

                if (currentAmount > 0 || summary.RequiredAmount > 1)
                {
                    builder.Append(' ');
                    builder.Append(currentAmount);
                    builder.Append('/');
                    builder.Append(summary.RequiredAmount);
                }
            }

            return builder.ToString();
        }

        private static bool BuildSummaries(
            IReadOnlyList<ItemRequirement> requirements,
            out List<RequirementSummary> summaries)
        {
            summaries = new List<RequirementSummary>();

            if (requirements == null || requirements.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                ItemRequirement requirement = requirements[i];
                if (requirement == null || !requirement.IsValid)
                {
                    continue;
                }

                string itemId = requirement.ItemId;
                RequirementSummary summary = FindSummary(summaries, itemId);

                if (summary == null)
                {
                    summary = new RequirementSummary
                    {
                        ItemId = itemId,
                        DisplayName = string.IsNullOrWhiteSpace(requirement.DisplayName)
                            ? itemId
                            : requirement.DisplayName,
                        RequiredAmount = 0,
                        ConsumeAmount = 0
                    };

                    summaries.Add(summary);
                }

                summary.RequiredAmount += requirement.RequiredAmount;

                if (requirement.ConsumeOnComplete)
                {
                    summary.ConsumeAmount += requirement.RequiredAmount;
                }
            }

            return summaries.Count > 0;
        }

        private static RequirementSummary FindSummary(
            List<RequirementSummary> summaries,
            string itemId)
        {
            for (int i = 0; i < summaries.Count; i++)
            {
                RequirementSummary summary = summaries[i];
                if (string.Equals(
                        summary.ItemId,
                        itemId,
                        System.StringComparison.Ordinal))
                {
                    return summary;
                }
            }

            return null;
        }
    }
}
