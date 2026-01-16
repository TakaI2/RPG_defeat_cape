using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using RPGDefete.Character;

namespace RPG.Action
{
    /// <summary>
    /// キスアクション
    /// ターゲットの姿勢（立ち・座り・寝）や身長差に応じて自動的にアプローチ方法を調整します
    /// </summary>
    public class KissAction : ActionBase
    {
        public override string ActionName => "Kiss";
        public override ActionPriority Priority => ActionPriority.Normal;

        [Header("Kiss設定")]
        [SerializeField] private float approachDuration = 0.5f;
        [SerializeField] private float kissDuration = 5.0f;
        [SerializeField] private float handPlacementDuration = 0.4f;
        [SerializeField] private float faceApproachDuration = 0.5f;

        [Header("タイミング調整")]
        [SerializeField] private float afterApproachWait = 0.2f;
        [SerializeField] private float afterRotateWait = 0.1f;
        [SerializeField] private float beforeHandPlaceWait = 0.3f;
        [SerializeField] private float afterHandPlaceWait = 0.2f;
        [SerializeField] private float beforeFaceApproachWait = 0.3f;
        [SerializeField] private float beforeKissWait = 0.2f;
        [SerializeField] private float afterKissWait = 0.3f;

        [Header("デバッグ")]
        [SerializeField] private bool showDetailedLog = true;
        [SerializeField] private bool showGizmos = true;

        private GameObject _rightHandIKTarget;
        private GameObject _leftHandIKTarget;

        public override bool CanExecute(ActionContext context)
        {
            // 対象がGameCharacterであること
            if (context.Target == null) return false;
            var targetChar = context.Target.GetComponent<GameCharacter>();
            return targetChar != null;
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var actor = context.Actor;
            var target = context.Target.GetComponent<GameCharacter>();

            if (target == null)
            {
                Debug.LogWarning("[KissAction] Target is not a GameCharacter");
                yield break;
            }

            // ターゲットのNavMeshAgentを一時的に無効化（衝突回避を防ぐため）
            NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
            bool wasAgentEnabled = false;
            if (targetAgent != null && targetAgent.enabled)
            {
                wasAgentEnabled = true;
                targetAgent.enabled = false;
                if (showDetailedLog)
                    Debug.Log("[KissAction] Disabled target's NavMeshAgent to prevent collision avoidance");
            }

            try
            {
                // 1. ターゲット状態分析
                var postureDetector = target.GetComponent<PostureDetector>();
                if (postureDetector == null)
                {
                    postureDetector = target.gameObject.AddComponent<PostureDetector>();
                    postureDetector.Initialize();
                }

                CharacterPosture targetPosture = postureDetector.GetCurrentPosture();
                float heightDiff = HeightDifferenceCalculator.CalculateHeightDifference(actor, target);
                var heightCategory = HeightDifferenceCalculator.CategorizeHeightDifference(heightDiff);

                if (showDetailedLog)
                    Debug.Log($"[KissAction] 1. Target analysis - Posture: {targetPosture}, Height diff: {heightDiff:F2}m ({heightCategory})");

                // 2. アプローチ方法決定
                KissApproachType approachType = KissApproachStrategy.DetermineApproach(targetPosture, heightCategory);
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 2. Approach determined - Type: {approachType}");

                // 3. アプローチ位置へ移動
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 3. Moving to approach position...");
                Vector3 approachPos = KissApproachStrategy.CalculateApproachPosition(target, approachType);
                if (context.Navigator != null)
                {
                    context.Navigator.MoveTo(approachPos);
                    yield return new WaitUntil(() => !context.Navigator.IsMoving);
                }
                else
                {
                    actor.transform.position = approachPos;
                }
                yield return new WaitForSeconds(afterApproachWait);

                // 4. 向きを調整（相手を向く）
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 4. Rotating to face target...");
                yield return RotateToFace(actor, target);
                yield return new WaitForSeconds(afterRotateWait);

                // 5. 姿勢調整
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 5. Adjusting posture for {approachType}...");
                yield return AdjustPosture(context, approachType, heightDiff);

                // 6. 手を添える前の待機
                yield return new WaitForSeconds(beforeHandPlaceWait);

                // 7. 手を添える
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 6. Placing hands...");
                yield return PlaceHands(context, approachType, target, targetPosture);
                yield return new WaitForSeconds(afterHandPlaceWait);

                // 8. 視線を合わせる
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 7. Making eye contact...");
                var targetEyePoint = target.GetInteractionPoint(InteractionPointType.Eye);
                if (targetEyePoint != null && context.EyeGazeController != null)
                {
                    context.EyeGazeController.SetGazeTarget(targetEyePoint.transform);
                    context.EyeGazeController.SetWeight(1f);
                }

                // 9. 表情変化（照れ）
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 8. Showing embarrassed expression...");
                context.ExpressionController?.StartPresetTransition("embarrassed", 0.3f);
                if (target.ExpressionController != null)
                    target.ExpressionController.StartPresetTransition("embarrassed", 0.3f);

                yield return new WaitForSeconds(beforeFaceApproachWait);

                // 10. 顔を近づける
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 9. Approaching face...");
                yield return ApproachFace(context, target, approachType);
                yield return new WaitForSeconds(beforeKissWait);

                // 11. 目を閉じる
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 10. Closing eyes...");
                context.ExpressionController?.SetExpression("blink", 0.9f);
                if (target.ExpressionController != null)
                    target.ExpressionController.SetExpression("blink", 0.8f);

                // 12. キス（接触）
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 11. Kiss! {actor.CharacterName} kisses {target.CharacterName}");
                yield return new WaitForSeconds(kissDuration);

                // 13. 離れる
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 12. Retracting face...");
                yield return RetractFace(context);
                yield return new WaitForSeconds(afterKissWait);

                // 14. 目を開ける
                context.ExpressionController?.SetExpression("blink", 0f);
                if (target.ExpressionController != null)
                    target.ExpressionController.SetExpression("blink", 0f);

                // 15. 余韻表情
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 13. Showing afterglow expression...");
                context.ExpressionController?.StartPresetTransition("joy", 0.3f);
                if (target.ExpressionController != null)
                    target.ExpressionController.StartPresetTransition("smile", 0.3f);

                // 16. 手を離す
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 14. Releasing hands...");
                yield return ReleaseHands(context);

                // 17. 姿勢を戻す
                if (showDetailedLog)
                    Debug.Log($"[KissAction] 15. Resetting posture...");
                yield return ResetPosture(context, approachType);

                    // 18. 視線解除
                    if (context.EyeGazeController != null)
                        context.EyeGazeController.SetWeight(0f);

                    yield return new WaitForSeconds(0.5f);

                    if (showDetailedLog)
                        Debug.Log("[KissAction] ✓ Kiss completed successfully!");
            }
            finally
            {
                // ターゲットのNavMeshAgentを再有効化
                if (targetAgent != null && wasAgentEnabled)
                {
                    targetAgent.enabled = true;
                    if (showDetailedLog)
                        Debug.Log("[KissAction] Re-enabled target's NavMeshAgent");
                }
            }
        }

