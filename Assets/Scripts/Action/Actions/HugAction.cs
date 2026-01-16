using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using RPGDefete.Character;

namespace RPG.Action
{
    /// <summary>
    /// ハグアクション
    /// 両腕で相手を抱きしめる
    /// </summary>
    public class HugAction : ActionBase
    {
        public override string ActionName => "Hug";
        public override ActionPriority Priority => ActionPriority.Normal;

        [Header("Hug設定")]
        [SerializeField] private float approachDistance = 0.2f;  // 抱きしめる距離
        [SerializeField] private float armPlacementDuration = 0.5f;
        [SerializeField] private float hugDuration = 2f;
        [SerializeField] private float releaseArmDuration = 0.4f;

        [Header("タイミング調整")]
        [SerializeField] private float afterApproachWait = 0.2f;
        [SerializeField] private float afterRotateWait = 0.1f;
        [SerializeField] private float beforeArmPlaceWait = 0.3f;
        [SerializeField] private float afterArmPlaceWait = 0.2f;
        [SerializeField] private float afterHugWait = 0.3f;

        [Header("デバッグ")]
        [SerializeField] private bool showDetailedLog = true;
        [SerializeField] private bool showGizmos = true;

        private GameObject _rightArmIKTarget;
        private GameObject _leftArmIKTarget;

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
                Debug.LogWarning("[HugAction] Target is not a GameCharacter");
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
                    Debug.Log("[HugAction] Disabled target's NavMeshAgent to prevent collision avoidance");
            }

