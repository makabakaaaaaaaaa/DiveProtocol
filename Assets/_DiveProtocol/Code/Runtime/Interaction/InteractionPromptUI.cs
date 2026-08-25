using TMPro;
using UnityEngine;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// 根据 PlayerInteractor 当前检测到的对象，
    /// 显示或隐藏屏幕交互提示。
    ///
    /// 建议挂在玩家 Prefab 下的 Screen Space Overlay Canvas 上。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]

        [Tooltip("玩家根对象上的 PlayerInteractor。")]
        [SerializeField]
        private PlayerInteractor playerInteractor;

        [Tooltip("整个提示区域。隐藏提示时会关闭这个对象。")]
        [SerializeField]
        private GameObject promptRoot;

        [Tooltip("用于显示交互文本的 TextMeshPro 文本。")]
        [SerializeField]
        private TMP_Text promptText;

        [Header("Text")]

        [Tooltip("显示在提示中的交互按键名称。")]
        [SerializeField]
        private string keyLabel = "E";

        [Tooltip("{0} 是按键，{1} 是交互对象提供的提示文本。")]
        [SerializeField]
        private string promptFormat = "[{0}] {1}";

        private InteractableBase currentInteractable;
        private string currentPrompt;

        private void Awake()
        {
            if (playerInteractor == null)
            {
                playerInteractor =
                    GetComponentInParent<PlayerInteractor>();
            }

            SetPromptVisible(false);
        }

        private void OnEnable()
        {
            if (playerInteractor == null)
            {
                playerInteractor =
                    GetComponentInParent<PlayerInteractor>();
            }

            if (playerInteractor == null)
            {
                Debug.LogError(
                    $"[{nameof(InteractionPromptUI)}] " +
                    "找不到 PlayerInteractor。",
                    this);

                SetPromptVisible(false);
                return;
            }

            playerInteractor.CurrentInteractableChanged +=
                HandleCurrentInteractableChanged;

            HandleCurrentInteractableChanged(
                playerInteractor.CurrentInteractable);
        }

        private void Update()
        {
            if (currentInteractable == null)
            {
                return;
            }

            RefreshPrompt(false);
        }

        private void OnDisable()
        {
            if (playerInteractor != null)
            {
                playerInteractor.CurrentInteractableChanged -=
                    HandleCurrentInteractableChanged;
            }

            SetPromptVisible(false);
        }

        private void HandleCurrentInteractableChanged(
            InteractableBase interactable)
        {
            currentInteractable = interactable;

            if (interactable == null)
            {
                currentPrompt = null;
                SetPromptVisible(false);
                return;
            }

            if (!interactable.UsesScreenPrompt)
            {
                currentPrompt = null;
                SetPromptVisible(false);
                return;
            }

            if (promptText == null)
            {
                Debug.LogError(
                    $"[{nameof(InteractionPromptUI)}] " +
                    "没有指定 Prompt Text。",
                    this);

                SetPromptVisible(false);
                return;
            }

            RefreshPrompt(true);
            SetPromptVisible(true);
        }

        private void RefreshPrompt(bool force)
        {
            if (currentInteractable == null ||
                promptText == null ||
                playerInteractor == null)
            {
                return;
            }

            string nextPrompt =
                currentInteractable.GetInteractionPrompt(
                    playerInteractor.gameObject);

            if (!force && string.Equals(currentPrompt, nextPrompt))
            {
                return;
            }

            currentPrompt = nextPrompt;
            promptText.text = string.Format(
                promptFormat,
                keyLabel,
                currentPrompt);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptRoot == null)
            {
                return;
            }

            if (promptRoot.activeSelf != visible)
            {
                promptRoot.SetActive(visible);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(keyLabel))
            {
                keyLabel = "E";
            }

            if (string.IsNullOrWhiteSpace(promptFormat))
            {
                promptFormat = "[{0}] {1}";
            }
        }
#endif
    }
}
