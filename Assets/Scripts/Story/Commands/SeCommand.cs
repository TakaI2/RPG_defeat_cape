using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Command to play a sound effect
    /// </summary>
    public class SeCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (context.SeSource == null)
            {
                Debug.LogWarning("[SeCommand] SeSource is null, skipping");
                yield break;
            }

            // Get SE clip
            AudioClip clip = null;
            if (!string.IsNullOrEmpty(data.seName))
            {
                if (!context.TryGetSe(data.seName, out clip))
                {
                    // Try to load on demand
                    clip = StoryResourceLoader.LoadSe(data.seName);
                    if (clip != null)
                    {
                        context.SeResources[data.seName] = clip;
                    }
                }
            }

            if (clip == null)
            {
                Debug.LogWarning($"[SeCommand] SE not found: {data.seName}");
                yield break;
            }

            context.SeSource.PlayOneShot(clip, data.seVolume);

            // SE plays asynchronously, no need to wait
            yield break;
        }
    }
}
