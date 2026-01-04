# 仕様書: アニメーション・IKハイブリッドシステム

## 概要

アニメーションとIKを組み合わせ、最小限のアニメーションクリップで
多様なシチュエーションに対応する姿勢制御システム。

---

## 1. 基本方針

```
┌─────────────────────────────────────────────────────────────┐
│                    ハイブリッド方式の考え方                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  【アニメーションの役割】                                   │
│  ・重心移動を伴う大きな姿勢変化                             │
│  ・膝をつく、立ち上がる、座る等の遷移動作                   │
│  ・歩行、走行などのループ動作                               │
│                                                             │
│  【IKの役割】                                               │
│  ・手足の細かい位置調整                                     │
│  ・脊椎の曲げ角度（前傾、後傾）                             │
│  ・頭の向き（LookAt）                                       │
│  ・身長差や距離に応じた連続的な調整                         │
│                                                             │
│  【組み合わせ方】                                           │
│   アニメーション（ベース姿勢）                              │
│         ↓                                                   │
│   IK（微調整レイヤー）                                      │
│         ↓                                                   │
│   最終的な姿勢                                              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 必要なアニメーション一覧

### 2.1 基本姿勢アニメーション（必須）

| # | アニメーション名 | 用途 | 入手方法 | 優先度 |
|---|-----------------|------|---------|--------|
| 1 | Idle_Standing | 立ち待機 | 既存/Mixamo | ★★★ |
| 2 | Walk | 歩行 | 既存/Mixamo | ★★★ |
| 3 | Run | 走行 | 既存/Mixamo | ★★★ |

### 2.2 姿勢遷移アニメーション（必須）

| # | アニメーション名 | 用途 | 入手方法 | 優先度 |
|---|-----------------|------|---------|--------|
| 4 | **Kneel_Down** | 膝をつく遷移 | Mixamo「Kneeling Down」 | ★★★ |
| 5 | **Kneel_Idle** | 膝をついた待機 | Mixamo「Kneeling Idle」 | ★★★ |
| 6 | **Kneel_Up** | 立ち上がる遷移 | Mixamo「Standing Up」 | ★★★ |
| 7 | **Crouch_Down** | しゃがむ遷移 | Mixamo「Crouching Down」 | ★★☆ |
| 8 | **Crouch_Idle** | しゃがみ待機 | Mixamo「Crouching Idle」 | ★★☆ |
| 9 | **Crouch_Up** | 立ち上がる | Kneel_Upと共用可 | ★★☆ |

### 2.3 アクション用アニメーション（推奨）

| # | アニメーション名 | 用途 | 入手方法 | 優先度 |
|---|-----------------|------|---------|--------|
| 10 | Sit_Down | 椅子に座る | Mixamo「Sitting Down」 | ★★☆ |
| 11 | Sit_Idle | 座り待機 | Mixamo「Sitting Idle」 | ★★☆ |
| 12 | Sit_Up | 立ち上がる | Mixamo「Standing Up From Sitting」 | ★★☆ |
| 13 | Grab_Standing | 立って掴む | Mixamo「Picking Up」 | ★☆☆ |
| 14 | Grab_Crouching | しゃがんで掴む | Mixamo「Crouching Pick Up」 | ★☆☆ |

### 2.4 戦闘用アニメーション（必須）

| # | アニメーション名 | 用途 | 入手方法 | 優先度 |
|---|-----------------|------|---------|--------|
| 15 | Attack_Melee | 近接攻撃 | Mixamo「Sword Slash」等 | ★★★ |
| 16 | Cast_Magic | 魔法詠唱 | Mixamo「Standing Magic Attack」 | ★★★ |
| 17 | Hit_Reaction | 被ダメージ | Mixamo「Hit Reaction」 | ★★☆ |

---

## 3. Mixamoでの検索キーワード

```
┌─────────────────────────────────────────────────────────────┐
│                 Mixamo検索キーワード一覧                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  【膝をつく系】                                             │
│  ・"kneeling"           → 膝をつく動作全般                 │
│  ・"kneeling down"      → 膝をつく遷移                     │
│  ・"kneeling idle"      → 膝をついた待機                   │
│  ・"praying"            → 祈り姿勢（膝立ち）               │
│                                                             │
│  【しゃがむ系】                                             │
│  ・"crouch"             → しゃがむ全般                     │
│  ・"crouching"          → しゃがみ姿勢                     │
│  ・"squat"              → スクワット                       │
│                                                             │
│  【座る系】                                                 │
│  ・"sitting"            → 座る全般                         │
│  ・"sitting down"       → 座る遷移                         │
│  ・"sitting idle"       → 座り待機                         │
│                                                             │
│  【立ち上がる系】                                           │
│  ・"standing up"        → 立ち上がる                       │
│  ・"getting up"         → 起き上がる                       │
│                                                             │
│  【拾う/掴む系】                                            │
│  ・"picking up"         → 物を拾う                         │
│  ・"grab"               → 掴む                             │
│  ・"reach"              → 手を伸ばす                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. IKで調整する項目

