using System;
using UnityEngine;
using UnityEngine.UI;
using RPGDefete.Story.UI;

namespace RPGDefete.Story
{
    /// <summary>
    /// Singleton manager for story playback
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        private static StoryManager instance;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static StoryManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<StoryManager>();
                    if (instance == null)
                    {
                        Debug.LogWarning("[StoryManager] No instance found in scene");
                    }
                }
                return instance;
            }
        }

        [Header("References")]
        [SerializeField]
        private StoryPlayer storyPlayer;

        [SerializeField]
        private MessageWindow messageWindow;

        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private AudioSource bgmSource;

        [SerializeField]
        private AudioSource seSource;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Pause game time during story playback")]
        private bool pauseGameDuringStory = true;

        [SerializeField]
        [Tooltip("Hide cursor during story playback")]
        private bool hideCursorDuringStory = false;

        /// <summary>
        /// Event fired when a story starts
        /// </summary>
        public event Action<string> OnStoryStarted;

        /// <summary>
        /// Event fired when a story ends
        /// </summary>
        public event Action<string, string> OnStoryEnded;

        /// <summary>
        /// Whether a story is currently playing
        /// </summary>
        public bool IsStoryPlaying => storyPlayer != null && storyPlayer.IsPlaying;

        /// <summary>
        /// Current story being played
        /// </summary>
        public StoryData CurrentStory => storyPlayer?.CurrentStory;

        private float previousTimeScale;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;

        private void Awake()
        {
            // Singleton setup
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Debug.LogWarning("[StoryManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            // Create StoryPlayer if not assigned
            if (storyPlayer == null)
            {
                storyPlayer = GetComponent<StoryPlayer>();
                if (storyPlayer == null)
                {
                    storyPlayer = gameObject.AddComponent<StoryPlayer>();
                }
            }

            // Setup references
            SetupStoryPlayer();

            // Subscribe to events
            storyPlayer.OnStoryStarted += HandleStoryStarted;
            storyPlayer.OnStoryEnded += HandleStoryEnded;
        }

        private void SetupStoryPlayer()
        {
            if (messageWindow != null)
            {
                storyPlayer.SetMessageWindow(messageWindow);
            }

            if (backgroundImage != null)
            {
                storyPlayer.SetBackgroundImage(backgroundImage);
            }

            // Create audio sources if not assigned
            if (bgmSource == null)
            {
                bgmSource = CreateAudioSource("BGM", true);
            }

            if (seSource == null)
            {
                seSource = CreateAudioSource("SE", false);
            }

            storyPlayer.SetBgmSource(bgmSource);
            storyPlayer.SetSeSource(seSource);
        }

        private AudioSource CreateAudioSource(string name, bool loop)
        {
            var go = new GameObject($"StoryAudio_{name}");
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
        }

        /// <summary>
        /// Play a story by StoryData asset
        /// </summary>
        public void PlayStory(StoryData story)
        {
            if (story == null)
            {
                Debug.LogError("[StoryManager] Cannot play null story");
                return;
            }

            storyPlayer.Play(story);
        }

        /// <summary>
        /// Play a story by ID (loads from Resources/Story/Data/)
        /// </summary>
        public void PlayStory(string storyId)
        {
            var story = StoryResourceLoader.LoadStoryData(storyId);
            if (story == null)
            {
                Debug.LogError($"[StoryManager] Story not found: {storyId}");
                return;
            }

            PlayStory(story);
        }

        /// <summary>
        /// Stop the current story
        /// </summary>
        public void StopStory()
        {
            if (storyPlayer != null)
            {
                storyPlayer.Stop();
            }

            RestoreGameState();
        }

        /// <summary>
        /// Pause the current story
        /// </summary>
        public void PauseStory()
        {
            if (storyPlayer != null)
            {
                storyPlayer.Pause();
            }
        }

        /// <summary>
        /// Resume the current story
        /// </summary>
        public void ResumeStory()
        {
            if (storyPlayer != null)
            {
                storyPlayer.Resume();
            }
        }

        /// <summary>
        /// Skip the current dialogue
        /// </summary>
        public void SkipDialogue()
        {
            if (storyPlayer != null)
            {
                storyPlayer.SkipCurrent();
            }
        }

        /// <summary>
        /// Set the message window reference
        /// </summary>
        public void SetMessageWindow(MessageWindow window)
        {
            messageWindow = window;
            if (storyPlayer != null)
            {
                storyPlayer.SetMessageWindow(window);
            }
        }

        private void HandleStoryStarted(string storyId)
        {
            // Pause game if enabled
            if (pauseGameDuringStory)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            // Handle cursor
            if (hideCursorDuringStory)
            {
                previousCursorLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            OnStoryStarted?.Invoke(storyId);
        }

        private void HandleStoryEnded(string storyId, string returnTo)
        {
            RestoreGameState();
            OnStoryEnded?.Invoke(storyId, returnTo);
        }

        private void RestoreGameState()
        {
            // Restore time scale
            if (pauseGameDuringStory)
            {
                Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;
            }

            // Restore cursor
            if (hideCursorDuringStory)
            {
                Cursor.lockState = previousCursorLockMode;
                Cursor.visible = previousCursorVisible;
            }
        }

        private void OnDestroy()
        {
            if (storyPlayer != null)
            {
                storyPlayer.OnStoryStarted -= HandleStoryStarted;
                storyPlayer.OnStoryEnded -= HandleStoryEnded;
            }

            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
