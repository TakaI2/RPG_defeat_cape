using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace RPGDefete.Story.UI
{
    /// <summary>
    /// Typewriter text effect that displays text character by character
    /// </summary>
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Characters displayed per second")]
        private float charactersPerSecond = 30f;

        [SerializeField]
        [Tooltip("Optional sound to play while typing")]
        private AudioClip typingSound;

        [SerializeField]
        [Tooltip("Play typing sound every N characters")]
        private int playSoundEveryNChars = 3;

        [SerializeField]
        [Tooltip("AudioSource for typing sound")]
        private AudioSource audioSource;

        private TMP_Text targetText;
        private string fullText;
        private bool isTyping;
        private bool skipRequested;

        /// <summary>
        /// Whether text is currently being typed
        /// </summary>
        public bool IsTyping => isTyping;

        /// <summary>
        /// Event fired when typing completes
        /// </summary>
        public event Action OnTypingCompleted;

        /// <summary>
        /// Initialize with target text component
        /// </summary>
        public void Initialize(TMP_Text text)
        {
            targetText = text;
        }

        /// <summary>
        /// Start typing the given text
        /// </summary>
        public IEnumerator StartTyping(string text)
        {
            if (targetText == null)
            {
                Debug.LogError("[TypewriterEffect] Target text is null");
                yield break;
            }

            fullText = text ?? "";
            isTyping = true;
            skipRequested = false;
            targetText.text = "";

            if (string.IsNullOrEmpty(fullText))
            {
                isTyping = false;
                OnTypingCompleted?.Invoke();
                yield break;
            }

            float delay = charactersPerSecond > 0 ? 1f / charactersPerSecond : 0f;
            int charIndex = 0;

            foreach (char c in fullText)
            {
                if (skipRequested)
                {
                    targetText.text = fullText;
                    break;
                }

                targetText.text += c;
                charIndex++;

                // Play typing sound
                if (typingSound != null && audioSource != null)
                {
                    if (charIndex % playSoundEveryNChars == 0 && !char.IsWhiteSpace(c))
                    {
                        audioSource.PlayOneShot(typingSound, 0.5f);
                    }
                }

                if (delay > 0)
                {
                    // Use unscaled time to work during pause
                    float elapsed = 0f;
                    while (elapsed < delay && !skipRequested)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
                else
                {
                    yield return null;
                }
            }

            isTyping = false;
            OnTypingCompleted?.Invoke();
        }

        /// <summary>
        /// Skip to end of current text immediately
        /// </summary>
        public void Skip()
        {
            if (isTyping)
            {
                skipRequested = true;
            }
        }

        /// <summary>
        /// Clear the current text
        /// </summary>
        public void Clear()
        {
            if (targetText != null)
            {
                targetText.text = "";
            }
            fullText = "";
            isTyping = false;
            skipRequested = false;
        }

        /// <summary>
        /// Set characters per second
        /// </summary>
        public void SetSpeed(float cps)
        {
            charactersPerSecond = Mathf.Max(0, cps);
        }
    }
}
