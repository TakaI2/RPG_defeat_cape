using System.Collections;
using UnityEngine;
using RPGDefete.Character;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// 腰IKコマンド
    /// キャラクターの腰を指定位置に配置（腰かけるなど）
    /// </summary>
    public class HipIKCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[HipIKCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetIKController(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[HipIKCommand] IK controller not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!controller.IsValid)
            {
                Debug.LogWarning($"[HipIKCommand] IK controller is not valid for character: {data.targetCharacter}");
                yield break;
            }

            // "none" の場合はHip IKを解除
            if (string.IsNullOrEmpty(data.hipIKTarget) || data.hipIKTarget.ToLower() == "none")
            {
                yield return controller.ClearHipIK(data.hipIKDuration);
                yield break;
            }

            // ターゲットを解決
            Transform target = null;

            // IKTargetsから検索
            if (!context.TryGetIKTarget(data.hipIKTarget, out target))
            {
                // MovePointsからも検索
                context.TryGetMovePoint(data.hipIKTarget, out target);
            }

            if (target == null)
            {
                Debug.LogWarning($"[HipIKCommand] Target not found: {data.hipIKTarget}");
                yield break;
            }

            // Hip IKターゲットを設定
            controller.SetHipIKTarget(target);

            // Weight遷移
            yield return controller.SetHipIKWeight(data.hipIKWeight, data.hipIKDuration);
        }
    }
}
