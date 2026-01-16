using UnityEngine;

namespace RPGDefete.Character
{
    /// <summary>
    /// InteractionPoint間のマッチングを計算するユーティリティ
    /// Actor側のInteractionPointをTarget側のInteractionPointに重ね合わせるための
    /// IKターゲット位置・回転を計算します
    /// </summary>
    public static class InteractionPointMatcher
    {
        /// <summary>
        /// ActorのpointをTargetのpointに重ね合わせるためのワールド変換を計算
        /// </summary>
        /// <param name="actorPoint">Actor側のInteractionPoint（例: Actor.mouth）</param>
        /// <param name="targetPoint">Target側のInteractionPoint（例: Target.mouth）</param>
        /// <returns>IKターゲットの位置と回転</returns>
        public static (Vector3 position, Quaternion rotation) CalculateMatchTransform(
            InteractionPoint actorPoint,
            InteractionPoint targetPoint)
        {
            if (actorPoint == null || targetPoint == null)
            {
                Debug.LogWarning("[InteractionPointMatcher] ActorPoint or TargetPoint is null");
                return (Vector3.zero, Quaternion.identity);
            }

            // Target側のPointのワールド位置・回転
            Vector3 targetWorldPos = targetPoint.GetWorldPosition();
            Quaternion targetWorldRot = targetPoint.GetRotation();

            return (targetWorldPos, targetWorldRot);
        }

        /// <summary>
        /// ActorのpointをTargetのpointに重ね合わせる位置を計算（オフセット付き）
        /// </summary>
        /// <param name="actorPoint">Actor側のInteractionPoint</param>
        /// <param name="targetPoint">Target側のInteractionPoint</param>
        /// <param name="offset">Targetの座標系でのオフセット（例: 5cm手前 = -0.05f * forward）</param>
        /// <returns>IKターゲット位置</returns>
        public static Vector3 CalculateMatchPosition(
            InteractionPoint actorPoint,
            InteractionPoint targetPoint,
            Vector3 offset = default)
        {
            if (actorPoint == null || targetPoint == null)
            {
                Debug.LogWarning("[InteractionPointMatcher] ActorPoint or TargetPoint is null");
                return Vector3.zero;
            }

            Vector3 targetWorldPos = targetPoint.GetWorldPosition();

            // オフセットをTargetの座標系で適用
            if (offset != Vector3.zero)
            {
                Vector3 worldOffset = targetPoint.transform.TransformDirection(offset);
                targetWorldPos += worldOffset;
            }

            return targetWorldPos;
        }

        /// <summary>
        /// ActorのpointをTargetのpointに近づける位置を計算（距離指定）
        /// </summary>
        /// <param name="actorPoint">Actor側のInteractionPoint</param>
        /// <param name="targetPoint">Target側のInteractionPoint</param>
        /// <param name="stopDistance">停止距離（メートル）</param>
        /// <returns>IKターゲット位置</returns>
        public static Vector3 CalculateApproachPosition(
            InteractionPoint actorPoint,
            InteractionPoint targetPoint,
            float stopDistance = 0.05f)
        {
            if (actorPoint == null || targetPoint == null)
            {
                Debug.LogWarning("[InteractionPointMatcher] ActorPoint or TargetPoint is null");
                return Vector3.zero;
            }

            Vector3 targetWorldPos = targetPoint.GetWorldPosition();
            Vector3 targetForward = targetPoint.GetDirection();

            // Targetのforward方向にstopDistance分オフセット（手前で停止）
            Vector3 approachPos = targetWorldPos - targetForward * stopDistance;

            return approachPos;
        }

        /// <summary>
        /// Hand IK用: Actor.handをTarget.targetPointに配置する位置を計算
        /// </summary>
        /// <param name="actorHand">Actor側の手のInteractionPoint</param>
        /// <param name="targetPoint">Target側のInteractionPoint（肩、頭など）</param>
        /// <param name="localOffset">ターゲット座標系でのローカルオフセット</param>
        /// <returns>IKターゲット位置</returns>
        public static Vector3 CalculateHandIKTarget(
            InteractionPoint actorHand,
            InteractionPoint targetPoint,
            Vector3 localOffset = default)
        {
            if (actorHand == null || targetPoint == null)
            {
                Debug.LogWarning("[InteractionPointMatcher] ActorHand or TargetPoint is null");
                return Vector3.zero;
            }

            Vector3 targetPos = targetPoint.GetWorldPosition();

            // ローカルオフセットをワールド座標に変換
            if (localOffset != Vector3.zero)
            {
                targetPos += targetPoint.transform.TransformDirection(localOffset);
            }

            return targetPos;
        }

        /// <summary>
        /// 2つのInteractionPoint間の距離を計算
        /// </summary>
        public static float Distance(InteractionPoint pointA, InteractionPoint pointB)
        {
            if (pointA == null || pointB == null)
            {
                Debug.LogWarning("[InteractionPointMatcher] PointA or PointB is null");
                return float.MaxValue;
            }

            return Vector3.Distance(pointA.GetWorldPosition(), pointB.GetWorldPosition());
        }

        /// <summary>
        /// 2つのInteractionPoint間の方向ベクトルを計算（A → B）
        /// </summary>
        public static Vector3 Direction(InteractionPoint fromPoint, InteractionPoint toPoint)
        {
            if (fromPoint == null || toPoint == null)
            {
                Debug.LogWarning("[InteractionPointMatcher] FromPoint or ToPoint is null");
                return Vector3.forward;
            }

            Vector3 from = fromPoint.GetWorldPosition();
            Vector3 to = toPoint.GetWorldPosition();
            return (to - from).normalized;
        }
    }
}