        /// <summary>
        /// 相手を向く
        /// </summary>
        private IEnumerator RotateToFace(GameCharacter actor, GameCharacter target)
        {
            Vector3 direction = (target.transform.position - actor.transform.position).normalized;
            direction.y = 0;

            if (direction == Vector3.zero) yield break;

            Quaternion targetRot = Quaternion.LookRotation(direction);
            Quaternion startRot = actor.transform.rotation;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                actor.transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            actor.transform.rotation = targetRot;
        }

        /// <summary>
        /// 姿勢調整
        /// </summary>
        private IEnumerator AdjustPosture(ActionContext context, KissApproachType approachType, float heightDiff)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            if (showDetailedLog)
                Debug.Log($"[KissAction-AdjustPosture] Adjusting for {approachType}, height diff: {heightDiff:F2}m");

            switch (approachType)
            {
                case KissApproachType.BendDown:
                    // 上体を前に傾ける（身長差に応じて角度調整）
                    float bendAngle = Mathf.Abs(heightDiff) * 80f; // -0.3m差で約24度
                    bendAngle = Mathf.Clamp(bendAngle, 15f, 45f);
                    if (showDetailedLog)
                        Debug.Log($"[KissAction-AdjustPosture] Bending spine to {bendAngle:F1} degrees");
                    yield return ik.BendSpineSmooth(bendAngle, 0.4f);
                    break;

                case KissApproachType.KneelDown:
                    // 膝をつくアニメーション
                    context.AnimationController?.CrossFade("Kneel_Down", 0.2f);
                    yield return new WaitForSeconds(0.8f);
                    break;

                case KissApproachType.LeanOver:
                    // しゃがむ + 覆いかぶさる
                    context.AnimationController?.CrossFade("Kneel_Down", 0.2f);
                    yield return new WaitForSeconds(0.5f);
                    yield return ik.BendSpineSmooth(55f, 0.3f);
                    break;

                case KissApproachType.TipToe:
                    // つま先立ち
                    yield return ik.TipToeSmooth(0.05f, 0.3f);
                    break;

                default:
                    yield return null;
                    break;
            }
        }

