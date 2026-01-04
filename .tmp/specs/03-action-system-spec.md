# 仕様書: 行動システム

## 概要

キャラクターが実行する各種行動を統一的に管理・実行するシステム。
IK制御、アニメーション、表情変化を組み合わせて自然な動作を実現する。

---

## 1. ActionBase 基底クラス

```csharp
public enum ActionState
{
    Ready,
    Executing,
    Completed,
    Cancelled
}

public abstract class ActionBase
{
    public abstract string ActionName { get; }
    public ActionState State { get; protected set; } = ActionState.Ready;

    /// <summary>
    /// このアクションが実行可能かどうか
    /// </summary>
    public abstract bool CanExecute(ActionContext context);

    /// <summary>
    /// アクション実行（コルーチン）
    /// </summary>
    public abstract IEnumerator Execute(ActionContext context);

    /// <summary>
    /// アクション開始時のコールバック
    /// </summary>
    public virtual void OnStart(ActionContext context)
    {
        State = ActionState.Executing;
    }

    /// <summary>
    /// アクション終了時のコールバック
    /// </summary>
    public virtual void OnEnd(ActionContext context)
    {
        State = ActionState.Completed;
    }

    /// <summary>
    /// アクションキャンセル
    /// </summary>
    public virtual void Cancel(ActionContext context)
    {
        State = ActionState.Cancelled;
    }
}
```

---

## 2. ActionContext クラス

```csharp
public class ActionContext
{
    // 実行者
    public GameCharacter Actor { get; set; }

    // ターゲット情報
    public GameObject Target { get; set; }
    public Vector3 TargetPosition { get; set; }
    public InteractionPoint TargetPoint { get; set; }
    public Interactable TargetInteractable { get; set; }

    // 追加パラメータ
    public float Distance { get; set; }
    public SizeCategory TargetSize { get; set; }

    // コントローラー参照
    public VRMFinalIKController IKController => Actor?.GetComponent<VRMFinalIKController>();
    public VRMExpressionController ExpressionController => Actor?.GetComponent<VRMExpressionController>();
    public VRMAnimationController AnimationController => Actor?.GetComponent<VRMAnimationController>();
    public VRMEyeGazeController EyeGazeController => Actor?.GetComponent<VRMEyeGazeController>();
}
```

---

## 3. ActionExecutor クラス

```csharp
public class ActionExecutor : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameCharacter character;

    // 登録されたアクション
    private Dictionary<string, ActionBase> _actions = new Dictionary<string, ActionBase>();

    // 現在実行中のアクション
    public ActionBase CurrentAction { get; private set; }
    public bool IsExecuting => CurrentAction != null && CurrentAction.State == ActionState.Executing;

    private void Awake()
    {
        RegisterDefaultActions();
    }

    /// <summary>
    /// デフォルトアクションを登録
    /// </summary>
    private void RegisterDefaultActions()
    {
        RegisterAction(new AttackAction());
        RegisterAction(new MagicAction());
        RegisterAction(new GrabAction());
        RegisterAction(new TouchAction());
        RegisterAction(new EatAction());
        RegisterAction(new SitAction());
        RegisterAction(new StompAction());
        RegisterAction(new TalkAction());
        RegisterAction(new KissAction());
        RegisterAction(new HugAction());
    }

    /// <summary>
    /// アクションを登録
    /// </summary>
    public void RegisterAction(ActionBase action)
    {
        _actions[action.ActionName] = action;
    }

    /// <summary>
    /// アクションを実行
    /// </summary>
    public bool TryExecuteAction(string actionName, ActionContext context)
    {
        if (IsExecuting) return false;
        if (!_actions.TryGetValue(actionName, out var action)) return false;
        if (!action.CanExecute(context)) return false;

        StartCoroutine(ExecuteActionCoroutine(action, context));
        return true;
    }

    private IEnumerator ExecuteActionCoroutine(ActionBase action, ActionContext context)
    {
        CurrentAction = action;
        action.OnStart(context);

        yield return action.Execute(context);

        action.OnEnd(context);
        CurrentAction = null;
    }

    /// <summary>
    /// 現在のアクションをキャンセル
    /// </summary>
    public void CancelCurrentAction()
    {
        if (CurrentAction != null)
        {
            CurrentAction.Cancel(new ActionContext { Actor = character });
            StopAllCoroutines();
            CurrentAction = null;
        }
    }
}
```

---

## 4. 攻撃行動

### 4.1 AttackAction（近接攻撃）

