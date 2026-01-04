# 仕様書: Kissアクション詳細

## 概要

キャラクター間のキスを自然に実現するためのシステム。
対象の姿勢（立っている、座っている、寝ている）や身長差に応じて
アプローチ方法とIK設定を動的に調整する。

---

## 1. 基本フロー

```
┌─────────────────────────────────────────────────────────────┐
│                    Kissアクション基本フロー                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [1] ターゲット状態分析                                     │
│  ┌────────────────────────────────────────┐                │
│  │ ・姿勢判定（Standing/Sitting/Lying）   │                │
│  │ ・身長差計算                           │                │
│  │ ・mouth_point位置取得                  │                │
│  └────────────────────────────────────────┘                │
│         │                                                   │
│         ▼                                                   │
│  [2] アプローチ位置決定                                     │
│  ┌────────────────────────────────────────┐                │
│  │ 姿勢と身長差に応じた最適位置を計算     │                │
│  └────────────────────────────────────────┘                │
│         │                                                   │
│         ▼                                                   │
│  [3] 移動・姿勢調整                                         │
│  ┌────────────────────────────────────────┐                │
│  │ ・NavMeshで移動                        │                │
│  │ ・必要に応じてしゃがむ/かがむ          │                │
│  └────────────────────────────────────────┘                │
│         │                                                   │
│         ▼                                                   │
│  [4] 手を添える                                             │
│  ┌────────────────────────────────────────┐                │
│  │ ・肩/頬/後頭部に手をIK                 │                │
│  └────────────────────────────────────────┘                │
│         │                                                   │
│         ▼                                                   │
│  [5] 顔を近づける                                           │
│  ┌────────────────────────────────────────┐                │
│  │ ・Head IKで口元を近づける              │                │
│  │ ・目を閉じる/細める                    │                │
│  └────────────────────────────────────────┘                │
│         │                                                   │
│         ▼                                                   │
│  [6] キス実行                                               │
│  ┌────────────────────────────────────────┐                │
│  │ ・接触                                 │                │
│  │ ・表情変化（両者）                     │                │
│  └────────────────────────────────────────┘                │
│         │                                                   │
│         ▼                                                   │
│  [7] 離れる                                                 │
│  ┌────────────────────────────────────────┐                │
│  │ ・IKリセット                           │                │
│  │ ・姿勢を戻す                           │                │
│  │ ・余韻表情                             │                │
│  └────────────────────────────────────────┘                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. ターゲット状態分析

### 2.1 姿勢判定

```csharp
public enum CharacterPosture
{
    Standing,   // 立っている
    Sitting,    // 座っている
    Crouching,  // しゃがんでいる
    LyingDown,  // 寝ている（仰向け）
    LyingFace,  // 寝ている（うつ伏せ）
    LyingSide   // 寝ている（横向き）
}

public class PostureDetector : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hipsTransform;
    [SerializeField] private Transform headTransform;

    [Header("判定閾値")]
    [SerializeField] private float lyingHeightThreshold = 0.5f;   // この高さ以下なら寝ている
    [SerializeField] private float sittingHeightRatio = 0.6f;     // 立位の60%以下なら座っている

    private float _standingHipsHeight;  // 立っている時の腰の高さ

    private void Start()
    {
        // 立位時の腰の高さを記録
        _standingHipsHeight = hipsTransform.position.y;
    }

    /// <summary>
    /// 現在の姿勢を判定
    /// </summary>
    public CharacterPosture GetCurrentPosture()
    {
        float currentHipsHeight = hipsTransform.position.y;
        float headHeight = headTransform.position.y;

        // 寝ている判定（腰が非常に低い）
        if (currentHipsHeight < lyingHeightThreshold)
        {
            return DetermineLyingDirection();
        }

        // 座っている判定
        if (currentHipsHeight < _standingHipsHeight * sittingHeightRatio)
        {
            return CharacterPosture.Sitting;
        }

        // しゃがんでいる判定（腰が中間）
        if (currentHipsHeight < _standingHipsHeight * 0.8f)
        {
            return CharacterPosture.Crouching;
        }

        return CharacterPosture.Standing;
    }

    /// <summary>
    /// 寝ている方向を判定
    /// </summary>
    private CharacterPosture DetermineLyingDirection()
    {
        Vector3 headToHips = (hipsTransform.position - headTransform.position).normalized;
        float dotUp = Vector3.Dot(headToHips, Vector3.up);
        float dotForward = Vector3.Dot(transform.forward, Vector3.up);

        if (Mathf.Abs(dotForward) > 0.7f)
        {
            // 顔が上または下を向いている
            return dotForward > 0 ? CharacterPosture.LyingFace : CharacterPosture.LyingDown;
        }

        return CharacterPosture.LyingSide;
    }

    /// <summary>
    /// mouth_pointのワールド位置を取得
    /// </summary>
    public Vector3 GetMouthPosition()
    {
        var mouthPoint = GetComponentInChildren<InteractionPoint>()
            ?.GetPoint(InteractionPointType.Mouth);

        if (mouthPoint != null)
            return mouthPoint.GetWorldPosition();

        // フォールバック：頭の前方
        return headTransform.position + headTransform.forward * 0.1f;
    }
}
```

### 2.2 身長差計算

```csharp
public class HeightDifferenceCalculator
{
    /// <summary>
    /// 身長差を計算（プラス = 対象が高い、マイナス = 対象が低い）
    /// </summary>
    public static float CalculateHeightDifference(GameCharacter actor, GameCharacter target)
    {
        float actorMouthHeight = GetMouthHeight(actor);
        float targetMouthHeight = GetMouthHeight(target);

        return targetMouthHeight - actorMouthHeight;
    }

