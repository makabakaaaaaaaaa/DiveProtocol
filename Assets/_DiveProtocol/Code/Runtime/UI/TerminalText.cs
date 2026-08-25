using TMPro;
using UnityEngine;

namespace DiveProtocol.UI
{
    /// <summary>Applies a consistent low-saturation terminal treatment to TMP text.</summary>
    [DisallowMultipleComponent]
    public sealed class TerminalText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Color _color = new(0.67f, 0.72f, 0.39f, 1f);
        [SerializeField, Range(-10f, 20f)] private float _characterSpacing = 3f;

        public void Configure(TMP_Text text, Color color, float characterSpacing)
        {
            _text = text;
            _color = color;
            _characterSpacing = characterSpacing;
            Apply();
        }

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Apply()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }

            if (_text == null)
            {
                return;
            }

            _text.color = _color;
            _text.characterSpacing = _characterSpacing;
        }
    }
}
