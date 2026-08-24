using DiveProtocol.Gameplay;
using DiveProtocol.Interaction;
using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Interactable or trigger-volume entry point for one run-scoped three-choice build offer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildSelectionTrigger : InteractableBase
    {
        [SerializeField] private bool _activateOnPlayerEnter = true;
        [SerializeField] private bool _singleUse = true;
        [SerializeField, Min(1)] private int _choiceCount = 3;

        private bool _hasBeenUsed;

        public override string InteractionPrompt => _hasBeenUsed
            ? string.Empty
            : "Choose an augmentation";

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) && !_hasBeenUsed && !GameplayInputLock.IsLocked;
        }

        public override void Interact(GameObject interactor)
        {
            TryOpenSelection(interactor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_activateOnPlayerEnter || other == null)
            {
                return;
            }

            PlayerInteractor playerInteractor = other.GetComponentInParent<PlayerInteractor>();
            if (playerInteractor != null)
            {
                TryOpenSelection(playerInteractor.gameObject);
            }
        }

        private void TryOpenSelection(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (!BuildSelectionFlow.TryOpen(interactor, _choiceCount, HandleSelectionCompleted))
            {
                return;
            }
        }

        private void HandleSelectionCompleted()
        {
            _hasBeenUsed = _singleUse;
            if (_hasBeenUsed && TryGetComponent(out Collider triggerCollider))
            {
                triggerCollider.enabled = false;
            }
        }
    }
}