```csharp
public class AttackAction : ActionBase
{
    public override string ActionName => "Attack";

    public override bool CanExecute(ActionContext context)
    {
        // 近距離のEnemyが必要
        return context.Target != null &&
               context.Target.CompareTag("Enemy") &&
               context.Distance < 2f;
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var target = context.Target;
        var actor = context.Actor;

        // 1. ターゲットに視線を向ける
        context.EyeGazeController?.SetLookAtTarget(target.transform);

        // 2. 表情変更（怒り）
        context.ExpressionController?.StartPresetTransition("angry", 0.2f);

        // 3. 攻撃アニメーション再生
        context.AnimationController?.PlayAnimation("Attack");

        // 4. アニメーション完了待機
        yield return new WaitForSeconds(0.8f);

        // 5. ダメージ判定（別システムで処理）

        // 6. 表情を戻す
        context.ExpressionController?.StartPresetTransition("neutral", 0.3f);

        yield return new WaitForSeconds(0.3f);
    }
}
```

### 4.2 MagicAction（魔法攻撃）

```csharp
public class MagicAction : ActionBase
{
    public override string ActionName => "Magic";

    [Header("魔法設定")]
    public GameObject magicEffectPrefab;
    public float castTime = 1.5f;

    public override bool CanExecute(ActionContext context)
    {
        // 遠距離または地面クリック
        return context.Distance >= 2f || context.Target == null;
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        Vector3 targetPos = context.TargetPosition;

        // 1. 詠唱準備（手を前に出す）
        SetupCastingPose(context);

        // 2. 詠唱表情
        context.ExpressionController?.StartPresetTransition("smile", 0.3f);

        // 3. 詠唱アニメーション
        context.AnimationController?.PlayAnimation("Cast");

        // 4. RFX4エフェクト発動
        var effectEvent = actor.GetComponent<RFX4_EffectEvent>();
        effectEvent?.ActivateCharacterEffect(); // 手元エフェクト

        yield return new WaitForSeconds(castTime * 0.7f);

        // 5. メインエフェクト発射
        effectEvent?.ActivateEffect();

        yield return new WaitForSeconds(castTime * 0.3f);

        // 6. IKリセット
        ResetCastingPose(context);

        // 7. 表情を戻す
        context.ExpressionController?.ResetToNeutral(0.3f);
    }

    private void SetupCastingPose(ActionContext context)
    {
        // 右手を前に出すIK設定
        var ik = context.IKController;
        // IK設定コード
    }

    private void ResetCastingPose(ActionContext context)
    {
        var ik = context.IKController;
        // IKリセットコード
    }
}
```

---

## 5. 日常行動

### 5.1 GrabAction（掴む）

| サイズ | 挙動 | IK設定 |
|--------|------|--------|
| Tiny | 指で摘む | RightHand |
| Small | 片手で持つ | RightHand |
| Medium | 両手で持つ | BothHands |
| Large | 押す/引く | BothHands + Body |

```csharp
public class GrabAction : ActionBase
{
    public override string ActionName => "Grab";

    public override bool CanExecute(ActionContext context)
    {
        var interactable = context.TargetInteractable;
        return interactable != null &&
               interactable.HasAttribute(InteractableAttribute.Grabbable);
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.TargetInteractable;
        var grabPoint = target.GetPoint(InteractionPointType.Grab);

        // 1. オブジェクトに視線
        context.EyeGazeController?.SetLookAtTarget(target.transform);

        // 2. サイズに応じた掴み方
        switch (context.TargetSize)
        {
            case SizeCategory.Tiny:
            case SizeCategory.Small:
                yield return GrabOneHanded(context, grabPoint);
                break;
            case SizeCategory.Medium:
            case SizeCategory.Large:
                yield return GrabTwoHanded(context, grabPoint);
                break;
        }

        // 3. オブジェクトを手の子要素に
        AttachToHand(context, target);

        // 4. 状態更新
        target.isBeingHeld = true;
        target.heldBy = actor;
        actor.CurrentHeldObject = target;
    }

    private IEnumerator GrabOneHanded(ActionContext context, InteractionPoint point)
    {
        var ik = context.IKController;
        // 右手IKをgrab_pointへ移動
        // アニメーション同期
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator GrabTwoHanded(ActionContext context, InteractionPoint point)
    {
        var ik = context.IKController;
        // 両手IKをgrab_pointへ移動
        yield return new WaitForSeconds(0.7f);
    }

    private void AttachToHand(ActionContext context, Interactable obj)
    {
        // オブジェクトを手に取り付け
        var handTransform = context.Actor.GetHandTransform(HandSide.Right);
        obj.transform.SetParent(handTransform);
        obj.transform.localPosition = Vector3.zero;
    }
}
```