### 4.1 BipedIK調整項目

```csharp
public class HybridPostureController : MonoBehaviour
{
    [Header("FinalIK References")]
    [SerializeField] private BipedIK bipedIK;
    [SerializeField] private LookAtIK lookAtIK;
    [SerializeField] private FABRIK spineFABRIK;  // オプション

    [Header("IK調整範囲")]
    [SerializeField] private float maxSpineBend = 60f;      // 脊椎最大前傾角度
    [SerializeField] private float maxTiptoeHeight = 0.1f;  // つま先立ち最大高さ
    [SerializeField] private float maxHeadTurn = 80f;       // 頭の最大回転角度

    // 現在のIK状態
    private float _currentSpineBend;
    private float _currentTiptoeHeight;
    private Vector3 _currentHeadOffset;

    /// <summary>
    /// IKで調整可能な項目一覧
    /// </summary>
    public enum IKAdjustment
    {
        SpineBend,      // 脊椎の前傾/後傾
        HeadPosition,   // 頭の位置オフセット
        HeadRotation,   // 頭の回転（LookAt）
        RightHand,      // 右手の位置/回転
        LeftHand,       // 左手の位置/回転
        RightFoot,      // 右足の位置（つま先立ち等）
        LeftFoot,       // 左足の位置
        BodyOffset      // 体全体のオフセット
    }
}
```

### 4.2 調整項目と用途

| 調整項目 | 用途 | 調整範囲 |
|---------|------|---------|
| **SpineBend** | 前かがみ、お辞儀 | 0〜60度 |
| **HeadPosition** | 顔を近づける | 0〜20cm |
| **HeadRotation** | 相手を見る | LookAtで自動 |
| **RightHand** | 触る、掴む、添える | 自由位置 |
| **LeftHand** | 同上 | 自由位置 |
| **RightFoot** | つま先立ち、踏む | 0〜10cm上昇 |
| **LeftFoot** | 同上 | 同上 |
| **BodyOffset** | 全体の位置微調整 | ±10cm |

---

## 5. シチュエーション別の実装

### 5.1 身長差対応（Kissの例）