        /// <summary>
        /// 手を添える（InteractionPointマッチング使用）
        /// </summary>
        private IEnumerator PlaceHands(ActionContext context, KissApproachType approachType,
            GameCharacter target, CharacterPosture targetPosture)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            // Actor側の手のInteractionPointを取得
            var actorRightHand = context.Actor.GetInteractionPoint(InteractionPointType.Hand, isRight: true);
            var actorLeftHand = context.Actor.GetInteractionPoint(InteractionPointType.Hand, isRight: false);

            // Target側のInteractionPointを取得
            var targetMouth = target.GetInteractionPoint(InteractionPointType.Mouth);
            var targetRightShoulder = target.GetInteractionPoint(InteractionPointType.Shoulder, isRight: true);
            var targetLeftShoulder = target.GetInteractionPoint(InteractionPointType.Shoulder, isRight: false);
            var targetHead = target.GetInteractionPoint(InteractionPointType.Head);

            Vector3 rightHandTarget;
            Vector3 leftHandTarget;

            switch (approachType)
            {
                case KissApproachType.LeanOver:
                    // 寝ている人：頬に手を添える（口の横）
                    if (actorRightHand != null && targetMouth != null)
                    {
                        // 右手：口の右側（Target基準で右）
                        rightHandTarget = InteractionPointMatcher.CalculateHandIKTarget(
                            actorRightHand, targetMouth, localOffset: new Vector3(0.08f, 0, 0));
                        // 左手：口の左側（Target基準で左）
                        leftHandTarget = InteractionPointMatcher.CalculateHandIKTarget(
                            actorLeftHand, targetMouth, localOffset: new Vector3(-0.08f, 0, 0));
                    }
                    else
                    {
                        // フォールバック
                        Vector3 mouthPos = targetMouth != null ? targetMouth.GetWorldPosition() : target.transform.position + Vector3.up * 1.5f;
                        rightHandTarget = mouthPos + target.transform.right * 0.08f;
                        leftHandTarget = mouthPos - target.transform.right * 0.08f;
                    }
                    break;

                case KissApproachType.BendDown:
                case KissApproachType.KneelDown:
                    // 低い人：両頬を包むように（頭の横）
                    if (actorRightHand != null && targetHead != null)
                    {
                        // 右手：頭の右側
                        rightHandTarget = InteractionPointMatcher.CalculateHandIKTarget(
                            actorRightHand, targetHead, localOffset: new Vector3(0.1f, -0.05f, 0));
                        // 左手：頭の左側
                        leftHandTarget = InteractionPointMatcher.CalculateHandIKTarget(
                            actorLeftHand, targetHead, localOffset: new Vector3(-0.1f, -0.05f, 0));
                    }
                    else
                    {
                        // フォールバック
                        Vector3 headPos = targetHead != null ? targetHead.GetWorldPosition() : target.transform.position + Vector3.up * 1.4f;
                        rightHandTarget = headPos + target.transform.right * 0.1f;
                        leftHandTarget = headPos - target.transform.right * 0.1f;
                    }
                    break;

                default:
                    // 通常：右手を肩、左手を後頭部/頬
                    if (actorRightHand != null && targetRightShoulder != null)
                    {
                        // 右手：相手の右肩
                        rightHandTarget = InteractionPointMatcher.CalculateHandIKTarget(
                            actorRightHand, targetRightShoulder);
                    }
                    else
                    {
                        // フォールバック
                        rightHandTarget = targetRightShoulder != null
                            ? targetRightShoulder.GetWorldPosition()
                            : target.transform.position + Vector3.up * 1.3f + target.transform.right * 0.15f;
                    }

                    // 左手：後頭部（頭の後ろ）
                    if (actorLeftHand != null && targetHead != null)
                    {
                        leftHandTarget = InteractionPointMatcher.CalculateHandIKTarget(
                            actorLeftHand, targetHead, localOffset: new Vector3(0, 0, -0.1f));
                    }
                    else
                    {
                        // フォールバック
                        leftHandTarget = targetHead != null
                            ? targetHead.GetWorldPosition() - target.transform.forward * 0.1f
                            : target.transform.position + Vector3.up * 1.5f - target.transform.forward * 0.1f;
                    }
                    break;
            }

