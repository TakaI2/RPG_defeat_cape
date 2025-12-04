using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Command to wait for a specified duration
    /// </summary>
    public class WaitCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            float duration = data.waitDuration / 1000f;

            if (duration > 0)
            {
                // Use unscaled time to work during pause
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }
    }
}