### 5.2 EatAction（食べる）

```csharp
public class EatAction : ActionBase
{
    public override string ActionName => "Eat";

    public override bool CanExecute(ActionContext context)
    {
        // 食べ物を持っている必要がある
        var heldObject = context.Actor.CurrentHeldObject;
        return heldObject != null &&
               heldObject.HasAttribute(InteractableAttribute.Eatable);
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var food = actor.CurrentHeldObject;
        var mouthPoint = actor.GetInteractionPoint(InteractionPointType.Mouth);

        switch (food.sizeCategory)
        {
            case SizeCategory.Tiny:
            case SizeCategory.Small:
                // 手を口に運ぶ
                yield return BringToMouth(context, mouthPoint);
                break;
            case SizeCategory.Medium:
            case SizeCategory.Large:
                // 顔を近づける
                yield return LeanToFood(context, food);
                break;
        }

        // 口を開ける
        context.ExpressionController?.SetExpressionWeight("aa", 0.7f);
        yield return new WaitForSeconds(0.3f);

        // 食べるアニメーション
        context.AnimationController?.PlayAnimation("Eat");
        yield return new WaitForSeconds(0.5f);

        // 口を閉じる + 幸せ表情
        context.ExpressionController?.SetExpressionWeight("aa", 0f);
        context.ExpressionController?.StartPresetTransition("happy", 0.3f);

        // 食べ物を消す（必要に応じて）
        // Object.Destroy(food.gameObject);

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator BringToMouth(ActionContext context, InteractionPoint mouth)
    {
        // 手を口の位置へIK移動
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator LeanToFood(ActionContext context, Interactable food)
    {
        // 頭/体を食べ物に近づけるIK
        yield return new WaitForSeconds(0.7f);
    }
}
```

### 5.3 SitAction（座る）

```csharp
public class SitAction : ActionBase
{
    public override string ActionName => "Sit";

    public override bool CanExecute(ActionContext context)
    {
        var interactable = context.TargetInteractable;
        return interactable != null &&
               interactable.HasAttribute(InteractableAttribute.Sittable);
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.TargetInteractable;
        var sitPoint = target.GetPoint(InteractionPointType.Sit);

        // 1. sit_pointへ移動
        actor.Navigator.MoveTo(sitPoint.GetApproachPosition());
        yield return new WaitUntil(() => !actor.Navigator.IsMoving);

        // 2. 向きを調整
        actor.transform.rotation = sitPoint.transform.rotation;

        // 3. 座るアニメーション
        context.AnimationController?.PlayAnimation("Sit");
        yield return new WaitForSeconds(0.8f);

        // 4. 腰IKをsit_pointに固定
        var ik = context.IKController;
        // Hip IK設定

        // 5. リラックス表情
        context.ExpressionController?.StartPresetTransition("content", 0.5f);

        // 6. 座っている状態を維持
        actor.State = CharacterState.Sitting;
    }
}
```

### 5.4 TouchAction / StompAction

```csharp
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
        var touchPoint = context.TargetInteractable.GetPoint(InteractionPointType.Touch);

        // 1. 視線をターゲットへ
        context.EyeGazeController?.SetLookAtTarget(touchPoint.transform);

        // 2. 手をtouch_pointへIK移動
        var ik = context.IKController;
        // Hand IK設定

        yield return new WaitForSeconds(0.5f);

        // 3. タッチ
        // コールバック発火など

        yield return new WaitForSeconds(0.3f);

        // 4. IKリセット
    }
}

public class StompAction : ActionBase
{
    public override string ActionName => "Stomp";

    public override bool CanExecute(ActionContext context)
    {
        var interactable = context.TargetInteractable;
        return interactable != null &&
               interactable.HasAttribute(InteractableAttribute.Stompable);
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var stompPoint = context.TargetInteractable.GetPoint(InteractionPointType.Stomp);

        // 1. 足を上げるアニメーション
        context.AnimationController?.PlayAnimation("LiftFoot");
        yield return new WaitForSeconds(0.3f);

        // 2. 足IKをstomp_pointへ
        var ik = context.IKController;
        // Foot IK設定

        yield return new WaitForSeconds(0.5f);

        // 3. 踏みつけ
        // コールバック発火

        yield return new WaitForSeconds(0.3f);
    }
}
```

