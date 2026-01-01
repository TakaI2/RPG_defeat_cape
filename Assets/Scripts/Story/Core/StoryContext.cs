using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPGDefete.Story.UI;
using RPGDefete.Character;

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
        /// Registered VRM characters, keyed by character name
        /// </summary>
        public Dictionary<string, VRMExpressionController> Characters { get; } = new Dictionary<string, VRMExpressionController>();

        /// <summary>
        /// Registered animation controllers, keyed by character name
        /// </summary>
        public Dictionary<string, VRMAnimationController> AnimationControllers { get; } = new Dictionary<string, VRMAnimationController>();

        /// <summary>
        /// Registered character navigators, keyed by character name
        /// </summary>
        public Dictionary<string, CharacterNavigator> Navigators { get; } = new Dictionary<string, CharacterNavigator>();

        /// <summary>
        /// Registered move points, keyed by point name
        /// </summary>
        public Dictionary<string, Transform> MovePoints { get; } = new Dictionary<string, Transform>();

        /// <summary>
        /// Registered IK controllers, keyed by character name
        /// </summary>
        public Dictionary<string, VRMIKController> IKControllers { get; } = new Dictionary<string, VRMIKController>();

        /// <summary>
        /// Registered FinalIK controllers, keyed by character name
        /// </summary>
        public Dictionary<string, VRMFinalIKController> FinalIKControllers { get; } = new Dictionary<string, VRMFinalIKController>();

        /// <summary>
        /// Registered Eye Gaze controllers, keyed by character name
        /// </summary>
        public Dictionary<string, VRMEyeGazeController> EyeGazeControllers { get; } = new Dictionary<string, VRMEyeGazeController>();

        /// <summary>
        /// Registered IK targets (LookAt, Hand, Foot, Hip targets), keyed by target name
        /// </summary>
        public Dictionary<string, Transform> IKTargets { get; } = new Dictionary<string, Transform>();

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
            // Note: Characters are not cleared as they are scene references
        }

        /// <summary>
        /// Register a VRM character for expression control
        /// </summary>
        /// <param name="name">Character name identifier</param>
        /// <param name="controller">VRMExpressionController component</param>
        public void RegisterCharacter(string name, VRMExpressionController controller)
        {
            if (string.IsNullOrEmpty(name) || controller == null) return;
            Characters[name] = controller;
        }

        /// <summary>
        /// Unregister a VRM character
        /// </summary>
        /// <param name="name">Character name identifier</param>
        public void UnregisterCharacter(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Characters.Remove(name);
        }

        /// <summary>
        /// Try to get a VRM character controller
        /// </summary>
        /// <param name="name">Character name identifier</param>
        /// <param name="controller">Output controller if found</param>
        /// <returns>True if character was found</returns>
        public bool TryGetCharacter(string name, out VRMExpressionController controller)
        {
            if (string.IsNullOrEmpty(name))
            {
                controller = null;
                return false;
            }
            return Characters.TryGetValue(name, out controller);
        }

        /// <summary>
        /// Clear all registered characters
        /// </summary>
        public void ClearCharacters()
        {
            Characters.Clear();
        }

        /// <summary>
        /// Register an animation controller
        /// </summary>
        public void RegisterAnimationController(string name, VRMAnimationController controller)
        {
            if (string.IsNullOrEmpty(name) || controller == null) return;
            AnimationControllers[name] = controller;
        }

        /// <summary>
        /// Unregister an animation controller
        /// </summary>
        public void UnregisterAnimationController(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            AnimationControllers.Remove(name);
        }

        /// <summary>
        /// Try to get an animation controller
        /// </summary>
        public bool TryGetAnimationController(string name, out VRMAnimationController controller)
        {
            if (string.IsNullOrEmpty(name))
            {
                controller = null;
                return false;
            }
            return AnimationControllers.TryGetValue(name, out controller);
        }

        /// <summary>
        /// Register a navigator
        /// </summary>
        public void RegisterNavigator(string name, CharacterNavigator navigator)
        {
            if (string.IsNullOrEmpty(name) || navigator == null) return;
            Navigators[name] = navigator;
        }

        /// <summary>
        /// Unregister a navigator
        /// </summary>
        public void UnregisterNavigator(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Navigators.Remove(name);
        }

        /// <summary>
        /// Try to get a navigator
        /// </summary>
        public bool TryGetNavigator(string name, out CharacterNavigator navigator)
        {
            if (string.IsNullOrEmpty(name))
            {
                navigator = null;
                return false;
            }
            return Navigators.TryGetValue(name, out navigator);
        }

        /// <summary>
        /// Register a move point
        /// </summary>
        public void RegisterMovePoint(string name, Transform point)
        {
            if (string.IsNullOrEmpty(name) || point == null) return;
            MovePoints[name] = point;
        }

        /// <summary>
        /// Unregister a move point
        /// </summary>
        public void UnregisterMovePoint(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            MovePoints.Remove(name);
        }

        /// <summary>
        /// Try to get a move point
        /// </summary>
        public bool TryGetMovePoint(string name, out Transform point)
        {
            if (string.IsNullOrEmpty(name))
            {
                point = null;
                return false;
            }
            return MovePoints.TryGetValue(name, out point);
        }

        /// <summary>
        /// Register an IK controller
        /// </summary>
        public void RegisterIKController(string name, VRMIKController controller)
        {
            if (string.IsNullOrEmpty(name) || controller == null) return;
            IKControllers[name] = controller;
        }

        /// <summary>
        /// Unregister an IK controller
        /// </summary>
        public void UnregisterIKController(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            IKControllers.Remove(name);
        }

        /// <summary>
        /// Try to get an IK controller
        /// </summary>
        public bool TryGetIKController(string name, out VRMIKController controller)
        {
            if (string.IsNullOrEmpty(name))
            {
                controller = null;
                return false;
            }
            return IKControllers.TryGetValue(name, out controller);
        }

        /// <summary>
        /// Register an IK target
        /// </summary>
        public void RegisterIKTarget(string name, Transform target)
        {
            if (string.IsNullOrEmpty(name) || target == null) return;
            IKTargets[name] = target;
        }

        /// <summary>
        /// Unregister an IK target
        /// </summary>
        public void UnregisterIKTarget(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            IKTargets.Remove(name);
        }

        /// <summary>
        /// Try to get an IK target
        /// </summary>
        public bool TryGetIKTarget(string name, out Transform target)
        {
            if (string.IsNullOrEmpty(name))
            {
                target = null;
                return false;
            }
            return IKTargets.TryGetValue(name, out target);
        }

        /// <summary>
        /// Register a FinalIK controller
        /// </summary>
        public void RegisterFinalIKController(string name, VRMFinalIKController controller)
        {
            if (string.IsNullOrEmpty(name) || controller == null) return;
            FinalIKControllers[name] = controller;
        }

        /// <summary>
        /// Unregister a FinalIK controller
        /// </summary>
        public void UnregisterFinalIKController(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            FinalIKControllers.Remove(name);
        }

        /// <summary>
        /// Try to get a FinalIK controller
        /// </summary>
        public bool TryGetFinalIKController(string name, out VRMFinalIKController controller)
        {
            if (string.IsNullOrEmpty(name))
            {
                controller = null;
                return false;
            }
            return FinalIKControllers.TryGetValue(name, out controller);
        }

        /// <summary>
        /// Register an Eye Gaze controller
        /// </summary>
        public void RegisterEyeGazeController(string name, VRMEyeGazeController controller)
        {
            if (string.IsNullOrEmpty(name) || controller == null) return;
            EyeGazeControllers[name] = controller;
        }

        /// <summary>
        /// Unregister an Eye Gaze controller
        /// </summary>
        public void UnregisterEyeGazeController(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            EyeGazeControllers.Remove(name);
        }

        /// <summary>
        /// Try to get an Eye Gaze controller
        /// </summary>
        public bool TryGetEyeGazeController(string name, out VRMEyeGazeController controller)
        {
            if (string.IsNullOrEmpty(name))
            {
                controller = null;
                return false;
            }
            return EyeGazeControllers.TryGetValue(name, out controller);
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
