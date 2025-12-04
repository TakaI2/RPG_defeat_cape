using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGDefete.Story.UI;

namespace RPGDefete.Story
{
    /// <summary>
    /// Runtime context for story execution, holding references to UI and resources
    /// </summary>
    public class StoryContext
    {
        /// <summary>
        /// Reference to the message window UI
        /// </summary>
        public MessageWindow MessageWindow { get; set; }

        /// <summary>
        /// Reference to the background image UI element
        /// </summary>
        public Image BackgroundImage { get; set; }

        /// <summary>
        /// AudioSource for playing background music
        /// </summary>
        public AudioSource BgmSource { get; set; }

        /// <summary>
        /// AudioSource for playing sound effects
        /// </summary>
        public AudioSource SeSource { get; set; }

        /// <summary>
        /// Cached portrait sprites, keyed by resource name
        /// </summary>
        public Dictionary<string, Sprite> PortraitResources { get; } = new Dictionary<string, Sprite>();

        /// <summary>
        /// Cached background sprites, keyed by resource name
        /// </summary>
        public Dictionary<string, Sprite> BackgroundResources { get; } = new Dictionary<string, Sprite>();

        /// <summary>
        /// Cached BGM audio clips, keyed by resource name
        /// </summary>
        public Dictionary<string, AudioClip> BgmResources { get; } = new Dictionary<string, AudioClip>();

        /// <summary>
        /// Cached SE audio clips, keyed by resource name
        /// </summary>
        public Dictionary<string, AudioClip> SeResources { get; } = new Dictionary<string, AudioClip>();

        /// <summary>
        /// The currently playing story data
        /// </summary>
        public StoryData CurrentStory { get; set; }

        /// <summary>
        /// Return value from EndCommand (where to return after story ends)
        /// </summary>
        public string ReturnTo { get; set; }

        /// <summary>
        /// Check if all required references are set
        /// </summary>
        public bool IsValid =>
            MessageWindow != null &&
            BgmSource != null &&
            SeSource != null;

        /// <summary>
        /// Clear all cached resources
        /// </summary>
        public void ClearResources()
        {
            PortraitResources.Clear();
            BackgroundResources.Clear();
            BgmResources.Clear();
            SeResources.Clear();
        }

        /// <summary>
        /// Try to get a portrait sprite from cache
        /// </summary>
        public bool TryGetPortrait(string name, out Sprite sprite)
        {
            if (string.IsNullOrEmpty(name))
            {
                sprite = null;
                return false;
            }
            return PortraitResources.TryGetValue(name, out sprite);
        }

        /// <summary>
        /// Try to get a background sprite from cache
        /// </summary>
        public bool TryGetBackground(string name, out Sprite sprite)
        {
            if (string.IsNullOrEmpty(name))
            {
                sprite = null;
                return false;
            }
            return BackgroundResources.TryGetValue(name, out sprite);
        }

        /// <summary>
        /// Try to get a BGM audio clip from cache
        /// </summary>
        public bool TryGetBgm(string name, out AudioClip clip)
        {
            if (string.IsNullOrEmpty(name))
            {
                clip = null;
                return false;
            }
            return BgmResources.TryGetValue(name, out clip);
        }

        /// <summary>
        /// Try to get a SE audio clip from cache
        /// </summary>
        public bool TryGetSe(string name, out AudioClip clip)
        {
            if (string.IsNullOrEmpty(name))
            {
                clip = null;
                return false;
            }
            return SeResources.TryGetValue(name, out clip);
        }
    }
}