    /// <summary>
    /// 口の高さを取得（姿勢を考慮）
    /// </summary>
    private static float GetMouthHeight(GameCharacter character)
    {
        var mouthPoint = character.GetInteractionPoint(InteractionPointType.Mouth);
        if (mouthPoint != null)
            return mouthPoint.GetWorldPosition().y;

        // フォールバック
        return character.transform.position.y + 1.5f;
    }

    /// <summary>
    /// 身長差カテゴリを判定
    /// </summary>
    public static HeightDifferenceCategory CategorizeHeightDifference(float difference)
    {
        if (difference > 0.3f) return HeightDifferenceCategory.MuchTaller;
        if (difference > 0.1f) return HeightDifferenceCategory.Taller;
        if (difference > -0.1f) return HeightDifferenceCategory.Similar;
        if (difference > -0.3f) return HeightDifferenceCategory.Shorter;
        return HeightDifferenceCategory.MuchShorter;
    }
}

public enum HeightDifferenceCategory
{
    MuchTaller,   // 対象がずっと高い（+30cm以上）
    Taller,       // 対象が高い（+10〜30cm）
    Similar,      // ほぼ同じ（±10cm）
    Shorter,      // 対象が低い（-10〜30cm）
    MuchShorter   // 対象がずっと低い（-30cm以上）
}
```

---

## 3. シチュエーション別アプローチ

### 3.1 アプローチパターン図解

```
┌─────────────────────────────────────────────────────────────┐
│              シチュエーション別アプローチ                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  【1】同じ身長・立っている                                  │
│  ┌─────────────────────────────────────┐                   │
│  │                                     │                   │
│  │     Actor      Target               │                   │
│  │       ○──────────○                  │                   │
│  │       │          │                  │                   │
│  │       │    →     │ ← 手を肩に       │                   │
│  │       │          │                  │                   │
│  │      ╱╲         ╱╲                  │                   │
│  │                                     │                   │
│  │  ・正面から近づく                    │                   │
│  │  ・手を肩/後頭部に                   │                   │
│  │  ・顔をまっすぐ近づける              │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  【2】対象がずっと低い（子供サイズ）                        │
│  ┌─────────────────────────────────────┐                   │
│  │                                     │                   │
│  │     Actor                           │                   │
│  │       ○                             │                   │
│  │       │╲                            │                   │
│  │       │ ╲  ← かがむ                 │                   │
│  │       │  ╲                          │                   │
│  │      ╱╲   ○ ← Target               │                   │
│  │           │                         │                   │
│  │          ╱╲                         │                   │
│  │                                     │                   │
│  │  ・膝を曲げてかがむ                  │                   │
│  │  ・手を頬/頭に                       │                   │
│  │  ・上から顔を近づける                │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  【3】対象が座っている                                      │
│  ┌─────────────────────────────────────┐                   │
│  │                                     │                   │
│  │     Actor                           │                   │
│  │       ○                             │                   │
│  │       │╲                            │                   │
│  │       │ ╲  ← 上体を倒す             │                   │
│  │      ╱╲  ○ ← Target（椅子に座る）   │                   │
│  │          └──                        │                   │
│  │                                     │                   │
│  │  ・上体を前に倒す                    │                   │
│  │  ・または膝をつく                    │                   │
│  │  ・手を肩/顎に                       │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  【4】対象が寝ている（仰向け）                              │
│  ┌─────────────────────────────────────┐                   │
│  │                                     │                   │
│  │           ○ ← Actor（かがむ）       │                   │
│  │          /│╲                        │                   │
│  │         / │ ╲                       │                   │
│  │     ───○──┴──────  ← Target（寝ている）│                │
│  │                                     │                   │
│  │  ・膝をつく or しゃがむ              │                   │
│  │  ・上から覆いかぶさる                │                   │
│  │  ・手を頬に                          │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
│  【5】対象がずっと高い                                      │
│  ┌─────────────────────────────────────┐                   │
│  │                                     │                   │
│  │              ○ ← Target             │                   │
│  │              │                      │                   │
│  │       ○     │ ← 見上げる            │                   │
│  │       │╱    │                       │                   │
│  │       │     │                       │                   │
│  │      ╱╲    ╱╲                       │                   │
│  │                                     │                   │
│  │  ・つま先立ち                        │                   │
│  │  ・相手に屈んでもらう                │                   │
│  │  ・手を胸/お腹に                     │                   │
│  └─────────────────────────────────────┘                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 アプローチ戦略決定

