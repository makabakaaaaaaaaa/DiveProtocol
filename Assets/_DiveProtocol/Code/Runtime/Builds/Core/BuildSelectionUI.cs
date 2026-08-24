using System;
using System.Collections.Generic;
using DiveProtocol.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Small runtime-generated modal for selecting one of three build upgrades.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildSelectionUI : MonoBehaviour
    {
        private static BuildSelectionUI _instance;

        private readonly List<Button> _buttons = new();
        private Action<BuildUpgradeId> _selectionAccepted;
        private CursorLockMode _previousCursorLockState;
        private bool _previousCursorVisible;
        private GameObject _panel;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public static BuildSelectionUI GetOrCreate()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var root = new GameObject("Build Selection UI");
            _instance = root.AddComponent<BuildSelectionUI>();
            return _instance;
        }

        public bool Show(
            IReadOnlyList<BuildUpgradeDefinition> choices,
            Func<BuildUpgradeId, bool> trySelect)
        {
            if (choices == null || choices.Count == 0 || trySelect == null || IsOpen)
            {
                return false;
            }

            EnsureVisuals();
            _selectionAccepted = id =>
            {
                if (trySelect(id))
                {
                    Close();
                }
            };

            for (int index = 0; index < _buttons.Count; index++)
            {
                Button button = _buttons[index];
                bool hasChoice = index < choices.Count;
                button.gameObject.SetActive(hasChoice);
                button.onClick.RemoveAllListeners();

                if (!hasChoice)
                {
                    continue;
                }

                BuildUpgradeDefinition choice = choices[index];
                Text label = button.GetComponentInChildren<Text>();
                label.text = $"{choice.DisplayName}\n[{choice.Branch}]\n{choice.ShortDescription}";
                BuildUpgradeId selectedId = choice.Id;
                button.onClick.AddListener(() => _selectionAccepted?.Invoke(selectedId));
            }

            _previousCursorLockState = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameplayInputLock.Acquire(this);
            _panel.SetActive(true);
            return true;
        }

        public void Close()
        {
            if (_panel == null || !_panel.activeSelf)
            {
                return;
            }

            _panel.SetActive(false);
            _selectionAccepted = null;
            GameplayInputLock.Release(this);
            Cursor.lockState = _previousCursorLockState;
            Cursor.visible = _previousCursorVisible;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            GameplayInputLock.Release(this);
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void EnsureVisuals()
        {
            if (_panel != null)
            {
                return;
            }

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            _panel = CreateUiObject("Panel", transform);
            RectTransform panelTransform = _panel.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelTransform.pivot = new Vector2(0.5f, 0.5f);
            panelTransform.sizeDelta = new Vector2(720f, 430f);
            _panel.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.06f, 0.96f);

            Text title = CreateText("Title", _panel.transform, 28, TextAnchor.MiddleCenter);
            title.text = "SELECT AUGMENTATION";
            RectTransform titleTransform = title.rectTransform;
            titleTransform.anchorMin = new Vector2(0f, 1f);
            titleTransform.anchorMax = new Vector2(1f, 1f);
            titleTransform.pivot = new Vector2(0.5f, 1f);
            titleTransform.anchoredPosition = new Vector2(0f, -28f);
            titleTransform.sizeDelta = new Vector2(-48f, 44f);

            for (int index = 0; index < 3; index++)
            {
                Button button = CreateChoiceButton(index, _panel.transform);
                _buttons.Add(button);
            }

            _panel.SetActive(false);
        }

        private static Button CreateChoiceButton(int index, Transform parent)
        {
            GameObject buttonObject = CreateUiObject($"Choice {index + 1}", parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.17f, 0.19f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.85f, 0.92f, 1f);
            button.colors = colors;

            RectTransform transform = buttonObject.GetComponent<RectTransform>();
            transform.anchorMin = new Vector2(0f, 1f);
            transform.anchorMax = new Vector2(1f, 1f);
            transform.pivot = new Vector2(0.5f, 1f);
            transform.anchoredPosition = new Vector2(0f, -92f - index * 104f);
            transform.sizeDelta = new Vector2(-48f, 88f);

            Text label = CreateText("Label", buttonObject.transform, 17, TextAnchor.MiddleLeft);
            RectTransform labelTransform = label.rectTransform;
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = new Vector2(18f, 8f);
            labelTransform.offsetMax = new Vector2(-18f, -8f);
            return button;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("Build Selection EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
