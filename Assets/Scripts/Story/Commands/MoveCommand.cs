using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// 移動コマンド
    /// キャラクターをNavMeshで指定ポイントへ移動させる
    /// </summary>
    public class MoveCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[MoveCommand] targetCharacter is not specified");
                yield break;
            }

            if (!context.TryGetNavigator(data.targetCharacter, out var navigator))
            {
                Debug.LogWarning($"[MoveCommand] Navigator not found for character: {data.targetCharacter}");
                yield break;
            }

            if (!navigator.IsValid)
            {
                Debug.LogWarning($"[MoveCommand] Navigator is not valid for character: {data.targetCharacter}");
                yield break;
            }

            if (string.IsNullOrEmpty(data.moveTargetPoint))
            {
                Debug.LogWarning("[MoveCommand] moveTargetPoint is not specified");
                yield break;
            }

            // 移動ポイントの解決
            // まずStoryContextのMovePointsを確認、なければNavigatorのローカルポイントを使用
            Vector3 targetPosition;
            if (context.TryGetMovePoint(data.moveTargetPoint, out var movePoint))
            {
                targetPosition = movePoint.position;
            }
            else if (navigator.HasMovePoint(data.moveTargetPoint))
            {
                // NavigatorのMoveToPointを使用
                navigator.MoveToPoint(data.moveTargetPoint, data.moveSpeed);

                if (data.waitForArrival)
                {
                    yield return navigator.WaitForArrival();
                }
                yield break;
            }
            else
            {
                Debug.LogWarning($"[MoveCommand] Move point not found: {data.moveTargetPoint}");
                yield break;
            }

            // 座標への移動
            navigator.MoveTo(targetPosition, data.moveSpeed);

            // 到着待機
            if (data.waitForArrival)
            {
                yield return navigator.WaitForArrival();
            }
        }
    }
}
