using System.Collections.Generic;
using UnityEngine;

namespace RPGDefete.Story
{
    /// <summary>
    /// Utility class for loading story resources from Resources folder
    /// </summary>
    public static class StoryResourceLoader
    {
        private const string PortraitPath = "Story/Portraits/";
        private const string BackgroundPath = "Story/Backgrounds/";
        private const string BgmPath = "Story/BGM/";
        private const string SePath = "Story/SE/";
        private const string StoryDataPath = "Story/Data/";

        /// <summary>
        /// Load a portrait sprite by name
        /// </summary>
        public static Sprite LoadPortrait(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Resources.Load<Sprite>(PortraitPath + name);
        }

        /// <summary>
        /// Load a background sprite by name
        /// </summary>
        public static Sprite LoadBackground(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Resources.Load<Sprite>(BackgroundPath + name);
        }

        /// <summary>
        /// Load a BGM audio clip by name
        /// </summary>
        public static AudioClip LoadBgm(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Resources.Load<AudioClip>(BgmPath + name);
        }

        /// <summary>
        /// Load a sound effect audio clip by name
        /// </summary>
        public static AudioClip LoadSe(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Resources.Load<AudioClip>(SePath + name);
        }

        /// <summary>
        /// Load a story data asset by ID
        /// </summary>
        public static StoryData LoadStoryData(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return null;
            return Resources.Load<StoryData>(StoryDataPath + storyId);
        }

        /// <summary>
        /// Preload all resources referenced in a story into the context
        /// </summary>
        /// <param name="story">Story to preload resources for</param>
        /// <param name="context">Context to store loaded resources</param>
        public static void PreloadStory(StoryData story, StoryContext context)
        {
            if (story == null || context == null) return;

            foreach (var cmd in story.Commands)
            {
                switch (cmd.op)
                {
                    case "say":
                        if (!string.IsNullOrEmpty(cmd.portrait) &&
                            !context.PortraitResources.ContainsKey(cmd.portrait))
                        {
                            var sprite = LoadPortrait(cmd.portrait);
                            if (sprite != null)
                            {
                                context.PortraitResources[cmd.portrait] = sprite;
                            }
                            else
                            {
                                Debug.LogWarning($"[StoryResourceLoader] Portrait not found: {cmd.portrait}");
                            }
                        }
                        break;

                    case "bg":
                        if (!string.IsNullOrEmpty(cmd.backgroundName) &&
                            !context.BackgroundResources.ContainsKey(cmd.backgroundName))
                        {
                            var sprite = LoadBackground(cmd.backgroundName);
                            if (sprite != null)
                            {
                                context.BackgroundResources[cmd.backgroundName] = sprite;
                            }
                            else
                            {
                                Debug.LogWarning($"[StoryResourceLoader] Background not found: {cmd.backgroundName}");
                            }
                        }
                        break;

                    case "bgm.play":
                        if (!string.IsNullOrEmpty(cmd.bgmName) &&
                            !context.BgmResources.ContainsKey(cmd.bgmName))
                        {
                            var clip = LoadBgm(cmd.bgmName);
                            if (clip != null)
                            {
                                context.BgmResources[cmd.bgmName] = clip;
                            }
                            else
                            {
                                Debug.LogWarning($"[StoryResourceLoader] BGM not found: {cmd.bgmName}");
                            }
                        }
                        break;

                    case "se.play":
                        if (!string.IsNullOrEmpty(cmd.seName) &&
                            !context.SeResources.ContainsKey(cmd.seName))
                        {
                            var clip = LoadSe(cmd.seName);
                            if (clip != null)
                            {
                                context.SeResources[cmd.seName] = clip;
                            }
                            else
                            {
                                Debug.LogWarning($"[StoryResourceLoader] SE not found: {cmd.seName}");
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Clear all cached resources from context
        /// </summary>
        public static void ClearResources(StoryContext context)
        {
            if (context == null) return;

            context.PortraitResources.Clear();
            context.BackgroundResources.Clear();
            context.BgmResources.Clear();
            context.SeResources.Clear();
        }
    }
}