```
┌─────────────────────────────────────────────────────────────┐
│              身長差別：アニメ vs IK の使い分け               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  【±10cm】同身長                                            │
│  ─────────────────────────────────────────                  │
│  アニメ: Idle_Standing                                      │
│  IK:     手を肩/後頭部へ、LookAt                            │
│                                                             │
│      ○────○                                                │
│      │    │  ← IKのみで完結                                │
│     ╱╲   ╱╲                                                │
│                                                             │
│                                                             │
│  【-10〜30cm】やや低い                                      │
│  ─────────────────────────────────────────                  │
│  アニメ: Idle_Standing                                      │
│  IK:     SpineBend 15〜30度、手を頬へ                       │
│                                                             │
│      ○                                                      │
│     /│                                                      │
│    / │    ○  ← Spine IKで前傾                              │
│   ╱╲     │                                                  │
│         ╱╲                                                  │
│                                                             │
│                                                             │
│  【-30cm以上】かなり低い（子供など）                        │
│  ─────────────────────────────────────────                  │
│  アニメ: Kneel_Down → Kneel_Idle ★アニメ必要              │
│  IK:     手を両頬へ、LookAt下向き                           │
│                                                             │
│      ○                                                      │
│     /│╲   ← 膝をつくアニメ                                 │
│    ╱  ╲                                                     │
│        ○                                                    │
│        │                                                    │
│       ╱╲                                                    │
│                                                             │
│                                                             │
│  【+10〜30cm】やや高い                                      │
│  ─────────────────────────────────────────                  │
│  アニメ: Idle_Standing                                      │
│  IK:     Foot IKでつま先立ち、LookAt上向き                  │
│                                                             │
│          ○                                                  │
│          │                                                  │
│      ○   │  ← Foot IKでつま先立ち                          │
│      │╱  │                                                  │
│     △   ╱╲  ← 踵を上げる                                   │
│                                                             │
│                                                             │
│  【寝ている対象】                                           │
│  ─────────────────────────────────────────                  │
│  アニメ: Kneel_Down → Kneel_Idle ★アニメ必要              │
│  IK:     SpineBend 45〜60度、手を頬へ                       │
│                                                             │
│        ○                                                    │
│       /│╲    ← 膝をつく + Spine IK                         │
│      /    ╲                                                 │
│  ───○──────────                                            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 判定フローチャート

```
┌─────────────────────────────────────────────────────────────┐
│                  姿勢決定フローチャート                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ターゲット状態を取得                                       │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐                                           │
│  │対象が寝ている？│                                          │
│  └──────┬───────┘                                           │
│         │                                                   │
│    Yes  │  No                                               │
│    ┌────┴────┐                                              │
│    ▼         ▼                                              │
│ Kneel_Down  ┌──────────────┐                               │
│ アニメ再生  │対象が座ってる？│                               │
│    +        └──────┬───────┘                               │
│ Spine IK           │                                        │
│ 60度          Yes  │  No                                    │
│              ┌────┴────┐                                    │
│              ▼         ▼                                    │
│           Spine IK  ┌──────────────┐                       │
│           45度      │身長差を計算   │                       │
│                     └──────┬───────┘                       │
│                            │                                │
│               ┌────────────┼────────────┐                  │
│               ▼            ▼            ▼                  │
│           差 < -30cm   -30〜+30cm    差 > +30cm            │
│               │            │            │                  │
│               ▼            ▼            ▼                  │
│          Kneel_Down    Spine IK     Foot IK                │
│          アニメ再生    のみで調整   つま先立ち              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. 実装コード

### 6.1 HybridPostureController