            try
            {
                if (showDetailedLog)
                    Debug.Log($"[HugAction] Starting hug: {actor.CharacterName} -> {target.CharacterName}");

                // 1. アプローチ位置へ移動（正面、近距離）
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 1. Moving to approach position (distance: {approachDistance}m)...");
                Vector3 approachPos = target.transform.position + target.transform.forward * approachDistance;
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

                // 2. 向きを調整（相手を向く）
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 2. Rotating to face target...");
                yield return RotateToFace(actor, target);
                yield return new WaitForSeconds(afterRotateWait);

                // 3. 視線を合わせる
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 3. Making eye contact...");
                var targetEyePoint = target.GetInteractionPoint(InteractionPointType.Eye);
                if (targetEyePoint != null && context.EyeGazeController != null)
                {
                    context.EyeGazeController.SetGazeTarget(targetEyePoint.transform);
                    context.EyeGazeController.SetWeight(1f);
                }

                // 4. 感情表現（笑顔または悲しみ）
                // ハグは慰め・喜び両方のケースがあるため、感情に応じて変える
                string emotionPreset = actor.CurrentEmotion == Emotion.Sadness ? "sorrow" : "joy";
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 4. Showing emotion: {emotionPreset}...");
                context.ExpressionController?.StartPresetTransition(emotionPreset, 0.3f);

                if (target.ExpressionController != null)
                {
                    string targetEmotion = target.CurrentEmotion == Emotion.Sadness ? "sorrow" : "joy";
                    target.ExpressionController.StartPresetTransition(targetEmotion, 0.3f);
                }

                yield return new WaitForSeconds(beforeArmPlaceWait);

                // 5. 腕を回す（両腕で抱きしめる）
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 5. Placing arms around target...");
                yield return PlaceArms(context, target);
                yield return new WaitForSeconds(afterArmPlaceWait);

                // 6. ハグ中（静止）
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 6. Hugging for {hugDuration} seconds...");
                yield return new WaitForSeconds(hugDuration);

                // 7. 腕を離す
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 7. Releasing arms...");
                yield return ReleaseArms(context);
                yield return new WaitForSeconds(afterHugWait);

                // 8. 表情を戻す（余韻）
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 8. Showing afterglow expression...");
                context.ExpressionController?.StartPresetTransition("smile", 0.3f);
                if (target.ExpressionController != null)
                    target.ExpressionController.StartPresetTransition("smile", 0.3f);

                // 9. 視線解除
                if (context.EyeGazeController != null)
                    context.EyeGazeController.SetWeight(0f);

                // 10. 少し離れる
                if (showDetailedLog)
                    Debug.Log($"[HugAction] 9. Stepping back...");
                Vector3 backPos = actor.transform.position - actor.transform.forward * 0.3f;
                if (context.Navigator != null)
                {
                    context.Navigator.MoveTo(backPos);
                    yield return new WaitUntil(() => !context.Navigator.IsMoving);
                }

                    if (showDetailedLog)
                        Debug.Log("[HugAction] ✓ Hug completed successfully!");
            }
            finally
            {
                // ターゲットのNavMeshAgentを再有効化
                if (targetAgent != null && wasAgentEnabled)
                {
                    targetAgent.enabled = true;
                    if (showDetailedLog)
                        Debug.Log("[HugAction] Re-enabled target's NavMeshAgent");
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
        /// 腕を配置（相手の背中/腰に）（InteractionPointマッチング使用）
        /// </summary>
        private IEnumerator PlaceArms(ActionContext context, GameCharacter target)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            // Actor側の手のInteractionPointを取得
            var actorRightHand = context.Actor.GetInteractionPoint(InteractionPointType.Hand, isRight: true);
            var actorLeftHand = context.Actor.GetInteractionPoint(InteractionPointType.Hand, isRight: false);

            // ターゲットの肩/腰のInteractionPointを取得
            var targetLeftShoulder = target.GetInteractionPoint(InteractionPointType.Shoulder, isRight: false);
            var targetHip = target.GetInteractionPoint(InteractionPointType.Hip);

            Vector3 rightArmTarget;
            Vector3 leftArmTarget;

            // 右手：相手の背中（左肩甲骨あたり）
            if (actorRightHand != null && targetLeftShoulder != null)
            {
                // InteractionPointMatcherで位置計算（背中側にオフセット）
                rightArmTarget = InteractionPointMatcher.CalculateHandIKTarget(
                    actorRightHand, targetLeftShoulder,
                    localOffset: new Vector3(-0.1f, 0, -0.15f)); // 左側・後ろ
            }
            else
            {
                // フォールバック
                if (targetLeftShoulder != null)
                {
                    rightArmTarget = targetLeftShoulder.GetWorldPosition() - target.transform.forward * 0.15f - target.transform.right * 0.1f;
                }
                else
                {
                    rightArmTarget = target.transform.position + Vector3.up * 1.2f - target.transform.forward * 0.15f;
                }
            }

            // 左手：相手の背中（腰あたり）
            if (actorLeftHand != null && targetHip != null)
            {
                // InteractionPointMatcherで位置計算（腰の背中側）
                leftArmTarget = InteractionPointMatcher.CalculateHandIKTarget(
                    actorLeftHand, targetHip,
                    localOffset: new Vector3(0.05f, 0, -0.1f)); // やや右・後ろ
            }
            else
            {
                // フォールバック
                if (targetHip != null)
                {
                    leftArmTarget = targetHip.GetWorldPosition() - target.transform.forward * 0.1f + target.transform.right * 0.05f;
                }
                else
                {
                    leftArmTarget = target.transform.position + Vector3.up * 0.9f - target.transform.forward * 0.1f;
                }
            }

            // IKターゲットオブジェクト作成
            _rightArmIKTarget = new GameObject("RightArmHugTarget");
            _rightArmIKTarget.transform.position = rightArmTarget;

            _leftArmIKTarget = new GameObject("LeftArmHugTarget");
            _leftArmIKTarget.transform.position = leftArmTarget;

            if (showDetailedLog)
            {
                Debug.Log($"[HugAction-PlaceArms] Right arm target: {rightArmTarget}");
                Debug.Log($"[HugAction-PlaceArms] Left arm target: {leftArmTarget}");
            }

            // 両腕を同時に動かす
            ik.SetHandIKTarget(HandType.Right, _rightArmIKTarget.transform);
            ik.SetHandIKTarget(HandType.Left, _leftArmIKTarget.transform);

            context.Actor.StartCoroutine(ik.SetHandIKWeight(HandType.Right, 1f, armPlacementDuration));
            yield return ik.SetHandIKWeight(HandType.Left, 1f, armPlacementDuration);

            if (showDetailedLog)
                Debug.Log("[HugAction-PlaceArms] Arms placed successfully using InteractionPoint matching");
        }

        /// <summary>
        /// 腕を離す
        /// </summary>
        private IEnumerator ReleaseArms(ActionContext context)
        {
            var ik = context.IKController;
            if (ik == null) yield break;

            if (showDetailedLog)
                Debug.Log("[HugAction-ReleaseArms] Releasing arm IK...");

            // 手のIKを解除
            context.Actor.StartCoroutine(ik.SetHandIKWeight(HandType.Right, 0f, releaseArmDuration));
            yield return ik.SetHandIKWeight(HandType.Left, 0f, releaseArmDuration);

            // クリーンアップ
            if (_rightArmIKTarget != null)
            {
                Object.Destroy(_rightArmIKTarget);
                _rightArmIKTarget = null;
            }

            if (_leftArmIKTarget != null)
            {
                Object.Destroy(_leftArmIKTarget);
                _leftArmIKTarget = null;
            }

            if (showDetailedLog)
                Debug.Log("[HugAction-ReleaseArms] Arms released successfully");
        }

        public override void Cancel(ActionContext context)
        {
            base.Cancel(context);

            // クリーンアップ
            if (_rightArmIKTarget != null)
            {
                Object.Destroy(_rightArmIKTarget);
                _rightArmIKTarget = null;
            }

            if (_leftArmIKTarget != null)
            {
                Object.Destroy(_leftArmIKTarget);
                _leftArmIKTarget = null;
            }
        }
    }
}
