using System.Collections;
using UnityEngine;
using RPGDefete.Character;
using RPG.Interaction;
using RPG.Combat;

namespace RPG.Action
{
    /// <summary>
    /// 攻撃アクション（近接）
    /// </summary>
    public class AttackAction : ActionBase
    {
        public override string ActionName => "Attack";
        public override ActionPriority Priority => ActionPriority.High;
        public override bool CanBeCancelled => false;

        public override bool CanExecute(ActionContext context)
        {
            return context.Target != null &&
                   context.Target.CompareTag("Enemy") &&
                   context.Distance < 2f;
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var target = context.Target;
            var actor = context.Actor;

            // 1. ターゲットに視線を向ける
            context.EyeGazeController?.SetGazeTarget(target.transform);

            // 2. 表情変更（怒り）
            context.ExpressionController?.StartPresetTransition("angry", 0.2f);

            // 3. 攻撃アニメーション再生
            context.AnimationController?.PlayAnimation("Attack");

            // 4. アニメーション完了待機
            yield return new WaitForSeconds(0.5f);

            // 5. ダメージ適用
            if (DamageSystem.Instance != null)
            {
                DamageSystem.Instance.ApplyDamage(
                    target,
                    10f,
                    DamageType.Physical,
                    MagicElement.None,
                    target.transform.position,
                    actor?.gameObject
                );
            }

            yield return new WaitForSeconds(0.3f);

            // 6. 表情を戻す
            context.ExpressionController?.StartPresetTransition("neutral", 0.3f);

            // 7. 視線解除
            context.EyeGazeController?.SetWeightImmediate(0);
        }
    }

    /// <summary>
    /// 魔法攻撃アクション
    /// </summary>
    public class MagicAction : ActionBase
    {
        public override string ActionName => "Magic";
        public override ActionPriority Priority => ActionPriority.High;
        public override bool CanBeCancelled => false;

        public float CastTime { get; set; } = 1f;

        public override bool CanExecute(ActionContext context)
        {
            // 遠距離または地面クリック
            return context.Distance >= 2f || context.Target == null;
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var actor = context.Actor;
            Vector3 targetPos = context.TargetPosition;

            // ターゲット方向を向く
            if (context.Target != null)
            {
                targetPos = context.Target.transform.position;
            }

            Vector3 lookDir = (targetPos - actor.transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                actor.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // 1. 詠唱アニメーション
            context.AnimationController?.PlayAnimation("Cast");

            // 2. 詠唱表情
            context.ExpressionController?.StartPresetTransition("focused", 0.3f);

            yield return new WaitForSeconds(CastTime * 0.7f);

            // 3. MagicSystem経由で発射
            var magicSystem = MagicSystem.Instance;
            if (magicSystem != null)
            {
                var firePoint = actor.transform;
                var targetTransform = context.Target?.transform;
                magicSystem.FireMagic(null, firePoint, targetTransform, actor.gameObject, 20f);
            }

            yield return new WaitForSeconds(CastTime * 0.3f);

            // 4. 表情を戻す
            context.ExpressionController?.StartPresetTransition("neutral", 0.3f);
        }
    }

    /// <summary>
    /// 掴むアクション
    /// </summary>
    public class GrabAction : ActionBase
    {
        public override string ActionName => "Grab";

        public override bool CanExecute(ActionContext context)
        {
            var interactable = context.TargetInteractable;
            return interactable != null &&
                   interactable.HasAttribute(InteractableAttribute.Grabbable) &&
                   !interactable.IsBeingHeld;
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var actor = context.Actor;
            var target = context.TargetInteractable;

            // 1. オブジェクトに視線
            context.EyeGazeController?.SetGazeTarget(target.transform);

            // 2. 掴みアニメーション
            context.AnimationController?.PlayAnimation("Grab");

            yield return new WaitForSeconds(0.5f);

            // 3. オブジェクトを手の子要素に
            Transform handTransform = actor.GetHandTransform(HandSide.Right);
            if (handTransform != null)
            {
                target.transform.SetParent(handTransform);
                target.transform.localPosition = Vector3.zero;
                target.transform.localRotation = Quaternion.identity;
            }

            // 4. 状態更新
            target.OnGrab(actor);

            yield return new WaitForSeconds(0.2f);

            // 5. 視線解除
            context.EyeGazeController?.SetWeightImmediate(0);
        }
    }

    /// <summary>
    /// 離すアクション
    /// </summary>
    public class ReleaseAction : ActionBase
    {
        public override string ActionName => "Release";