```csharp
public enum KissApproachType
{
    FrontStanding,      // 正面から立って
    BendDown,           // かがんで
    KneelDown,          // 膝をついて
    LeanOver,           // 覆いかぶさって
    TipToe,             // つま先立ちで
    SitBeside,          // 横に座って
    FromBehind          // 後ろから
}

public class KissApproachStrategy
{
    /// <summary>
    /// 最適なアプローチ方法を決定
    /// </summary>
    public static KissApproachType DetermineApproach(
        CharacterPosture targetPosture,
        HeightDifferenceCategory heightDiff)
    {
        // 対象が寝ている場合
        if (targetPosture == CharacterPosture.LyingDown ||
            targetPosture == CharacterPosture.LyingSide)
        {
            return KissApproachType.LeanOver;
        }

        if (targetPosture == CharacterPosture.LyingFace)
        {
            return KissApproachType.KneelDown; // うつ伏せなら横から
        }

        // 対象が座っている場合
        if (targetPosture == CharacterPosture.Sitting)
        {
            return heightDiff switch
            {
                HeightDifferenceCategory.MuchShorter => KissApproachType.KneelDown,
                _ => KissApproachType.BendDown
            };
        }

        // 対象が立っている場合
        return heightDiff switch
        {
            HeightDifferenceCategory.MuchTaller => KissApproachType.TipToe,
            HeightDifferenceCategory.MuchShorter => KissApproachType.BendDown,
            HeightDifferenceCategory.Shorter => KissApproachType.BendDown,
            _ => KissApproachType.FrontStanding
        };
    }

    /// <summary>
    /// アプローチ位置を計算
    /// </summary>
    public static Vector3 CalculateApproachPosition(
        GameCharacter target,
        KissApproachType approachType)
    {
        Vector3 targetPos = target.transform.position;
        Vector3 targetForward = target.transform.forward;
        var posture = target.GetComponent<PostureDetector>().GetCurrentPosture();

        switch (approachType)
        {
            case KissApproachType.FrontStanding:
            case KissApproachType.TipToe:
                // 正面から適切な距離
                return targetPos + targetForward * 0.4f;

            case KissApproachType.BendDown:
                // やや近め
                return targetPos + targetForward * 0.3f;

            case KissApproachType.KneelDown:
                // 膝をつく位置
                return targetPos + targetForward * 0.35f;

            case KissApproachType.LeanOver:
                // 寝ている人の横
                if (posture == CharacterPosture.LyingDown)
                {
                    // 仰向け：頭の横
                    Vector3 headPos = target.GetInteractionPoint(InteractionPointType.Mouth)
                        .GetWorldPosition();
                    return headPos + target.transform.right * 0.4f;
                }
                else
                {
                    // 横向き：顔の正面
                    return targetPos + targetForward * 0.3f;
                }

            case KissApproachType.SitBeside:
                // 横に座る
                return targetPos + target.transform.right * 0.5f;

            default:
                return targetPos + targetForward * 0.4f;
        }
    }
}
```

