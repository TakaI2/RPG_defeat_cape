using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RPGDefete.Story.UI
{
    /// <summary>
    /// Message window UI for displaying character dialogue and portraits
    /// </summary>
    public class MessageWindow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private CanvasGroup windowRoot;

        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text dialogueText;

        [SerializeField]
        private Image leftPortrait;

        [SerializeField]
        private Image centerPortrait;

        [SerializeField]
        private Image rightPortrait;

        [SerializeField]
        private GameObject advanceIndicator;

        [Header("Settings")]
        [SerializeField]
        private float fadeInDuration = 0.3f;

        [SerializeField]
        private float fadeOutDuration = 0.2f;

        [SerializeField]
        private float portraitFadeDuration = 0.3f;

        [Header("Input")]
        [SerializeField]
        private KeyCode[] advanceKeys = { KeyCode.Space, KeyCode.Return, KeyCode.Mouse0 };

        private TypewriterEffect typewriter;
        private bool isWaitingForInput;
        private bool inputReceived;
        private Coroutine fadeCoroutine;

        /// <summary>
        /// Event fired when all dialogue is completed
        /// </summary>
        public event Action OnDialogueCompleted;

        /// <summary>
        /// Whether the window is currently visible
        /// </summary>
        public bool IsVisible => windowRoot != null && windowRoot.alpha > 0.5f;

        private void Awake()
        {
            // Get or add TypewriterEffect
            typewriter = GetComponent<TypewriterEffect>();
            if (typewriter == null)
            {
                typewriter = gameObject.AddComponent<TypewriterEffect>();
            }

            if (dialogueText != null)
            {
                typewriter.Initialize(dialogueText);
            }

            // Initially hide
            if (windowRoot != null)
            {
                windowRoot.alpha = 0f;
                windowRoot.interactable = false;
                windowRoot.blocksRaycasts = false;
            }

            // Hide all portraits initially
            ClearPortraits();
        }

        private void Update()
        {
            // Check for advance input
            if (isWaitingForInput || (typewriter != null && typewriter.IsTyping))
            {
                foreach (var key in advanceKeys)
                {
                    if (Input.GetKeyDown(key))
                    {
                        OnAdvanceInput();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Show the message window with fade in
        /// </summary>
        public void Show()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeWindow(1f, fadeInDuration));
        }

        /// <summary>
        /// Hide the message window with fade out
        /// </summary>
        public void Hide()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeWindow(0f, fadeOutDuration));
            ClearPortraits();
        }

        /// <summary>
        /// Display dialogue with character name, lines, and optional portrait
        /// </summary>
        public IEnumerator ShowDialogue(
            string characterName,
            List<string> lines,
            Sprite portrait = null,
            PortraitPosition position = PortraitPosition.Center)
        {
            if (nameText != null)
            {
                nameText.text = characterName ?? "";
            }

            // Set portrait if provided
            if (portrait != null)
            {
                yield return SetPortrait(portrait, position, true);
            }

            // Display each line
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    // Hide advance indicator while typing
                    if (advanceIndicator != null)
                    {
                        advanceIndicator.SetActive(false);
                    }

                    // Type out the line
                    yield return typewriter.StartTyping(line);

                    // Show advance indicator
                    if (advanceIndicator != null)
                    {
                        advanceIndicator.SetActive(true);
                    }

                    // Wait for input to advance
                    isWaitingForInput = true;
                    inputReceived = false;
                    yield return new WaitUntil(() => inputReceived);
                    isWaitingForInput = false;
                }
            }

            OnDialogueCompleted?.Invoke();
        }

        /// <summary>
        /// Set a portrait image at the specified position
        /// </summary>
        public IEnumerator SetPortrait(Sprite sprite, PortraitPosition position, bool fadeIn)
        {
            Image targetImage = position switch
            {
                PortraitPosition.Left => leftPortrait,
                PortraitPosition.Right => rightPortrait,
                _ => centerPortrait
            };

            if (targetImage == null)
            {
                yield break;
            }

            targetImage.sprite = sprite;
            targetImage.gameObject.SetActive(true);

            if (fadeIn && portraitFadeDuration > 0)
            {
                yield return FadeImage(targetImage, 0f, 1f, portraitFadeDuration);
            }
            else
            {
                targetImage.color = Color.white;
            }
        }

        /// <summary>
        /// Clear all portrait images
        /// </summary>
        public void ClearPortraits()
        {
            if (leftPortrait != null)
            {
                leftPortrait.gameObject.SetActive(false);
                leftPortrait.sprite = null;
            }

            if (centerPortrait != null)
            {
                centerPortrait.gameObject.SetActive(false);
                centerPortrait.sprite = null;
            }

            if (rightPortrait != null)
            {
                rightPortrait.gameObject.SetActive(false);
                rightPortrait.sprite = null;
            }
        }

        /// <summary>
        /// Handle advance input (click or key press)
        /// </summary>
        public void OnAdvanceInput()
        {
            if (typewriter != null && typewriter.IsTyping)
            {
                // Skip to end of current text
                typewriter.Skip();
            }
            else if (isWaitingForInput)
            {
                // Advance to next line
                inputReceived = true;
            }
        }

        /// <summary>
        /// Called by UI Button click
        /// </summary>
        public void OnClickAdvance()
        {
            OnAdvanceInput();
        }

        private IEnumerator FadeWindow(float targetAlpha, float duration)
        {
            if (windowRoot == null) yield break;

            float startAlpha = windowRoot.alpha;
            float elapsed = 0f;

            // Enable interaction if fading in
            if (targetAlpha > 0.5f)
            {
                windowRoot.interactable = true;
                windowRoot.blocksRaycasts = true;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                windowRoot.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            windowRoot.alpha = targetAlpha;

            // Disable interaction if faded out
            if (targetAlpha < 0.5f)
            {
                windowRoot.interactable = false;
                windowRoot.blocksRaycasts = false;
            }
        }

        private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
        {
            if (image == null) yield break;

            Color color = image.color;
            color.a = startAlpha;
            image.color = color;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                image.color = color;
                yield return null;
            }

            color.a = endAlpha;
            image.color = color;
        }
    }
}