```csharp
public class HybridPostureController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private BipedIK bipedIK;
    [SerializeField] private LookAtIK lookAtIK;

    [Header("Animation States")]
    [SerializeField] private string idleState = "Idle_Standing";
    [SerializeField] private string kneelDownState = "Kneel_Down";
    [SerializeField] private string kneelIdleState = "Kneel_Idle";
    [SerializeField] private string kneelUpState = "Kneel_Up";
    [SerializeField] private string crouchState = "Crouch_Idle";

    [Header("IK Settings")]
    [SerializeField] private float spineBendSpeed = 2f;
    [SerializeField] private float maxSpineBend = 60f;

    // 現在の姿勢状態
    public PostureState CurrentPosture { get; private set; } = PostureState.Standing;

    // IK値
    private float _targetSpineBend;
    private float _currentSpineBend;
    private float _targetTiptoe;
    private float _currentTiptoe;

    public enum PostureState
    {
        Standing,
        Kneeling,
        Crouching,
        Sitting
    }

    private void Update()
    {
        // IK値をスムーズに補間
        _currentSpineBend = Mathf.Lerp(_currentSpineBend, _targetSpineBend,
            Time.deltaTime * spineBendSpeed);
        _currentTiptoe = Mathf.Lerp(_currentTiptoe, _targetTiptoe,
            Time.deltaTime * spineBendSpeed);

        ApplySpineBend(_currentSpineBend);
        ApplyTiptoe(_currentTiptoe);
    }

    /// <summary>
    /// 身長差に応じた姿勢を設定
    /// </summary>
    public IEnumerator AdaptToHeightDifference(float heightDiff, bool targetLying = false)
    {
        // 対象が寝ている場合
        if (targetLying)
        {
            yield return TransitionToKneel();
            SetSpineBend(55f);
            yield break;
        }

        // 身長差による分岐
        if (heightDiff < -0.3f)
        {
            // 30cm以上低い → 膝をつく
            yield return TransitionToKneel();
            SetSpineBend(20f);
        }
        else if (heightDiff < -0.1f)
        {
            // 10〜30cm低い → 前傾のみ
            float bendAngle = Mathf.Abs(heightDiff) * 100f; // 比例
            SetSpineBend(Mathf.Min(bendAngle, 45f));
        }
        else if (heightDiff > 0.1f)
        {
            // 10cm以上高い → つま先立ち
            float tiptoe = Mathf.Min(heightDiff * 0.3f, 0.08f);
            SetTiptoe(tiptoe);
        }
        else
        {
            // ほぼ同じ → 調整なし
            SetSpineBend(0f);
            SetTiptoe(0f);
        }
    }

    /// <summary>
    /// 膝をつく遷移
    /// </summary>
    public IEnumerator TransitionToKneel()
    {
        if (CurrentPosture == PostureState.Kneeling) yield break;

        animator.CrossFade(kneelDownState, 0.2f);
        yield return new WaitForSeconds(0.8f); // アニメーション時間

        animator.CrossFade(kneelIdleState, 0.1f);
        CurrentPosture = PostureState.Kneeling;
    }

    /// <summary>
    /// 立ち上がる遷移
    /// </summary>
    public IEnumerator TransitionToStand()
    {
        if (CurrentPosture == PostureState.Standing) yield break;

        // IKをリセット
        SetSpineBend(0f);
        SetTiptoe(0f);

        animator.CrossFade(kneelUpState, 0.2f);
        yield return new WaitForSeconds(0.8f);

        animator.CrossFade(idleState, 0.1f);
        CurrentPosture = PostureState.Standing;
    }

    /// <summary>
    /// 脊椎の前傾を設定
    /// </summary>
    public void SetSpineBend(float angle)
    {
        _targetSpineBend = Mathf.Clamp(angle, 0f, maxSpineBend);
    }

    /// <summary>
    /// つま先立ちを設定
    /// </summary>
    public void SetTiptoe(float height)
    {
        _targetTiptoe = Mathf.Clamp(height, 0f, 0.1f);
    }

    /// <summary>
    /// 脊椎IKを適用
    /// </summary>
    private void ApplySpineBend(float angle)
    {
        if (bipedIK == null) return;

        // FinalIKのSpine Bendを使用
        // または直接ボーン回転
        // 実装はFinalIKのバージョンに依存

        // 例: Spineボーンを直接回転
        Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        if (spine != null)
        {
            Quaternion bendRotation = Quaternion.Euler(angle, 0, 0);
            spine.localRotation = bendRotation;
        }
    }

    /// <summary>
    /// つま先立ちIKを適用
    /// </summary>
    private void ApplyTiptoe(float height)
    {
        if (bipedIK == null) return;

        // Foot IKで踵を上げる
        // 実装はFinalIKの設定に依存
    }

    /// <summary>
    /// 姿勢をリセット
    /// </summary>
    public IEnumerator ResetPosture()
    {
        SetSpineBend(0f);
        SetTiptoe(0f);

        if (CurrentPosture != PostureState.Standing)
        {
            yield return TransitionToStand();
        }
    }
}
```

### 6.2 使用例（KissAction統合）

