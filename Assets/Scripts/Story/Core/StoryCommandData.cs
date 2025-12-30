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

        // ===== Expression command parameters =====
        /// <summary>Target character name for expression command</summary>
        public string targetCharacter;
        /// <summary>Expression name (happy, angry, sad, surprised, relaxed, neutral, etc.)</summary>
        public string expressionName;
        /// <summary>Expression weight/intensity (0-1)</summary>
        [Range(0f, 1f)]
        public float expressionWeight = 1f;
        /// <summary>Transition duration in milliseconds (default: 300ms)</summary>
        public int expressionDuration = 300;
        /// <summary>Whether to wait for the expression transition to complete</summary>
        public bool waitForCompletion = true;

        // ===== Pose command parameters =====
        /// <summary>Animation trigger name to play</summary>
        public string animationTrigger;
        /// <summary>Animation state name for CrossFade</summary>
        public string animationState;
        /// <summary>CrossFade duration in seconds (default: 0.25s)</summary>
        public float animationFadeTime = 0.25f;
        /// <summary>Whether to wait for the animation to complete</summary>
        public bool waitForAnimation = false;

        // ===== Move command parameters =====
        /// <summary>Target move point name</summary>
        public string moveTargetPoint;
        /// <summary>Move speed (default: 3.5)</summary>
        public float moveSpeed = 3.5f;
        /// <summary>Whether to wait for arrival</summary>
        public bool waitForArrival = true;

        // ===== LookAt command parameters =====
        /// <summary>LookAt target name or "none" to disable</summary>
        public string lookAtTarget;
        /// <summary>LookAt weight (0-1)</summary>
        [Range(0f, 1f)]
        public float lookAtWeight = 1f;
        /// <summary>LookAt transition duration in seconds</summary>
        public float lookAtDuration = 0.3f;

        // ===== Hand IK command parameters =====
        /// <summary>Hand IK target name or "none" to disable</summary>
        public string handIKTarget;
        /// <summary>Hand type: "left" or "right"</summary>
        public string handType = "right";
        /// <summary>Hand IK weight (0-1)</summary>
        [Range(0f, 1f)]
        public float handIKWeight = 1f;
        /// <summary>Hand IK transition duration in seconds</summary>
        public float handIKDuration = 0.3f;

        // ===== Foot IK command parameters =====
        /// <summary>Foot IK target name or "none" to disable</summary>
        public string footIKTarget;
        /// <summary>Foot type: "left" or "right"</summary>
        public string footType = "right";
        /// <summary>Foot IK weight (0-1)</summary>
        [Range(0f, 1f)]
        public float footIKWeight = 1f;
        /// <summary>Foot IK transition duration in seconds</summary>
        public float footIKDuration = 0.3f;

        // ===== Hip IK command parameters =====
        /// <summary>Hip IK target name or "none" to disable</summary>
        public string hipIKTarget;
        /// <summary>Hip IK weight (0-1)</summary>
        [Range(0f, 1f)]
        public float hipIKWeight = 1f;
        /// <summary>Hip IK transition duration in seconds</summary>
        public float hipIKDuration = 0.3f;

        // ===== IK Control command parameters =====
        /// <summary>Enable or disable all IK</summary>
        public bool ikEnabled = true;
        /// <summary>IK control transition duration in seconds</summary>
        public float ikTransitionDuration = 0.2f;

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
                returnTo = returnTo,
                // Expression command
                targetCharacter = targetCharacter,
                expressionName = expressionName,
                expressionWeight = expressionWeight,
                expressionDuration = expressionDuration,
                waitForCompletion = waitForCompletion,
                // Pose command
                animationTrigger = animationTrigger,
                animationState = animationState,
                animationFadeTime = animationFadeTime,
                waitForAnimation = waitForAnimation,
                // Move command
                moveTargetPoint = moveTargetPoint,
                moveSpeed = moveSpeed,
                waitForArrival = waitForArrival,
                // LookAt command
                lookAtTarget = lookAtTarget,
                lookAtWeight = lookAtWeight,
                lookAtDuration = lookAtDuration,
                // Hand IK command
                handIKTarget = handIKTarget,
                handType = handType,
                handIKWeight = handIKWeight,
                handIKDuration = handIKDuration,
                // Foot IK command
                footIKTarget = footIKTarget,
                footType = footType,
                footIKWeight = footIKWeight,
                footIKDuration = footIKDuration,
                // Hip IK command
                hipIKTarget = hipIKTarget,
                hipIKWeight = hipIKWeight,
                hipIKDuration = hipIKDuration,
                // IK Control command
                ikEnabled = ikEnabled,
                ikTransitionDuration = ikTransitionDuration
            };
            return clone;
        }
    }
}