---

## 4. KissAction クラス

```csharp
public class KissAction : ActionBase
{
    public override string ActionName => "Kiss";

    [Header("キス設定")]
    [SerializeField] private float approachDuration = 0.5f;
    [SerializeField] private float kissDuration = 1.5f;
    [SerializeField] private float handPlacementDuration = 0.3f;

    public override bool CanExecute(ActionContext context)
    {
        // 対象がキャラクターであること
        var targetChar = context.Target?.GetComponent<GameCharacter>();
        return targetChar != null;
    }

    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.Target.GetComponent<GameCharacter>();

        // 1. ターゲット状態分析
        var postureDetector = target.GetComponent<PostureDetector>();
        CharacterPosture targetPosture = postureDetector?.GetCurrentPosture()
            ?? CharacterPosture.Standing;

        float heightDiff = HeightDifferenceCalculator.CalculateHeightDifference(actor, target);
        var heightCategory = HeightDifferenceCalculator.CategorizeHeightDifference(heightDiff);

        // 2. アプローチ方法決定
        KissApproachType approachType = KissApproachStrategy.DetermineApproach(
            targetPosture, heightCategory);

        // 3. アプローチ位置へ移動
        Vector3 approachPos = KissApproachStrategy.CalculateApproachPosition(target, approachType);
        actor.Navigator.MoveTo(approachPos);
        yield return new WaitUntil(() => !actor.Navigator.IsMoving);

        // 4. 向きを調整（相手を向く）
        yield return RotateToFace(actor, target);

        // 5. 姿勢調整
        yield return AdjustPosture(context, approachType, heightDiff);

        // 6. 手を添える
        yield return PlaceHands(context, approachType, targetPosture);

        // 7. 視線を合わせる
        context.EyeGazeController?.SetLookAtTarget(
            target.GetInteractionPoint(InteractionPointType.Eye)?.transform);

        // 8. 表情変化（照れ/期待）
        context.ExpressionController?.StartPresetTransition("embarrassed", 0.3f);
        target.GetComponent<VRMExpressionController>()?.StartPresetTransition("embarrassed", 0.3f);

        yield return new WaitForSeconds(0.3f);

        // 9. 顔を近づける
        yield return ApproachFace(context, target, approachType);

        // 10. 目を閉じる
        context.ExpressionController?.SetExpressionWeight("blink", 0.9f);
        target.GetComponent<VRMExpressionController>()?.SetExpressionWeight("blink", 0.8f);

        // 11. キス
        yield return new WaitForSeconds(kissDuration);

        // 12. 離れる
        yield return RetractFace(context);

        // 13. 目を開ける
        context.ExpressionController?.SetExpressionWeight("blink", 0f);
        target.GetComponent<VRMExpressionController>()?.SetExpressionWeight("blink", 0f);

        // 14. 余韻表情
        context.ExpressionController?.StartPresetTransition("happy", 0.3f);
        target.GetComponent<VRMExpressionController>()?.StartPresetTransition("smile", 0.3f);

        // 15. 手を離す
        yield return ReleaseHands(context);

        // 16. 姿勢を戻す
        yield return ResetPosture(context, approachType);

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 相手を向く
    /// </summary>
    private IEnumerator RotateToFace(GameCharacter actor, GameCharacter target)
    {
        Vector3 direction = (target.transform.position - actor.transform.position).normalized;
        direction.y = 0;

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
    private IEnumerator AdjustPosture(ActionContext context, KissApproachType approachType,
        float heightDiff)
    {
        var animator = context.Actor.GetComponent<Animator>();
        var ik = context.IKController;

        switch (approachType)
        {
            case KissApproachType.BendDown:
                // 上体を前に傾ける + 腰を落とす
                yield return BendForward(ik, Mathf.Abs(heightDiff) * 30f); // 身長差に応じて角度調整
                break;

            case KissApproachType.KneelDown:
                // 膝をつくアニメーション
                animator?.CrossFade("Kneel", 0.3f);
                yield return new WaitForSeconds(0.5f);
                break;

            case KissApproachType.LeanOver:
                // しゃがむ + 覆いかぶさる
                animator?.CrossFade("Crouch", 0.3f);
                yield return new WaitForSeconds(0.3f);
                yield return LeanOver(ik);
                break;

            case KissApproachType.TipToe:
                // つま先立ち
                yield return TipToe(ik);
                break;

            default:
                yield return null;
                break;
        }
    }

    /// <summary>
    /// 手を添える
    /// </summary>
    private IEnumerator PlaceHands(ActionContext context, KissApproachType approachType,
        CharacterPosture targetPosture)
    {
        var target = context.Target.GetComponent<GameCharacter>();
        var ik = context.IKController;
        var finger = context.Actor.GetComponent<VRMFingerController>();

        // 手のターゲット位置を決定
        Vector3 rightHandTarget;
        Vector3 leftHandTarget;

        switch (approachType)
        {
            case KissApproachType.LeanOver:
                // 寝ている人：頬に手を添える
                rightHandTarget = target.GetInteractionPoint(InteractionPointType.Mouth)
                    .GetWorldPosition() + target.transform.right * 0.1f;
                leftHandTarget = rightHandTarget - target.transform.right * 0.2f;
                break;

            case KissApproachType.BendDown:
            case KissApproachType.KneelDown:
                // 低い人：両頬を包むように
                var headPos = target.GetInteractionPoint(InteractionPointType.Eye)?.GetWorldPosition()
                    ?? target.transform.position + Vector3.up * 1f;
                rightHandTarget = headPos + target.transform.right * 0.08f;
                leftHandTarget = headPos - target.transform.right * 0.08f;
                break;

            default:
                // 通常：片手を肩、片手を後頭部
                var shoulderPoint = target.GetInteractionPoint(InteractionPointType.Shoulder);
                rightHandTarget = shoulderPoint?.GetWorldPosition()
                    ?? target.transform.position + Vector3.up * 1.3f + target.transform.right * 0.15f;

                // 後頭部
                leftHandTarget = target.GetInteractionPoint(InteractionPointType.Eye).GetWorldPosition()
                    - target.transform.forward * 0.15f;
                break;
        }

        // 優しく触れる指ポーズ
        finger?.ApplyPose("Caress", HandSide.Right);
        finger?.ApplyPose("Caress", HandSide.Left);

        // 両手を同時に動かす
        StartCoroutine(ik.MoveHandSmooth(HandSide.Right, rightHandTarget, handPlacementDuration));
        yield return ik.MoveHandSmooth(HandSide.Left, leftHandTarget, handPlacementDuration);
    }

    /// <summary>
    /// 顔を近づける
    /// </summary>
    private IEnumerator ApproachFace(ActionContext context, GameCharacter target,
        KissApproachType approachType)
    {
        var ik = context.IKController;
        var actor = context.Actor;

        // ターゲットの口の位置
        Vector3 targetMouth = target.GetInteractionPoint(InteractionPointType.Mouth)
            .GetWorldPosition();

        // 自分の口の位置
        Vector3 actorMouth = actor.GetInteractionPoint(InteractionPointType.Mouth)
            .GetWorldPosition();

        // 近づく方向と距離
        Vector3 direction = (targetMouth - actorMouth).normalized;
        float distance = Vector3.Distance(targetMouth, actorMouth);

        // Head IKで顔を近づける
        // FinalIKの場合はLookAtIKやFABRIKを使用
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t);

            // 接触直前で止める（口が重ならないよう）
            float targetDistance = distance - 0.05f;
            Vector3 headOffset = direction * (targetDistance * t);

            // Head IKを設定（実装は使用するIKシステムに依存）
            ik.SetHeadOffset(headOffset);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 顔を戻す
    /// </summary>
    private IEnumerator RetractFace(ActionContext context)
    {
        var ik = context.IKController;

        float elapsed = 0f;
        float duration = 0.4f;
        Vector3 currentOffset = ik.GetHeadOffset();

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            ik.SetHeadOffset(Vector3.Lerp(currentOffset, Vector3.zero, t));

            elapsed += Time.deltaTime;
            yield return null;
        }

        ik.SetHeadOffset(Vector3.zero);
    }

    /// <summary>
    /// 前かがみになる
    /// </summary>
    private IEnumerator BendForward(VRMFinalIKController ik, float angle)
    {
        // Spine IKを使用して上体を傾ける
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentAngle = Mathf.Lerp(0, angle, Mathf.SmoothStep(0, 1, t));
            ik.SetSpineBend(currentAngle);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 覆いかぶさる
    /// </summary>
    private IEnumerator LeanOver(VRMFinalIKController ik)
    {
        // 大きく前傾
        yield return BendForward(ik, 60f);
    }

    /// <summary>
    /// つま先立ち
    /// </summary>
    private IEnumerator TipToe(VRMFinalIKController ik)
    {
        // 足首IKで踵を上げる
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float heelHeight = Mathf.Lerp(0, 0.05f, t);
            ik.SetHeelHeight(heelHeight);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 手を離す
    /// </summary>
    private IEnumerator ReleaseHands(ActionContext context)
    {
        var ik = context.IKController;
        var finger = context.Actor.GetComponent<VRMFingerController>();

        // 指をOpenに戻す
        StartCoroutine(finger.TransitionToPose("Open", HandSide.Right, 0.3f));
        StartCoroutine(finger.TransitionToPose("Open", HandSide.Left, 0.3f));

        // 手のIKを解除
        StartCoroutine(ik.ResetHand(HandSide.Right, 0.3f));
        yield return ik.ResetHand(HandSide.Left, 0.3f);
    }

    /// <summary>
    /// 姿勢を戻す
    /// </summary>
    private IEnumerator ResetPosture(ActionContext context, KissApproachType approachType)
    {
        var animator = context.Actor.GetComponent<Animator>();
        var ik = context.IKController;

        switch (approachType)
        {
            case KissApproachType.BendDown:
                yield return BendForward(ik, 0); // 戻す
                break;

            case KissApproachType.KneelDown:
                animator?.CrossFade("StandUp", 0.3f);
                yield return new WaitForSeconds(0.5f);
                break;

            case KissApproachType.LeanOver:
                yield return BendForward(ik, 0);
                animator?.CrossFade("Stand", 0.3f);
                yield return new WaitForSeconds(0.3f);
                break;

            case KissApproachType.TipToe:
                ik.SetHeelHeight(0);
                break;
        }

        yield return null;
    }
}
```

