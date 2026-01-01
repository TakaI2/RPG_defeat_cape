using System.Collections;
using UnityEngine;
using RPGDefete.Character;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// 視線制御コマンド（目玉のみ）
    /// VRMキャラクターの視線をターゲットに向ける
    /// 頭IK（FinalIK LookAtIK）と連携して自然な視線を実現
    /// </summary>
    public class EyeLookAtCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[EyeLookAtCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetEyeGazeController(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[EyeLookAtCommand] EyeGaze controller not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!controller.IsValid)
            {
                Debug.LogWarning($"[EyeLookAtCommand] EyeGaze controller is not valid for character: {data.targetCharacter}");
                yield break;
            }

            // "none" の場合は視線を解除
            if (string.IsNullOrEmpty(data.eyeLookAtTarget) || data.eyeLookAtTarget.ToLower() == "none")
            {
                yield return controller.DisableGaze(data.eyeLookAtDuration);
                yield break;
            }

            // ターゲットを解決
            Transform target = null;

            // IKTargetsから検索
            if (!context.TryGetIKTarget(data.eyeLookAtTarget, out target))
            {
                // MovePointsからも検索
                if (!context.TryGetMovePoint(data.eyeLookAtTarget, out target))
                {
                    // シーン内のGameObjectを検索
                    var go = GameObject.Find(data.eyeLookAtTarget);
                    if (go != null)
                        target = go.transform;
                }
            }

            if (target == null)
            {
                Debug.LogWarning($"[EyeLookAtCommand] Target not found: {data.eyeLookAtTarget}");
                yield break;
            }

            // 視線ターゲットを設定
            controller.SetGazeTarget(target);

            // Weight遷移
            yield return controller.SetWeight(data.eyeLookAtWeight, data.eyeLookAtDuration);
        }
    }
}