```csharp
public class KissAction : ActionBase
{
    public override IEnumerator Execute(ActionContext context)
    {
        var actor = context.Actor;
        var target = context.Target.GetComponent<GameCharacter>();
        var postureController = actor.GetComponent<HybridPostureController>();

        // 姿勢検出
        bool targetLying = IsTargetLying(target);
        float heightDiff = CalculateHeightDifference(actor, target);

        // 姿勢適応（アニメ + IK自動選択）
        yield return postureController.AdaptToHeightDifference(heightDiff, targetLying);

        // 手を添える（IK）
        yield return PlaceHands(context);

        // 顔を近づける（IK）
        yield return ApproachFace(context);

        // キス実行
        yield return new WaitForSeconds(1.5f);

        // 離れる
        yield return RetractFace(context);
        yield return ReleaseHands(context);

        // 姿勢リセット
        yield return postureController.ResetPosture();
    }
}
```

---

## 7. アニメーション準備チェックリスト

### 7.1 最小構成（これだけあれば動く）

```
□ Idle_Standing     - 立ち待機
□ Walk              - 歩行
□ Kneel_Down        - 膝をつく遷移 ★重要
□ Kneel_Idle        - 膝をついた待機 ★重要
□ Kneel_Up          - 立ち上がる遷移 ★重要
```

### 7.2 推奨構成（より自然に）

```
□ Idle_Standing
□ Walk
□ Run
□ Kneel_Down        ★
□ Kneel_Idle        ★
□ Kneel_Up          ★
□ Crouch_Down
□ Crouch_Idle
□ Sit_Down
□ Sit_Idle
□ Attack_Melee
□ Cast_Magic
```

### 7.3 フル構成（高品質）

```
□ 基本姿勢系（5種）
□ 姿勢遷移系（6種）
□ アクション系（5種）
□ 戦闘系（5種）
□ 表情連動（3種）
─────────────────
合計: 約24種類
```

---

## 8. Mixamoからのダウンロード手順

```
┌─────────────────────────────────────────────────────────────┐
│                 Mixamoアニメーション取得手順                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. https://www.mixamo.com/ にアクセス                      │
│                                                             │
│  2. Adobeアカウントでログイン（無料）                       │
│                                                             │
│  3. 「CHARACTERS」から任意のキャラを選択                    │
│     ※VRMを使う場合は後でリターゲット                        │
│                                                             │
│  4. 「ANIMATIONS」タブで検索                                │
│     ・"kneeling" で検索                                     │
│     ・プレビューで確認                                      │
│                                                             │
│  5. ダウンロード設定                                        │
│     ・Format: FBX for Unity (.fbx)                          │
│     ・Skin: Without Skin（アニメのみ）                      │
│     ・Frames per Second: 30                                 │
│     ・Keyframe Reduction: none                              │
│                                                             │
│  6. Unityにインポート                                       │
│     ・Assets/Animations/ に配置                             │
│     ・Rig設定をHumanoidに変更                               │
│     ・Avatar定義を設定（VRMのAvatarを参照）                 │
│                                                             │
│  7. AnimatorControllerに追加                                │
│     ・State追加                                             │
│     ・遷移設定                                              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 9. テストケース

| # | テスト | アニメ使用 | IK使用 | 期待結果 |
|---|-------|----------|--------|---------|
| 1 | 同身長Kiss | Idle | 手/頭 | 自然な立ちキス |
| 2 | 20cm低い相手にKiss | Idle | Spine 20度 | 軽く前かがみ |
| 3 | 40cm低い相手にKiss | Kneel系 | 手/頭 | 膝をついてキス |
| 4 | 寝ている相手にKiss | Kneel系 | Spine 55度 | 覆いかぶさる |
| 5 | 高い相手にKiss | Idle | Foot IK | つま先立ち |
| 6 | 座っている相手にKiss | Idle | Spine 45度 | 前かがみ |
| 7 | Kiss後に立ち上がる | Kneel_Up | - | スムーズに復帰 |

---

## 10. 今後の拡張

- Crouch系アニメの追加
- 座る系アニメの追加
- IKブレンドの細かい調整
- Animation Rigging連携

