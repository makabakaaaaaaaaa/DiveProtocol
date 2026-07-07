using UnityEngine;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// Interactable document, log, or read-only terminal that opens an InspectionUI panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InspectableInteractable : InteractableBase
    {
        [Header("Inspection")]
        [SerializeField] private string documentTitle = "Untitled";

        [SerializeField, TextArea(6, 18)]
        private string documentBody;

        [SerializeField] private bool allowRepeat = true;
        [SerializeField] private string readPrompt = "Read";

        /// <summary>
        /// True after this item has successfully opened its inspection panel at least once.
        /// </summary>
        public bool HasBeenRead { get; private set; }

        public override string InteractionPrompt =>
            string.IsNullOrWhiteSpace(readPrompt) ? "Read" : readPrompt;

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) &&
                   (allowRepeat || !HasBeenRead);
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (interactor == null)
            {
                Debug.LogError("[Inspection] Cannot read because the interactor is null.", this);
                return;
            }

            InspectionUI inspectionUI = FindInspectionUI(interactor);
            if (inspectionUI == null)
            {
                Debug.LogError(
                    "[Inspection] Cannot read because no InspectionUI was found under the player. " +
                    "Add InspectionUI to an always-active Gameplay UI object under the player and assign its panel references.",
                    this);
                return;
            }

            if (inspectionUI.Open(documentTitle, documentBody))
            {
                HasBeenRead = true;
            }
        }

        private static InspectionUI FindInspectionUI(GameObject interactor)
        {
            return interactor.GetComponentInChildren<InspectionUI>(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(readPrompt))
            {
                readPrompt = "Read";
            }
        }
#endif
    }
}
