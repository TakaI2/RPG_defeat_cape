using System.Collections;
using UnityEngine;
using RPGDefete.Character;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// 足IKコマンド
    /// キャラクターの足を指定位置に配置（何かを踏むなど）
    /// </summary>
    public class FootIKCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[FootIKCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetIKController(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[FootIKCommand] IK controller not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!controller.IsValid)
            {
                Debug.LogWarning($"[FootIKCommand] IK controller is not valid for character: {data.targetCharacter}");
                yield break;
            }

            // FootTypeを解決
            FootType footType = data.footType?.ToLower() == "left" ? FootType.Left : FootType.Right;

            // "none" の場合はFoot IKを解除
            if (string.IsNullOrEmpty(data.footIKTarget) || data.footIKTarget.ToLower() == "none")
            {
                yield return controller.ClearFootIK(footType, data.footIKDuration);
                yield break;
            }

            // ターゲットを解決
            Transform target = null;

            // IKTargetsから検索
            if (!context.TryGetIKTarget(data.footIKTarget, out target))
            {
                // MovePointsからも検索
                context.TryGetMovePoint(data.footIKTarget, out target);
            }

            if (target == null)
            {
                Debug.LogWarning($"[FootIKCommand] Target not found: {data.footIKTarget}");
                yield break;
            }

            // Foot IKターゲットを設定
            controller.SetFootIKTarget(footType, target);

            // Weight遷移
            yield return controller.SetFootIKWeight(footType, data.footIKWeight, data.footIKDuration);
        }
    }
}
