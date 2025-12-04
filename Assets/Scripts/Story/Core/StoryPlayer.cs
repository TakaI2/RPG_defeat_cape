using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGDefete.Story.Commands;
using RPGDefete.Story.UI;

namespace RPGDefete.Story
{
    /// <summary>
    /// Story playback engine that executes story commands in sequence
    /// </summary>
    public class StoryPlayer : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private MessageWindow messageWindow;

        [SerializeField]
        private Image backgroundImage;

        [Header("Audio")]
        [SerializeField]
        private AudioSource bgmSource;

        [SerializeField]
        private AudioSource seSource;

        [Header("Settings")]
        [SerializeField]
        private bool preloadResources = true;

        /// <summary>
        /// Event fired when a story starts
        /// </summary>
        public event Action<string> OnStoryStarted;

        /// <summary>
        /// Event fired when a story ends
        /// </summary>
        public event Action<string, string> OnStoryEnded; // storyId, returnTo

        /// <summary>
        /// Event fired when a command is executed
        /// </summary>
        public event Action<StoryCommandData> OnCommandExecuted;

        /// <summary>
        /// Whether a story is currently playing
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Whether playback is paused
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Current story being played
        /// </summary>
        public StoryData CurrentStory => context?.CurrentStory;

        private StoryContext context;
        private int commandIndex;
        private Dictionary<string, IStoryCommand> commandHandlers;
        private Coroutine playbackCoroutine;

        private void Awake()
        {
            InitializeCommandHandlers();
            InitializeContext();
        }

        private void InitializeCommandHandlers()
        {
            commandHandlers = new Dictionary<string, IStoryCommand>
            {
                { "say", new SayCommand() },
                { "bg", new BgCommand() },
                { "bgm.play", new BgmPlayCommand() },
                { "bgm.stop", new BgmStopCommand() },
                { "se.play", new SeCommand() },
                { "wait", new WaitCommand() },
                { "end", new EndCommand() }
            };
        }

        private void InitializeContext()
        {
            context = new StoryContext
            {
                MessageWindow = messageWindow,
                BackgroundImage = backgroundImage,
                BgmSource = bgmSource,
                SeSource = seSource
            };
        }

        /// <summary>
        /// Play a story from the beginning
        /// </summary>
        /// <param name="story">Story data to play</param>
        public void Play(StoryData story)
        {
            if (story == null)
            {
                Debug.LogError("[StoryPlayer] Cannot play null story");
                return;
            }

            // Stop any currently playing story
            if (IsPlaying)
            {
                Stop();
            }

            // Setup context
            context.CurrentStory = story;
            context.ReturnTo = null;
            commandIndex = 0;

            // Preload resources if enabled
            if (preloadResources)
            {
                StoryResourceLoader.PreloadStory(story, context);
            }

            IsPlaying = true;
            IsPaused = false;

            OnStoryStarted?.Invoke(story.StoryId);

            playbackCoroutine = StartCoroutine(ExecuteCommands());
        }

        /// <summary>
        /// Stop the current story immediately
        /// </summary>
        public void Stop()
        {
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }

            IsPlaying = false;
            IsPaused = false;

            // Hide message window
            if (messageWindow != null)
            {
                messageWindow.Hide();
            }

            // Stop BGM
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }

            // Clear resources
            StoryResourceLoader.ClearResources(context);
        }

        /// <summary>
        /// Pause story playback
        /// </summary>
        public void Pause()
        {
            if (IsPlaying && !IsPaused)
            {
                IsPaused = true;
            }
        }

        /// <summary>
        /// Resume paused story playback
        /// </summary>
        public void Resume()
        {
            if (IsPlaying && IsPaused)
            {
                IsPaused = false;
            }
        }

        /// <summary>
        /// Skip the current dialogue/command
        /// </summary>
        public void SkipCurrent()
        {
            if (messageWindow != null)
            {
                messageWindow.OnAdvanceInput();
            }
        }

        private IEnumerator ExecuteCommands()
        {
            var story = context.CurrentStory;

            while (commandIndex < story.CommandCount)
            {
                // Wait while paused
                while (IsPaused)
                {
                    yield return null;
                }

                var cmdData = story.Commands[commandIndex];

                // Execute command
                if (commandHandlers.TryGetValue(cmdData.op, out var handler))
                {
                    yield return handler.Execute(context, cmdData);
                    OnCommandExecuted?.Invoke(cmdData);
                }
                else
                {
                    Debug.LogWarning($"[StoryPlayer] Unknown command: {cmdData.op}");
                }

                // Check for end command
                if (cmdData.op == "end")
                {
                    break;
                }

                commandIndex++;
            }

            // Story completed
            string storyId = story.StoryId;
            string returnTo = context.ReturnTo;

            IsPlaying = false;
            playbackCoroutine = null;

            // Clear resources
            StoryResourceLoader.ClearResources(context);

            OnStoryEnded?.Invoke(storyId, returnTo);
        }

        /// <summary>
        /// Register a custom command handler
        /// </summary>
        public void RegisterCommand(string op, IStoryCommand handler)
        {
            if (string.IsNullOrEmpty(op) || handler == null) return;
            commandHandlers[op] = handler;
        }

        /// <summary>
        /// Set the message window reference
        /// </summary>
        public void SetMessageWindow(MessageWindow window)
        {
            messageWindow = window;
            if (context != null)
            {
                context.MessageWindow = window;
            }
        }

        /// <summary>
        /// Set the background image reference
        /// </summary>
        public void SetBackgroundImage(Image image)
        {
            backgroundImage = image;
            if (context != null)
            {
                context.BackgroundImage = image;
            }
        }

        /// <summary>
        /// Set the BGM audio source
        /// </summary>
        public void SetBgmSource(AudioSource source)
        {
            bgmSource = source;
            if (context != null)
            {
                context.BgmSource = source;
            }
        }

        /// <summary>
        /// Set the SE audio source
        /// </summary>
        public void SetSeSource(AudioSource source)
        {
            seSource = source;
            if (context != null)
            {
                context.SeSource = source;
            }
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