            // IKターゲットオブジェクト作成
            _rightHandIKTarget = new GameObject("RightHandKissTarget");
            _rightHandIKTarget.transform.position = rightHandTarget;

            _leftHandIKTarget = new GameObject("LeftHandKissTarget");
            _leftHandIKTarget.transform.position = leftHandTarget;

            if (showDetailedLog)
            {
                Debug.Log($"[KissAction-PlaceHands] Approach: {approachType}");
                Debug.Log($"[KissAction-PlaceHands] Right hand target: {rightHandTarget}");
                Debug.Log($"[KissAction-PlaceHands] Left hand target: {leftHandTarget}");
            }

            // 両手を同時に動かす
            ik.SetHandIKTarget(HandType.Right, _rightHandIKTarget.transform);
            ik.SetHandIKTarget(HandType.Left, _leftHandIKTarget.transform);

            context.Actor.StartCoroutine(ik.SetHandIKWeight(HandType.Right, 1f, handPlacementDuration));
            yield return ik.SetHandIKWeight(HandType.Left, 1f, handPlacementDuration);

            if (showDetailedLog)
                Debug.Log("[KissAction-PlaceHands] Hands placed successfully using InteractionPoint matching");
        }

        /// <summary>
        /// 顔を近づける（Head IK使用 + InteractionPointマッチング）
        /// </summary>
        private IEnumerator ApproachFace(ActionContext context, GameCharacter target, KissApproachType approachType)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            // Actor側とTarget側のMouth InteractionPointを取得
            var actorMouthPoint = context.Actor.GetInteractionPoint(InteractionPointType.Mouth);
            var targetMouthPoint = target.GetInteractionPoint(InteractionPointType.Mouth);

            // フォールバック: InteractionPointがない場合
            if (actorMouthPoint == null || targetMouthPoint == null)
            {
                Debug.LogWarning("[KissAction-ApproachFace] Mouth InteractionPoint not found. Please setup InteractionPoints using Auto Setup tool.");

                // 従来の方法でフォールバック
                Vector3 targetMouth = targetMouthPoint != null
                    ? targetMouthPoint.GetWorldPosition()
                    : target.transform.position + Vector3.up * 1.5f;
                yield return ik.ApproachHeadToPosition(targetMouth, faceApproachDuration, stopDistance: 0.0f);
                yield break;
            }

            // InteractionPointMatcherを使用してターゲット位置を計算（完全一致位置）
            Vector3 actorMouthPosition = actorMouthPoint.GetWorldPosition();
            Vector3 targetMouthPosition = targetMouthPoint.GetWorldPosition();

            // 現在の距離を計算（デバッグ用）
            float distance = InteractionPointMatcher.Distance(actorMouthPoint, targetMouthPoint);

            // ActorのHeadボーンとMouthの現在の相対位置を取得（IK適用後の実際の位置関係）
            var actorAnimator = context.Actor.GetComponent<Animator>();
            var actorHeadBone = actorAnimator != null ? actorAnimator.GetBoneTransform(HumanBodyBones.Head) : null;
            Vector3 headToMouthOffset = Vector3.zero;
            if (actorHeadBone != null)
            {
                headToMouthOffset = actorMouthPosition - actorHeadBone.position;
            }

            // TargetのMouth位置に、ActorのHeadボーンからMouthへのオフセットを適用
            // これにより、ActorのHeadボーンがTargetのMouth位置まで移動すると、ActorのMouthがTargetのMouthに重なる
            Vector3 headTargetPosition = targetMouthPosition - headToMouthOffset;

            if (showDetailedLog)
            {
                Debug.Log($"[KissAction-ApproachFace] === Current State (after IK) ===");
                Debug.Log($"[KissAction-ApproachFace] Actor HeadBone: {actorHeadBone.position}");
                Debug.Log($"[KissAction-ApproachFace] Actor Mouth: {actorMouthPosition}");
                Debug.Log($"[KissAction-ApproachFace] Target Mouth: {targetMouthPosition}");
                Debug.Log($"[KissAction-ApproachFace] Current HeadBone-to-Mouth offset: {headToMouthOffset} ({headToMouthOffset.y * 100f:F1}cm)");
                Debug.Log($"[KissAction-ApproachFace] Head target position: {headTargetPosition}");
                Debug.Log($"[KissAction-ApproachFace] Mouth distance: {distance:F3}m");
            }

            // Head IKを使用して頭を近づける（完全接触）
            yield return ik.ApproachHeadToPosition(headTargetPosition, faceApproachDuration, stopDistance: 0.0f);

            if (showDetailedLog)
            {
                Debug.Log($"[KissAction-ApproachFace] Face approached using Head IK + InteractionPoint matching");

                // 完了後の実際の位置を確認
                Vector3 finalActorMouth = actorMouthPoint.GetWorldPosition();
                Vector3 finalTargetMouth = targetMouthPoint.GetWorldPosition();
                float finalDistance = Vector3.Distance(finalActorMouth, finalTargetMouth);

                Debug.Log($"[KissAction-ApproachFace] === Final Result ===");
                Debug.Log($"  Final Actor Mouth: {finalActorMouth}");
                Debug.Log($"  Final Target Mouth: {finalTargetMouth}");
                Debug.Log($"  Final mouth distance: {finalDistance * 100f:F1}cm");
                Debug.Log($"  Y difference: {(finalActorMouth.y - finalTargetMouth.y) * 100f:F1}cm");
            }
        }

        /// <summary>
        /// 顔を戻す（Head IK使用）
        /// </summary>
        private IEnumerator RetractFace(ActionContext context)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            if (showDetailedLog)
                Debug.Log("[KissAction-RetractFace] Retracting face using Head IK...");

            // Head IKを使用して頭を元に戻す
            yield return ik.RetractHead(0.4f);

            if (showDetailedLog)
                Debug.Log("[KissAction-RetractFace] Face retracted successfully");
        }

        /// <summary>
        /// 手を離す
        /// </summary>
        private IEnumerator ReleaseHands(ActionContext context)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            if (showDetailedLog)
                Debug.Log("[KissAction-ReleaseHands] Releasing hand IK...");

            // 手のIKを解除
            context.Actor.StartCoroutine(ik.SetHandIKWeight(HandType.Right, 0f, 0.3f));
            yield return ik.SetHandIKWeight(HandType.Left, 0f, 0.3f);

            // クリーンアップ
            if (_rightHandIKTarget != null)
            {
                Object.Destroy(_rightHandIKTarget);
                _rightHandIKTarget = null;
            }

            if (_leftHandIKTarget != null)
            {
                Object.Destroy(_leftHandIKTarget);
                _leftHandIKTarget = null;
            }

            if (showDetailedLog)
                Debug.Log("[KissAction-ReleaseHands] Hands released successfully");
        }

        /// <summary>
        /// 姿勢を戻す
        /// </summary>
        private IEnumerator ResetPosture(ActionContext context, KissApproachType approachType)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            if (showDetailedLog)
                Debug.Log($"[KissAction-ResetPosture] Resetting posture for {approachType}...");

            switch (approachType)
            {
                case KissApproachType.BendDown:
                case KissApproachType.LeanOver:
                    yield return ik.BendSpineSmooth(0f, 0.4f);
                    break;

                case KissApproachType.KneelDown:
                    context.AnimationController?.CrossFade("Idle", 0.3f);
                    yield return new WaitForSeconds(0.5f);
                    break;

                case KissApproachType.TipToe:
                    yield return ik.TipToeSmooth(0f, 0.3f);
                    break;
            }

            if (showDetailedLog)
                Debug.Log("[KissAction-ResetPosture] Posture reset successfully");

            yield return null;
        }

        public override void Cancel(ActionContext context)
        {
            base.Cancel(context);

            // クリーンアップ
            if (_rightHandIKTarget != null)
            {
                Object.Destroy(_rightHandIKTarget);
                _rightHandIKTarget = null;
            }

            if (_leftHandIKTarget != null)
            {
                Object.Destroy(_leftHandIKTarget);
                _leftHandIKTarget = null;
            }
        }
    }
}
