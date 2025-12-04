using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Command to change the background image with optional fade
    /// </summary>
    public class BgCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (context.BackgroundImage == null)
            {
                Debug.LogWarning("[BgCommand] BackgroundImage is null, skipping");
                yield break;
            }

            // Get background sprite
            Sprite background = null;
            if (!string.IsNullOrEmpty(data.backgroundName))
            {
                if (!context.TryGetBackground(data.backgroundName, out background))
                {
                    // Try to load on demand
                    background = StoryResourceLoader.LoadBackground(data.backgroundName);
                    if (background != null)
                    {
                        context.BackgroundResources[data.backgroundName] = background;
                    }
                }
            }

            if (background == null)
            {
                Debug.LogWarning($"[BgCommand] Background not found: {data.backgroundName}");
                yield break;
            }

            float fadeDuration = data.fadeDuration / 1000f;

            if (fadeDuration > 0)
            {
                yield return FadeBackground(context.BackgroundImage, background, fadeDuration);
            }
            else
            {
                context.BackgroundImage.sprite = background;
                context.BackgroundImage.color = Color.white;
            }
        }

        private IEnumerator FadeBackground(Image image, Sprite newSprite, float duration)
        {
            float halfDuration = duration / 2f;
            float elapsed = 0f;

            // Fade out
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
                image.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            // Change sprite
            image.sprite = newSprite;

            // Fade in
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
                image.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            image.color = Color.white;
        }
    }
}