---

## 6. 感情行動

### 6.1 TalkAction（会話）

```csharp
public class TalkAction : ActionBase
{
    public override string ActionName => "Talk";

    public override bool CanExecute(ActionContext context)
    {
        return context.Target != null &&
               (context.Target.CompareTag("Chara") || context.Target.CompareTag("Player"));
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.Target.GetComponent<GameCharacter>();

        // 1. 相手の目を見る
        var eyePoint = target?.GetInteractionPoint(InteractionPointType.Eye);
        context.EyeGazeController?.SetLookAtTarget(eyePoint?.transform ?? context.Target.transform);

        // 2. 会話システム呼び出し（別途実装）
        // DialogueSystem.StartDialogue(actor, target);

        // 3. 口パク処理はDialogueSystemから制御
        // VRMExpressionControllerのSetLipSyncを使用

        yield return null; // 会話終了まで待機
    }
}
```

### 6.2 KissAction / HugAction

```csharp
public class KissAction : ActionBase
{
    public override string ActionName => "Kiss";

    public override bool CanExecute(ActionContext context)
    {
        // 特殊コマンドまたは親密度条件
        return context.Target != null &&
               context.Target.CompareTag("Chara");
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.Target.GetComponent<GameCharacter>();
        var mouthPoint = target.GetInteractionPoint(InteractionPointType.Mouth);

        // 1. 相手に近づく
        actor.Navigator.MoveTo(target.transform.position + target.transform.forward * 0.3f);
        yield return new WaitUntil(() => !actor.Navigator.IsMoving);

        // 2. 相手の肩に手を置く
        var shoulderPoint = target.GetInteractionPoint(InteractionPointType.Shoulder);
        // Hand IK設定

        // 3. 目を細める
        context.ExpressionController?.StartPresetTransition("embarrassed", 0.3f);

        // 4. 頭を近づける
        // Head IK設定

        yield return new WaitForSeconds(1f);

        // 5. キス
        yield return new WaitForSeconds(0.5f);

        // 6. 離れる
        // IKリセット
        context.ExpressionController?.StartPresetTransition("smile", 0.3f);
    }
}

public class HugAction : ActionBase
{
    public override string ActionName => "Hug";

    public override bool CanExecute(ActionContext context)
    {
        return context.Target != null &&
               context.Target.CompareTag("Chara");
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.Target.GetComponent<GameCharacter>();

        // 1. 相手に近づく
        actor.Navigator.MoveTo(target.transform.position);
        yield return new WaitUntil(() => !actor.Navigator.IsMoving);

        // 2. 両腕を相手の肩/腰へ
        var shoulderL = target.GetInteractionPoint(InteractionPointType.Shoulder);
        var hipPoint = target.GetInteractionPoint(InteractionPointType.Hip);
        // Both Arms IK設定

        // 3. 表情（感情に応じて変化）
        string expression = actor.CurrentEmotion == Emotion.Sadness ? "crying" : "happy";
        context.ExpressionController?.StartPresetTransition(expression, 0.3f);

        yield return new WaitForSeconds(2f);

        // 4. 離れる
        // IKリセット
    }
}
```

---

## 7. アクション優先度と競合

```csharp
public enum ActionPriority
{
    Low = 0,      // 日常行動
    Normal = 1,   // 通常行動
    High = 2,     // 戦闘行動
    Critical = 3  // 緊急行動（ダメージ反応等）
}
```

| アクション | 優先度 | キャンセル可能 |
|-----------|--------|---------------|
| Walk | Low | Yes |
| Grab/Touch | Normal | Yes |
| Sit | Normal | Yes |
| Talk | Normal | Yes |
| Attack | High | No |
| Magic | High | No |
| Damage Reaction | Critical | No |

---

## 8. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | Smallオブジェクトをgrab | 片手で掴むIK + アニメ |
| 2 | Mediumオブジェクトをgrab | 両手で掴むIK |
| 3 | 食べ物を持ってeat | 口へ運ぶ + 口開け |
| 4 | 椅子をクリックしてsit | 移動→座るアニメ |
| 5 | 近距離Enemyにattack | 攻撃アニメ + 表情 |
| 6 | 遠距離でmagic | 詠唱 + エフェクト |
| 7 | NPCにtalk | 視線合わせ + 口パク |
| 8 | アクション中に移動 | アクションキャンセル確認 |

