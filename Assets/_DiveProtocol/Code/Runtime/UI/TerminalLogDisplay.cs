using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DiveProtocol.UI
{
    /// <summary>Types a looping facility-monitoring log into a terminal text field.</summary>
    [DisallowMultipleComponent]
    public sealed class TerminalLogDisplay : MonoBehaviour
    {
        private static readonly string[] NormalMessages =
        {
            "FACILITY NETWORK CONNECTED",
            "NODE DP-01 ONLINE",
            "ARCHIVE DATABASE ACCESSIBLE",
            "SECURITY SYSTEM ACTIVE",
            "DESCENT SYSTEM READY",
            "WAITING FOR OPERATOR"
        };

        private static readonly string[] SuspiciousMessages =
        {
            "UNKNOWN SIGNAL DETECTED",
            "SOURCE:\nLOWER FACILITY SECTOR",
            "SIGNAL LOST",
            "UNAUTHORIZED MOVEMENT DETECTED",
            "CAMERA FEED CORRUPTED"
        };

        private static readonly string[] RareAbnormalMessages =
        {
            "SUBJECT COUNT:\nEXPECTED 12\nCURRENT 13",
            "LAST OPERATOR LOGIN:\nUNKNOWN",
            "DO NOT OPEN ARCHIVE 07"
        };

        [Header("References")]
        [SerializeField] private TMP_Text _text;

        [Header("Timing")]
        [SerializeField, Min(0.005f)] private float _typingSpeed = 0.05f;
        [SerializeField, Min(0f)] private float _delayBetweenMessages = 1.15f;
        [SerializeField, Min(1)] private int _maximumVisibleLines = 9;

        [Header("Message Selection")]
        [SerializeField, Range(0f, 1f)] private float _abnormalMessageProbability = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _rareAbnormalMessageProbability = 0.025f;

        private readonly List<string> _visibleMessages = new();
        private Coroutine _displayRoutine;
        private int _previousNormalIndex = -1;
        private int _previousSuspiciousIndex = -1;
        private int _previousRareIndex = -1;

        /// <summary>Assigns the display and its initial terminal behavior.</summary>
        public void Configure(TMP_Text text, float typingSpeed, float delayBetweenMessages, float abnormalMessageProbability)
        {
            _text = text;
            _typingSpeed = Mathf.Max(0.005f, typingSpeed);
            _delayBetweenMessages = Mathf.Max(0f, delayBetweenMessages);
            _abnormalMessageProbability = Mathf.Clamp01(abnormalMessageProbability);
        }

        private void OnEnable()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }

            if (_text == null)
            {
                Debug.LogWarning("[TerminalLogDisplay] Missing TMP text reference.", this);
                return;
            }

            _visibleMessages.Clear();
            _text.text = string.Empty;
            _displayRoutine = StartCoroutine(DisplayLogs());
        }

        private void OnDisable()
        {
            if (_displayRoutine != null)
            {
                StopCoroutine(_displayRoutine);
                _displayRoutine = null;
            }
        }

        private IEnumerator DisplayLogs()
        {
            while (true)
            {
                string message = SelectMessage();
                int messageLineCount = CountLines(message);

                if (CountVisibleLines() + messageLineCount > _maximumVisibleLines)
                {
                    _visibleMessages.Clear();
                }

                yield return TypeMessage(message);
                _visibleMessages.Add(message);

                if (_delayBetweenMessages > 0f)
                {
                    yield return new WaitForSecondsRealtime(_delayBetweenMessages);
                }
            }
        }

        private IEnumerator TypeMessage(string message)
        {
            for (int characterIndex = 0; characterIndex < message.Length; characterIndex++)
            {
                _text.text = ComposeDisplay(message.Substring(0, characterIndex + 1));
                yield return new WaitForSecondsRealtime(_typingSpeed);
            }
        }

        private string SelectMessage()
        {
            float roll = Random.value;
            if (roll <= _rareAbnormalMessageProbability)
            {
                return SelectWithoutImmediateRepeat(RareAbnormalMessages, ref _previousRareIndex);
            }

            if (roll <= _rareAbnormalMessageProbability + _abnormalMessageProbability)
            {
                return SelectWithoutImmediateRepeat(SuspiciousMessages, ref _previousSuspiciousIndex);
            }

            return SelectWithoutImmediateRepeat(NormalMessages, ref _previousNormalIndex);
        }

        private string ComposeDisplay(string currentMessage)
        {
            if (_visibleMessages.Count == 0)
            {
                return currentMessage;
            }

            return string.Join("\n", _visibleMessages) + "\n" + currentMessage;
        }

        private int CountVisibleLines()
        {
            int lineCount = 0;
            foreach (string message in _visibleMessages)
            {
                lineCount += CountLines(message);
            }

            return lineCount;
        }

        private static int CountLines(string message)
        {
            int lineCount = 1;
            foreach (char character in message)
            {
                if (character == '\n')
                {
                    lineCount++;
                }
            }

            return lineCount;
        }

        private static string SelectWithoutImmediateRepeat(string[] messages, ref int previousIndex)
        {
            int index = Random.Range(0, messages.Length);
            if (messages.Length > 1 && index == previousIndex)
            {
                index = (index + 1) % messages.Length;
            }

            previousIndex = index;
            return messages[index];
        }
    }
}
