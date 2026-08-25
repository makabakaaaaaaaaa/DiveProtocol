using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiveProtocol.UI
{
    /// <summary>Reusable industrial terminal button with inverted idle and hover presentation.</summary>
    [DisallowMultipleComponent]
    public sealed class TerminalButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image _background;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _indexText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private Color _idleBackground = new(0f, 0.008f, 0.002f, 0.045f);
        [SerializeField] private Color _highlightedBackground = new(0.010f, 0.030f, 0.008f, 0.12f);
        [SerializeField] private Color _idleTextColor = Color.white;
        [SerializeField] private Color _highlightedTextColor = new(0.65f, 0.69f, 0.34f, 0.94f);

        private bool _isHighlighted;

        /// <summary>Assigns the UI pieces created by the scene layout.</summary>
        public void Configure(Image background, TMP_Text titleText, TMP_Text indexText, TMP_Text detailText)
        {
            _background = background;
            _titleText = titleText;
            _indexText = indexText;
            _detailText = detailText;
            RefreshStateAppearance();
        }

        public void SetContent(string title, string index, string detail)
        {
            if (_titleText != null) _titleText.text = title;
            if (_indexText != null) _indexText.text = index;
            if (_detailText != null) _detailText.text = detail;
        }

        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;
            ApplyTextColor();
        }

        /// <summary>Refreshes the serialized Button ColorBlock and the current text state.</summary>
        public void RefreshStateAppearance()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            ConfigureColorBlock();
            ApplyTextColor();
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true);
        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false);
        public void OnSelect(BaseEventData eventData) => SetHighlighted(false);
        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false);

        private void Awake()
        {
            RefreshStateAppearance();
        }

        private void Start()
        {
            // Selectable does not re-run its normal ColorBlock transition after colors change in Awake.
            RefreshStateAppearance();
            ApplyNormalBackground();
        }

        private void OnValidate()
        {
            RefreshStateAppearance();
        }

        private void ConfigureColorBlock()
        {
            if (_button == null)
            {
                return;
            }

            if (_background != null)
            {
                _button.targetGraphic = _background;
            }

            _button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = _button.colors;
            colors.normalColor = _highlightedBackground;
            colors.highlightedColor = _idleBackground;
            colors.selectedColor = _highlightedBackground;
            colors.pressedColor = _idleBackground;
            colors.colorMultiplier = 1f;
            _button.colors = colors;
        }

        private void ApplyTextColor()
        {
            Color color = _isHighlighted ? _highlightedTextColor : _idleTextColor;
            ApplyTextColor(_titleText, color);
            ApplyTextColor(_indexText, color);
            ApplyTextColor(_detailText, color);
        }

        private void ApplyNormalBackground()
        {
            if (_isHighlighted || _background == null || _button == null)
            {
                return;
            }

            _background.CrossFadeColor(_button.colors.normalColor, 0f, true, true);
        }

        private static void ApplyTextColor(TMP_Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }
    }
}
