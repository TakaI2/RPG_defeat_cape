using System.Collections;
using UnityEngine;
using RPGDefete.Character;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// IK一括制御コマンド
    /// 全IKを有効化/無効化する（歩行時など）
    /// </summary>
    public class IKControlCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[IKControlCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetIKController(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[IKControlCommand] IK controller not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!controller.IsValid)
            {
                Debug.LogWarning($"[IKControlCommand] IK controller is not valid for character: {data.targetCharacter}");
                yield break;
            }

            if (data.ikEnabled)
            {
                // 全IKを有効化
                yield return controller.EnableAllIK(data.ikTransitionDuration);
            }
            else
            {
                // 全IKを無効化
                yield return controller.DisableAllIK(data.ikTransitionDuration);
            }
        }
    }
}