---

## 5. VRMFinalIKController 拡張

```csharp
// KissAction用のIKメソッド追加
public partial class VRMFinalIKController
{
    private Vector3 _headOffset;
    private float _spineBendAngle;
    private float _heelHeight;

    /// <summary>
    /// 頭のオフセットを設定（顔を近づける）
    /// </summary>
    public void SetHeadOffset(Vector3 offset)
    {
        _headOffset = offset;
        // FinalIKのLookAtやFABRIKで適用
        // 実装は使用するIKソリューションに依存
    }

    public Vector3 GetHeadOffset() => _headOffset;

    /// <summary>
    /// 脊椎の曲げ角度を設定（前かがみ）
    /// </summary>
    public void SetSpineBend(float angle)
    {
        _spineBendAngle = angle;
        // SpineのIK設定で前傾を適用
        // または直接ボーン回転
    }

    /// <summary>
    /// 踵の高さを設定（つま先立ち）
    /// </summary>
    public void SetHeelHeight(float height)
    {
        _heelHeight = height;
        // Foot IKで踵を上げる
    }
}
```

---

## 6. 対象の反応

### 6.1 KissReactionController

```csharp
public class KissReactionController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameCharacter character;
    [SerializeField] private VRMExpressionController expressionController;

    [Header("反応設定")]
    [SerializeField] private bool allowKiss = true;
    [SerializeField] private float affectionThreshold = 50f; // 好感度閾値

    // イベント
    public event Action OnKissReceived;
    public event Action OnKissRejected;

    /// <summary>
    /// キスを受け入れるか判定
    /// </summary>
    public bool WillAcceptKiss(GameCharacter kisser)
    {
        if (!allowKiss) return false;

        // 好感度チェック（将来拡張）
        // float affection = RelationshipSystem.GetAffection(kisser, character);
        // return affection >= affectionThreshold;

        return true;
    }

    /// <summary>
    /// キス開始時の反応
    /// </summary>
    public void OnKissApproach(GameCharacter kisser)
    {
        if (WillAcceptKiss(kisser))
        {
            // 受け入れ：照れ表情 + 目を閉じる準備
            expressionController?.StartPresetTransition("embarrassed", 0.3f);
        }
        else
        {
            // 拒否：驚き/嫌がり
            expressionController?.StartPresetTransition("surprised", 0.2f);
            OnKissRejected?.Invoke();
        }
    }

    /// <summary>
    /// キス中の反応
    /// </summary>
    public void OnKissing()
    {
        // 目を閉じる
        expressionController?.SetExpressionWeight("blink", 0.8f);
    }

    /// <summary>
    /// キス後の反応
    /// </summary>
    public void OnKissEnd()
    {
        OnKissReceived?.Invoke();

        // 余韻：嬉しそう
        expressionController?.StartPresetTransition("happy", 0.5f);
    }
}
```

