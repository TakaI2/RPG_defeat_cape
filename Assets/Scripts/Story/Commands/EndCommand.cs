using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Command to end the story and optionally specify return destination
    /// </summary>
    public class EndCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            // Store return destination in context
            context.ReturnTo = data.returnTo;

            // Hide message window
            if (context.MessageWindow != null)
            {
                context.MessageWindow.Hide();
            }

            // End command completes immediately
            yield break;
        }
    }
}
