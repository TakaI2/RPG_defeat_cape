# 仕様書: マウス入力・操作システム

## 概要

マウス操作でキャラクターの移動とアクションを制御するシステム。

## 機能要件

### 1. 右クリック: 移動

```
右クリック → Raycast → 地面判定 → NavMesh移動
```

| 条件 | 動作 |
|------|------|
| 地面をクリック | その位置へ移動開始 |
| 移動不可能領域 | 最寄りの移動可能点へ |
| 移動中に再クリック | 新しい目的地へ変更 |

### 2. 左クリック: アクション

```
左クリック → Raycast → オブジェクト判定 → タグ/属性判定 → アクション実行
```

| ターゲット | 判定 | アクション |
|-----------|------|-----------|
| Enemy（近距離） | Distance < 2m | Attack |
| Enemy（遠距離） | Distance >= 2m | Magic |
| タグなし（地面等） | - | Magic（その位置へ） |
| Object + grabbable | - | Grab |
| Object + touchable | - | Touch |
| Object + sittable | - | Sit |
| Chara/Player | - | Talk |

### 3. ホバー検出

```
毎フレームRaycast → ターゲット情報更新 → UI表示
```

| 検出内容 | UI表示 |
|---------|--------|
| Enemy | 赤ハイライト + HP表示 |
| Interactable | アイコン表示 |
| NPC | 名前表示 |
| 何もなし | 通常カーソル |

## クラス設計

```csharp
public class MouseInputController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameCharacter controlledCharacter;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactableLayer;

    [Header("設定")]
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private float meleeRange = 2f;

    // 現在のホバーターゲット
    public GameObject CurrentHoverTarget { get; private set; }
    public Interactable CurrentInteractable { get; private set; }

    // イベント
    public event Action<Vector3> OnMoveCommand;
    public event Action<GameObject, ActionType> OnActionCommand;
    public event Action<GameObject> OnHoverChanged;

    private void Update()
    {
        UpdateHover();
        HandleInput();
    }

    private void UpdateHover() { /* Raycastでホバー更新 */ }
    private void HandleInput() { /* クリック処理 */ }

    private ActionType DetermineAction(GameObject target)
    {
        // タグ・属性・距離からアクション決定
    }
}
```

## 入力フロー図

```
┌─────────────────────────────────────────────────────────┐
│                    マウス入力                           │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  右クリック                    左クリック               │
│      │                            │                     │
│      ▼                            ▼                     │
│  ┌────────┐                  ┌────────┐                │
│  │Raycast │                  │Raycast │                │
│  │(Ground)│                  │(All)   │                │
│  └───┬────┘                  └───┬────┘                │
│      │                            │                     │
│      ▼                            ▼                     │
│  ┌────────┐              ┌──────────────┐              │
│  │NavMesh │              │ターゲット判定 │              │
│  │移動開始│              └───────┬──────┘              │
│  └────────┘                      │                     │
│                    ┌─────────────┼─────────────┐       │
│                    ▼             ▼             ▼       │
│               ┌────────┐   ┌────────┐   ┌────────┐    │
│               │ Enemy  │   │ Object │   │ Chara  │    │
│               └───┬────┘   └───┬────┘   └───┬────┘    │
│                   │            │            │          │
│              ┌────┴────┐   属性判定      Talk        │
│              │距離判定  │       │                      │
│              └────┬────┘       ▼                      │
│            ┌─────┴─────┐   Grab/Touch/                │
│            ▼           ▼   Sit/Eat/Stomp              │
│         Attack      Magic                              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## 依存関係

- CharacterNavigator（移動）
- TargetingSystem（ターゲット判定）
- ActionSystem（アクション実行）

## テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | 地面を右クリック | その位置へ移動 |
| 2 | 敵を近距離で左クリック | Attack実行 |
| 3 | 敵を遠距離で左クリック | Magic実行 |
| 4 | grabbableオブジェクトを左クリック | Grab実行 |
| 5 | NPCを左クリック | Talk実行 |
| 6 | 何もない場所を左クリック | Magic（その位置へ） |
