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
            return base.CanInteract(interactor) && !HasBeenUsedThisRun() && !GameplayInputLock.IsLocked;
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

            if (!LevelBuildSelectionCatalog.TryGetForActiveScene(out LevelBuildSelectionDefinition definition) ||
                !BuildSelectionFlow.TryOpen(interactor, definition, _choiceCount, HandleSelectionCompleted))
            {
                return;
            }
        }

        private void Update()
        {
            if (!_activateOnPlayerEnter || HasBeenUsedThisRun() || GameplayInputLock.IsLocked ||
                !TryGetComponent(out Collider triggerCollider) || !triggerCollider.enabled)
            {
                return;
            }

            PlayerInteractor playerInteractor = FindFirstObjectByType<PlayerInteractor>();
            Collider playerCollider = playerInteractor != null
                ? playerInteractor.GetComponent<Collider>() ?? playerInteractor.GetComponentInChildren<Collider>()
                : null;
            if (playerCollider != null && triggerCollider.bounds.Intersects(playerCollider.bounds))
            {
                TryOpenSelection(playerInteractor.gameObject);
            }
        }

        private bool HasBeenUsedThisRun()
        {
            if (_hasBeenUsed)
            {
                return true;
            }

            return AppRoot.TryGetInstance(out AppRoot appRoot) &&
                   appRoot.RunManager.CurrentRun != null &&
                   LevelBuildSelectionCatalog.TryGetForActiveScene(out LevelBuildSelectionDefinition definition) &&
                   appRoot.RunManager.CurrentRun.BuildState.HasClaimedSelectionNode(definition.NodeId);
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