        public override bool CanExecute(ActionContext context)
        {
            // 何か持っているか
            return context.TargetInteractable != null &&
                   context.TargetInteractable.IsBeingHeld &&
                   context.TargetInteractable.HeldBy == context.Actor;
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var actor = context.Actor;
            var target = context.TargetInteractable;

            // 1. 離すアニメーション
            context.AnimationController?.PlayAnimation("Release");

            yield return new WaitForSeconds(0.3f);

            // 2. オブジェクトを離す
            target.transform.SetParent(null);

            // 3. 状態更新
            target.OnRelease(actor);

            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 触るアクション
    /// </summary>
    public class TouchAction : ActionBase
    {
        public override string ActionName => "Touch";

        public override bool CanExecute(ActionContext context)
        {
            var interactable = context.TargetInteractable;
            return interactable != null &&
                   interactable.HasAttribute(InteractableAttribute.Touchable);
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var target = context.TargetInteractable;
            var touchPoint = target.GetPoint(InteractionPointType.Touch);

            // 1. 視線をターゲットへ
            context.EyeGazeController?.SetGazeTarget(touchPoint?.transform ?? target.transform);

            // 2. 手を伸ばすアニメーション
            context.AnimationController?.PlayAnimation("Reach");

            yield return new WaitForSeconds(0.5f);

            // 3. タッチ
            target.Interact(context.Actor);

            yield return new WaitForSeconds(0.3f);

            // 4. 視線解除
            context.EyeGazeController?.SetWeightImmediate(0);
        }
    }

    /// <summary>
    /// 座るアクション
    /// </summary>
    public class SitAction : ActionBase
    {
        public override string ActionName => "Sit";

        public override bool CanExecute(ActionContext context)
        {
            var interactable = context.TargetInteractable;
            return interactable != null &&
                   interactable.HasAttribute(InteractableAttribute.Sittable) &&
                   context.Actor.State != CharacterState.Sitting;
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var actor = context.Actor;
            var target = context.TargetInteractable;
            var sitPoint = target.GetPoint(InteractionPointType.Sit);

            Vector3 sitPosition = sitPoint?.transform.position ?? target.transform.position;
            Quaternion sitRotation = sitPoint?.transform.rotation ?? target.transform.rotation;

            // 1. sit_pointへ移動
            if (context.Navigator != null)
            {
                context.Navigator.MoveTo(sitPosition);
                yield return new WaitUntil(() => !context.Navigator.IsMoving);
            }

            // 2. 向きを調整
            actor.transform.rotation = sitRotation;

            // 3. 座るアニメーション
            context.AnimationController?.PlayAnimation("Sit");

            yield return new WaitForSeconds(0.8f);

            // 4. 状態更新
            actor.State = CharacterState.Sitting;

            // 5. リラックス表情
            context.ExpressionController?.StartPresetTransition("relaxed", 0.5f);
        }
    }

    /// <summary>
    /// 立ち上がるアクション
    /// </summary>
    public class StandAction : ActionBase
    {
        public override string ActionName => "Stand";

        public override bool CanExecute(ActionContext context)
        {
            return context.Actor.State == CharacterState.Sitting;
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var actor = context.Actor;

            // 1. 立ち上がるアニメーション
            context.AnimationController?.PlayAnimation("Stand");

            yield return new WaitForSeconds(0.6f);

            // 2. 状態更新
            actor.State = CharacterState.Idle;

            // 3. 表情を戻す
            context.ExpressionController?.StartPresetTransition("neutral", 0.3f);
        }
    }

    /// <summary>
    /// 会話アクション
    /// </summary>
    public class TalkAction : ActionBase
    {
        public override string ActionName => "Talk";

        public override bool CanExecute(ActionContext context)
        {
            return context.Target != null &&
                   (context.Target.CompareTag("NPC") ||
                    context.Target.CompareTag("Player") ||
                    context.Target.GetComponent<GameCharacter>() != null);
        }

        public override IEnumerator Execute(ActionContext context)
        {
            var actor = context.Actor;
            var target = context.Target.GetComponent<GameCharacter>();

            // 1. 相手の目を見る
            var eyePoint = target?.GetInteractionPoint(InteractionPointType.Eye);
            context.EyeGazeController?.SetGazeTarget(eyePoint?.transform ?? context.Target.transform);

            // 2. 会話状態に
            actor.State = CharacterState.Talking;
            if (target != null)
            {
                target.State = CharacterState.Talking;
            }

            // 3. 会話表情
            context.ExpressionController?.StartPresetTransition("smile", 0.3f);

            // 4. 会話システムを呼び出し（ここでは簡易実装）
            Debug.Log($"[TalkAction] {actor.CharacterName} started talking with {target?.CharacterName ?? context.Target.name}");

            // 会話終了まで待機（実際は会話システムからの通知を待つ）
            yield return new WaitForSeconds(1f);

            // 5. 状態を戻す
            actor.State = CharacterState.Idle;
            if (target != null)
            {
                target.State = CharacterState.Idle;
            }

            // 6. 視線解除
            context.EyeGazeController?.SetWeightImmediate(0);
        }
    }
}
