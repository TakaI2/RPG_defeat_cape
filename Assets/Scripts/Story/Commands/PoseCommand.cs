using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// ポーズ（アニメーション）コマンド
    /// キャラクターのアニメーションを再生する
    /// </summary>
    public class PoseCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[PoseCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetAnimationController(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[PoseCommand] Animation controller not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!controller.IsValid)
            {
                Debug.LogWarning($"[PoseCommand] Animation controller is not valid for character: {data.targetCharacter}");
                yield break;
            }

            // Trigger または State でアニメーション再生
            if (!string.IsNullOrEmpty(data.animationTrigger))
            {
                // Trigger ベースの再生
                controller.PlayAnimation(data.animationTrigger);
            }
            else if (!string.IsNullOrEmpty(data.animationState))
            {
                // State ベースのクロスフェード再生
                controller.CrossFade(data.animationState, data.animationFadeTime);
            }
            else
            {
                Debug.LogWarning("[PoseCommand] Neither animationTrigger nor animationState is specified");
                yield break;
            }

            // アニメーション完了待機
            if (data.waitForAnimation)
            {
                yield return controller.WaitForAnimationComplete();
            }
        }
    }
}