---

## 7. シチュエーション図解

```
┌─────────────────────────────────────────────────────────────┐
│                 各シチュエーションの詳細                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ 【シーン1】子供キャラへのキス                               │
│ ──────────────────────────────                              │
│                                                             │
│   Before              During                After           │
│                                                             │
│     Actor              Actor                 Actor          │
│       ○                  ○                    ○            │
│       │                 /│                    │            │
│       │               /  │                    │            │
│       │             /    │                    │            │
│      ╱╲    ○      ○────○      ○            ╱╲    ○      │
│          Target   (かがんで)    (照れ)           Target    │
│                                                             │
│   1. 身長差を検出（-40cm）                                  │
│   2. BendDownアプローチ選択                                 │
│   3. 膝を曲げてかがむ                                       │
│   4. 両頬を手で包む                                         │
│   5. おでこ or 頬にキス                                     │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ 【シーン2】寝ているキャラへのキス                           │
│ ──────────────────────────────                              │
│                                                             │
│   Before                During                After         │
│                                                             │
│                           ○                                │
│                          /│╲                               │
│   ───○───────       ───○──┴───       ───○───────          │
│     Target              Target              Target          │
│                          ↑                                  │
│        Actor           Actor                Actor           │
│         ○            (覆いかぶさる)           ○            │
│         │                                     │            │
│        ╱╲                                    ╱╲            │
│                                                             │
│   1. 姿勢をLyingDownと判定                                  │
│   2. LeanOverアプローチ選択                                 │
│   3. 頭の横に膝をつく                                       │
│   4. 頬に手を添える                                         │
│   5. 覆いかぶさってキス                                     │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ 【シーン3】座っているキャラへのキス                         │
│ ──────────────────────────────                              │
│                                                             │
│   Before              During                After           │
│                                                             │
│     Actor              Actor                 Actor          │
│       ○                 ○╲                    ○            │
│       │                  │ ╲                   │            │
│       │                  │  ╲                  │            │
│      ╱╲                 ╱╲  ○                ╱╲            │
│          ○──            ○──                  ○──           │
│         Target         Target               Target          │
│                       (前かがみ)                            │
│                                                             │
│   1. 姿勢をSittingと判定                                    │
│   2. BendDownアプローチ選択                                 │
│   3. 上体を前に傾ける                                       │
│   4. 肩に手を置く                                           │
│   5. 顔を近づけてキス                                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 8. テストケース

| # | シチュエーション | 期待結果 |
|---|-----------------|---------|
| 1 | 同身長・立ち | 正面から近づき、肩/後頭部に手、まっすぐキス |
| 2 | 対象が30cm低い | かがんで、両頬を包んでキス |
| 3 | 対象が30cm高い | つま先立ち、胸あたりに手 |
| 4 | 対象が座っている | 上体を傾けて、肩に手を置いてキス |
| 5 | 対象が仰向け寝 | 膝をついて覆いかぶさり、頬に手 |
| 6 | 対象が横向き寝 | 顔の正面からしゃがんでキス |
| 7 | 子供サイズ | 深くかがみ、頬/おでこにキス |
| 8 | キス拒否 | 対象が驚き表情、アクション中断 |

---

## 9. 今後の拡張

- キスの種類（頬、額、手の甲など）
- 好感度による反応変化
- 複数キャラの同時反応（目撃者）
- 会話後の自然なキスへの流れ

