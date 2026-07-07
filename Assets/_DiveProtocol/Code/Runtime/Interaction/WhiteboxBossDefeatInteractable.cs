using DiveProtocol.Encounters;
using UnityEngine;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// Temporary whitebox interaction that simulates defeating a boss before combat systems exist.
    /// </summary>
    public sealed class WhiteboxBossDefeatInteractable : InteractableBase
    {
        [Header("Whitebox Boss")]
        [SerializeField]
        private DefeatRewardDropper rewardDropper;

        [SerializeField]
        private string defeatPrompt = "Defeat Boss (Whitebox)";

        public override string GetInteractionPrompt(GameObject interactor)
        {
            return string.IsNullOrWhiteSpace(defeatPrompt)
                ? base.GetInteractionPrompt(interactor)
                : defeatPrompt;
        }

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor)
                && rewardDropper != null
                && !rewardDropper.IsDefeated;
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            rewardDropper.HandleDefeated();
        }

        private void Reset()
        {
            rewardDropper = GetComponentInParent<DefeatRewardDropper>();
        }
    }
}
