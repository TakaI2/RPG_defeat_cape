using System.Collections;
using UnityEngine;
using RPGDefete.Character;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// 手IKコマンド
    /// キャラクターの手を指定位置に伸ばす
    /// </summary>
    public class HandIKCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[HandIKCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetIKController(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[HandIKCommand] IK controller not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!controller.IsValid)
            {
                Debug.LogWarning($"[HandIKCommand] IK controller is not valid for character: {data.targetCharacter}");
                yield break;
            }

            // HandTypeを解決
            HandType handType = data.handType?.ToLower() == "left" ? HandType.Left : HandType.Right;

            // "none" の場合はHand IKを解除
            if (string.IsNullOrEmpty(data.handIKTarget) || data.handIKTarget.ToLower() == "none")
            {
                yield return controller.ClearHandIK(handType, data.handIKDuration);
                yield break;
            }

            // ターゲットを解決
            Transform target = null;

            // IKTargetsから検索
            if (!context.TryGetIKTarget(data.handIKTarget, out target))
            {
                // MovePointsからも検索
                context.TryGetMovePoint(data.handIKTarget, out target);
            }

            if (target == null)
            {
                Debug.LogWarning($"[HandIKCommand] Target not found: {data.handIKTarget}");
                yield break;
            }

            // Hand IKターゲットを設定
            controller.SetHandIKTarget(handType, target);

            // Weight遷移
            yield return controller.SetHandIKWeight(handType, data.handIKWeight, data.handIKDuration);
        }
    }
}
