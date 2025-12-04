using UnityEngine;

namespace RPGDefete.Story
{
    /// <summary>
    /// Simple test script to trigger story playback
    /// </summary>
    public class StoryTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField]
        private StoryData testStory;

        [SerializeField]
        private KeyCode playKey = KeyCode.T;

        [SerializeField]
        private KeyCode stopKey = KeyCode.Y;

        [Header("Runtime Info")]
        [SerializeField]
        private bool isPlaying;

        private void Update()
        {
            if (Input.GetKeyDown(playKey))
            {
                PlayTestStory();
            }

            if (Input.GetKeyDown(stopKey))
            {
                StopStory();
            }

            // Update runtime info
            if (StoryManager.Instance != null)
            {
                isPlaying = StoryManager.Instance.IsStoryPlaying;
            }
        }

        public void PlayTestStory()
        {
            if (testStory == null)
            {
                Debug.LogWarning("[StoryTester] No test story assigned");
                return;
            }

            if (StoryManager.Instance == null)
            {
                Debug.LogError("[StoryTester] StoryManager not found");
                return;
            }

            Debug.Log($"[StoryTester] Playing story: {testStory.StoryId}");
            StoryManager.Instance.PlayStory(testStory);
        }

        public void StopStory()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.StopStory();
                Debug.Log("[StoryTester] Story stopped");
            }
        }
    }
}
