using System.Collections;
using UnityEngine;
using RPGDefete.Character;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// LookAtコマンド
    /// キャラクターの視線をターゲットに向ける
    /// </summary>
    public class LookAtCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[LookAtCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetIKController(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[LookAtCommand] IK controller not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!controller.IsValid)
            {
                Debug.LogWarning($"[LookAtCommand] IK controller is not valid for character: {data.targetCharacter}");
                yield break;
            }

            // "none" の場合はLookAtを解除
            if (string.IsNullOrEmpty(data.lookAtTarget) || data.lookAtTarget.ToLower() == "none")
            {
                yield return controller.ClearLookAt(data.lookAtDuration);
                yield break;
            }

            // ターゲットを解決
            Transform target = null;

            // まずIKTargetsから検索
            if (!context.TryGetIKTarget(data.lookAtTarget, out target))
            {
                // MovePointsからも検索
                if (!context.TryGetMovePoint(data.lookAtTarget, out target))
                {
                    // 他のキャラクターのTransformを検索
                    if (context.TryGetCharacter(data.lookAtTarget, out var characterController))
                    {
                        target = characterController.transform;
                    }
                }
            }

            if (target == null)
            {
                Debug.LogWarning($"[LookAtCommand] Target not found: {data.lookAtTarget}");
                yield break;
            }

            // LookAtターゲットを設定
            controller.SetLookAtTarget(target);

            // Weight遷移
            yield return controller.SetLookAtWeight(data.lookAtWeight, data.lookAtDuration);
        }
    }
}
