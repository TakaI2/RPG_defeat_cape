using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGDefete.Story
{
    /// <summary>
    /// Position of character portrait on screen
    /// </summary>
    public enum PortraitPosition
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Serializable data structure for a single story command
    /// </summary>
    [Serializable]
    public class StoryCommandData
    {
        /// <summary>
        /// Command operation type: say, bg, bgm.play, bgm.stop, se.play, wait, end
        /// </summary>
        public string op;

        // ===== Say command parameters =====
        /// <summary>Character name displayed in dialogue box</summary>
        public string characterName;
        /// <summary>Portrait resource name (without path)</summary>
        public string portrait;
        /// <summary>Portrait position on screen</summary>
        public PortraitPosition portraitPosition = PortraitPosition.Center;
        /// <summary>Portrait scale multiplier</summary>
        public float portraitScale = 1f;
        /// <summary>Dialogue lines to display</summary>
        public List<string> lines = new List<string>();

        // ===== Bg command parameters =====
        /// <summary>Background resource name (without path)</summary>
        public string backgroundName;
        /// <summary>Fade duration in milliseconds</summary>
        public int fadeDuration;

        // ===== Bgm command parameters =====
        /// <summary>BGM resource name (without path)</summary>
        public string bgmName;
        /// <summary>Whether to loop the BGM</summary>
        public bool loop = true;
        /// <summary>BGM volume (0-1)</summary>
        [Range(0f, 1f)]
        public float volume = 1f;
        /// <summary>Fade in duration in milliseconds</summary>
        public int fadeInDuration;
        /// <summary>Fade out duration in milliseconds</summary>
        public int fadeOutDuration;

        // ===== Se command parameters =====
        /// <summary>SE resource name (without path)</summary>
        public string seName;
        /// <summary>SE volume (0-1)</summary>
        [Range(0f, 1f)]
        public float seVolume = 1f;

        // ===== Wait command parameters =====
        /// <summary>Wait duration in milliseconds</summary>
        public int waitDuration;

        // ===== End command parameters =====
        /// <summary>Scene or state to return to after story ends</summary>
        public string returnTo;

        /// <summary>
        /// Create a deep copy of this command data
        /// </summary>
        public StoryCommandData Clone()
        {
            var clone = new StoryCommandData
            {
                op = op,
                characterName = characterName,
                portrait = portrait,
                portraitPosition = portraitPosition,
                portraitScale = portraitScale,
                lines = new List<string>(lines),
                backgroundName = backgroundName,
                fadeDuration = fadeDuration,
                bgmName = bgmName,
                loop = loop,
                volume = volume,
                fadeInDuration = fadeInDuration,
                fadeOutDuration = fadeOutDuration,
                seName = seName,
                seVolume = seVolume,
                waitDuration = waitDuration,
                returnTo = returnTo
            };
            return clone;
        }
    }
}
