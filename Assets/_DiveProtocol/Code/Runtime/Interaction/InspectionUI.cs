using DiveProtocol.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiveProtocol.Interaction
{
    /// <summary>
    /// Runtime-only reading panel controller for documents, logs, and read-only terminals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InspectionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text closeHintText;

        [Header("Text")]
        [SerializeField] private string closeHint = "Press E or Esc to close";

        private bool _ignoreCloseInputThisFrame;

        /// <summary>
        /// True while the inspection panel is visible and holding gameplay input.
        /// </summary>
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (_ignoreCloseInputThisFrame)
            {
                _ignoreCloseInputThisFrame = false;
                return;
            }

            if (WasClosePressedThisFrame())
            {
                Close();
            }
        }

        /// <summary>
        /// Opens the reading panel and locks normal gameplay input.
        /// </summary>
        public bool Open(string title, string body)
        {
            if (!ValidateReferences())
            {
                return false;
            }

            titleText.text = string.IsNullOrWhiteSpace(title)
                ? "Untitled"
                : title;

            bodyText.text = string.IsNullOrWhiteSpace(body)
                ? string.Empty
                : body;

            if (closeHintText != null)
            {
                closeHintText.text = string.IsNullOrWhiteSpace(closeHint)
                    ? "Press E or Esc to close"
                    : closeHint;
            }

            panelRoot.SetActive(true);
            IsOpen = true;
            _ignoreCloseInputThisFrame = true;
            GameplayInputLock.Acquire(this);
            return true;
        }

        /// <summary>
        /// Closes the reading panel and releases its gameplay input lock.
        /// </summary>
        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            IsOpen = false;
            _ignoreCloseInputThisFrame = false;
            GameplayInputLock.Release(this);
        }

        private bool ValidateReferences()
        {
            if (panelRoot == null)
            {
                Debug.LogError("[Inspection] InspectionUI requires a Panel Root.", this);
                return false;
            }

            if (titleText == null)
            {
                Debug.LogError("[Inspection] InspectionUI requires a Title Text.", this);
                return false;
            }

            if (bodyText == null)
            {
                Debug.LogError("[Inspection] InspectionUI requires a Body Text.", this);
                return false;
            }

            return true;
        }

        private static bool WasClosePressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.eKey.wasPressedThisFrame ||
                    keyboard.escapeKey.wasPressedThisFrame);
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            Close();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(closeHint))
            {
                closeHint = "Press E or Esc to close";
            }
        }
#endif
    }
}
