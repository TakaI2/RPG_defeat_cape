# 仕様書: インタラクションシステム

## 概要

キャラクターとオブジェクト間のインタラクションを管理するシステム。
ターゲットポイント、属性、サイズに基づいてアクションを決定・実行する。

---

## 1. インタラクションポイント

### 1.1 キャラクター用ポイント

```
VRMキャラクター
├── InteractionPoints (Empty GameObject)
│   ├── eye_point      - Head子要素、目の前方
│   ├── mouth_point    - Head子要素、口元
│   ├── shoulder_R     - RightShoulder子要素
│   ├── shoulder_L     - LeftShoulder子要素
│   ├── hip_point      - Hips子要素
│   ├── hand_R         - RightHand子要素
│   ├── hand_L         - LeftHand子要素
│   └── foot_R/L       - 足ボーン子要素
```

### 1.2 オブジェクト用ポイント

| ポイント名 | 用途 | 配置例 |
|-----------|------|--------|
| grab_point | 掴む位置 | 取っ手、側面 |
| sit_point | 座る位置 | 座面中央 |
| touch_point | 触る位置 | ボタン面 |
| stomp_point | 踏む位置 | 上面 |
| look_point | 注目位置 | 中心 |

### 1.3 Enemy用ポイント

| ポイント名 | 用途 | 配置例 |
|-----------|------|--------|
| target_point | 攻撃目標 | 胴体中央 |
| weak_point | 弱点 | 頭部、コア |
| drag_point | 引きずり | 足首、腕 |

---

## 2. InteractionPoint クラス

```csharp
public enum InteractionPointType
{
    // キャラクター用
    Eye,
    Mouth,
    Shoulder,
    Hip,
    Hand,
    Foot,

    // オブジェクト用
    Grab,
    Sit,
    Touch,
    Stomp,
    Look,

    // Enemy用
    Target,
    WeakPoint,
    Drag
}

public class InteractionPoint : MonoBehaviour
{
    [Header("設定")]
    public InteractionPointType pointType;
    public bool isActive = true;

    [Header("オフセット")]
    public Vector3 approachOffset;  // アプローチ時のオフセット
    public Vector3 handRotation;    // 手のIK回転

    [Header("サイズ（Gizmo表示用）")]
    public float radius = 0.05f;

    /// <summary>
    /// このポイントへのワールド座標を取得
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// アプローチ位置を取得（キャラクターが立つべき位置）
    /// </summary>
    public Vector3 GetApproachPosition()
    {
        return transform.TransformPoint(approachOffset);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private Color GetGizmoColor()
    {
        return pointType switch
        {
            InteractionPointType.Eye => Color.cyan,
            InteractionPointType.Mouth => Color.red,
            InteractionPointType.Hand => Color.yellow,
            InteractionPointType.Grab => Color.green,
            InteractionPointType.Target => Color.red,
            _ => Color.white
        };
    }
}
```

---

## 3. Interactable クラス

```csharp
[Flags]
public enum InteractableAttribute
{
    None = 0,
    Grabbable = 1 << 0,    // 掴める
    Eatable = 1 << 1,      // 食べられる
    Sittable = 1 << 2,     // 座れる
    Touchable = 1 << 3,    // 触れる
    Stompable = 1 << 4,    // 踏める
    Talkable = 1 << 5,     // 話せる
    Kissable = 1 << 6,     // キスできる
    Huggable = 1 << 7      // 抱きしめられる
}

public enum SizeCategory
{
    Tiny,      // < 0.1m  - 摘む
    Small,     // 0.1-0.3m - 片手
    Medium,    // 0.3-0.8m - 両手
    Large      // > 0.8m  - 特殊
}

public class Interactable : MonoBehaviour
{
    [Header("属性")]
    public InteractableAttribute attributes;

    [Header("サイズ")]
    public SizeCategory sizeCategory = SizeCategory.Small;
    public bool autoDetectSize = true;

    [Header("インタラクションポイント")]
    public List<InteractionPoint> interactionPoints;

    [Header("状態")]
    public bool isBeingHeld = false;
    public GameCharacter heldBy = null;

    private void Start()
    {
        if (autoDetectSize)
        {
            DetectSize();
        }
        CollectInteractionPoints();
    }

    /// <summary>
    /// Boundsからサイズカテゴリを自動判定
    /// </summary>
    private void DetectSize()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        sizeCategory = maxDimension switch
        {
            < 0.1f => SizeCategory.Tiny,
            < 0.3f => SizeCategory.Small,
            < 0.8f => SizeCategory.Medium,
            _ => SizeCategory.Large
        };
    }

    /// <summary>
    /// 子要素のInteractionPointを収集
    /// </summary>
    private void CollectInteractionPoints()
    {
        interactionPoints = new List<InteractionPoint>(
            GetComponentsInChildren<InteractionPoint>()
        );
    }

    /// <summary>
    /// 指定タイプのポイントを取得
    /// </summary>
    public InteractionPoint GetPoint(InteractionPointType type)
    {
        return interactionPoints.Find(p => p.pointType == type);
    }

    /// <summary>
    /// 指定属性を持っているか
    /// </summary>
    public bool HasAttribute(InteractableAttribute attr)
    {
        return (attributes & attr) != 0;
    }
}
```

---

## 4. サイズによる挙動変化

### 4.1 Grab（掴む）

| サイズ | 挙動 | IK設定 |
|--------|------|--------|
| Tiny | 指で摘む | RightHand + 指IK |
| Small | 片手で持つ | RightHand IK |
| Medium | 両手で持つ | BothHands IK |
| Large | 押す/引く | BothHands + Body |

### 4.2 Eat（食べる）

| サイズ | 挙動 | IK設定 |
|--------|------|--------|
| Tiny | 掴んで口へ | Hand→Mouth IK |
| Small | 掴んで口へ | Hand→Mouth IK |
| Medium | 顔を近づける | Head IK→Object |
| Large | かぶりつく | Head IK + 口開き |

---

## 5. インタラクション判定フロー

```
┌─────────────────────────────────────────────────────────┐
│              インタラクション判定フロー                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ターゲット選択                                         │
│       │                                                 │
│       ▼                                                 │
│  ┌──────────────┐                                       │
│  │Interactable? │                                       │
│  └──────┬───────┘                                       │
│         │                                               │
│    Yes  │  No                                           │
│    ┌────┴────┐                                          │
│    ▼         ▼                                          │
│ 属性判定   タグ判定                                     │
│    │         │                                          │
│    ▼         ▼                                          │
│ ┌──────┐  ┌──────┐                                     │
│ │属性に │  │Enemy?│──Yes──▶ Combat判定                 │
│ │応じた │  └──┬───┘                                     │
│ │Action │     │No                                       │
│ └──┬───┘     ▼                                         │
│    │     ┌──────┐                                       │
│    │     │Chara?│──Yes──▶ Talk                         │
│    │     └──┬───┘                                       │
│    │        │No                                         │
│    │        ▼                                           │
│    │     Magic（地面へ）                                │
│    │                                                    │
│    ▼                                                    │
│ サイズ判定                                              │
│    │                                                    │
│    ▼                                                    │
│ IK/アニメ設定                                           │
│    │                                                    │
│    ▼                                                    │
│ Action実行                                              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 6. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | Tinyオブジェクトをgrab | 指で摘むアニメ |
| 2 | Mediumオブジェクトをgrab | 両手で持つアニメ |
| 3 | Largeオブジェクトをeat | 顔を近づけてかぶりつく |
| 4 | sittable椅子をクリック | sit_pointに座る |
| 5 | NPCのeye_pointにLookAt | 目を見て話す |
