using UnityEngine;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// 鎵€鏈夐渶瑕佺帺瀹朵富鍔ㄦ寜閿搷浣滅殑瀵硅薄锛岄兘搴旂户鎵胯繖涓被銆?
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField]
        private string interactionPrompt = "Interact";

        /// <summary>
        /// 浠ュ悗浜や簰鎻愮ずUI浼氳鍙栬繖涓枃鏈€?
        /// </summary>
        public virtual string InteractionPrompt => interactionPrompt;

        /// <summary>
        /// Indicates whether the shared screen-space interaction prompt should represent this interactable.
        /// World-space interactables can opt out and provide their own local prompt.
        /// </summary>
        public virtual bool UsesScreenPrompt => true;

        /// <summary>
        /// Returns the prompt shown to a specific interactor. Override for player-dependent prompts.
        /// </summary>
        public virtual string GetInteractionPrompt(GameObject interactor)
        {
            return InteractionPrompt;
        }

        /// <summary>
        /// 褰撳墠瀵硅薄鏄惁鍏佽琚寚瀹氱帺瀹朵氦浜掋€?
        /// 閿侀棬銆佸凡鎷惧彇鐗╁搧绛夊彲浠ラ噸鍐欒鏂规硶銆?
        /// </summary>
        public virtual bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled;
        }

        /// <summary>
        /// 鐜╁鐪熸鎵ц浜や簰鏃惰皟鐢ㄣ€?
        /// </summary>
        public abstract void Interact(GameObject interactor);
    }
}
