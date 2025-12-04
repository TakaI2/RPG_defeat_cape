using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Command to play background music with optional fade in
    /// </summary>
    public class BgmPlayCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (context.BgmSource == null)
            {
                Debug.LogWarning("[BgmPlayCommand] BgmSource is null, skipping");
                yield break;
            }

            // Get BGM clip
            AudioClip clip = null;
            if (!string.IsNullOrEmpty(data.bgmName))
            {
                if (!context.TryGetBgm(data.bgmName, out clip))
                {
                    // Try to load on demand
                    clip = StoryResourceLoader.LoadBgm(data.bgmName);
                    if (clip != null)
                    {
                        context.BgmResources[data.bgmName] = clip;
                    }
                }
            }

            if (clip == null)
            {
                Debug.LogWarning($"[BgmPlayCommand] BGM not found: {data.bgmName}");
                yield break;
            }

            context.BgmSource.clip = clip;
            context.BgmSource.loop = data.loop;

            float fadeInDuration = data.fadeInDuration / 1000f;
            float targetVolume = data.volume;

            if (fadeInDuration > 0)
            {
                context.BgmSource.volume = 0f;
                context.BgmSource.Play();
                yield return FadeVolume(context.BgmSource, targetVolume, fadeInDuration);
            }
            else
            {
                context.BgmSource.volume = targetVolume;
                context.BgmSource.Play();
            }
        }

        private IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }
    }

    /// <summary>
    /// Command to stop background music with optional fade out
    /// </summary>
    public class BgmStopCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (context.BgmSource == null || !context.BgmSource.isPlaying)
            {
                yield break;
            }

            float fadeOutDuration = data.fadeOutDuration / 1000f;

            if (fadeOutDuration > 0)
            {
                yield return FadeOutAndStop(context.BgmSource, fadeOutDuration);
            }
            else
            {
                context.BgmSource.Stop();
            }
        }

        private IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume; // Restore original volume for next play
        }
    }
}
