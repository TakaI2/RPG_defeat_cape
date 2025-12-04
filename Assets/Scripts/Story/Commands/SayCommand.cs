using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Command to display character dialogue with optional portrait
    /// </summary>
    public class SayCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (context.MessageWindow == null)
            {
                Debug.LogError("[SayCommand] MessageWindow is null");
                yield break;
            }

            // Get portrait sprite if specified
            Sprite portrait = null;
            if (!string.IsNullOrEmpty(data.portrait))
            {
                if (!context.TryGetPortrait(data.portrait, out portrait))
                {
                    // Try to load on demand
                    portrait = StoryResourceLoader.LoadPortrait(data.portrait);
                    if (portrait != null)
                    {
                        context.PortraitResources[data.portrait] = portrait;
                    }
                }
            }

            // Show message window and display dialogue
            context.MessageWindow.Show();
            yield return context.MessageWindow.ShowDialogue(
                data.characterName,
                data.lines,
                portrait,
                data.portraitPosition
            );
        }
    }
}
