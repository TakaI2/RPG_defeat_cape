# 仕様書: NPC動作システム

## 概要

NPCの自律行動、状態管理、感情システムを統合したAI動作仕様。
プレイヤーと同じ`GameCharacter`基盤を使用し、追加の`NPCBehaviorController`で自律行動を実現。

---

## 1. NPC状態遷移

### 1.1 状態一覧

```csharp
public enum NPCState
{
    Idle,       // 待機
    Patrol,     // 巡回
    Follow,     // 追従
    Combat,     // 戦闘
    Interact,   // インタラクション中
    Flee        // 逃走
}
```

### 1.2 状態遷移図

```
┌─────────────────────────────────────────────────────────┐
│                    NPC状態遷移図                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│                    ┌──────┐                             │
│         ┌─────────│ Idle │◀────────┐                   │
│         │         └──┬───┘         │                   │
│         │            │             │敵消滅              │
│    パトロール開始     │敵検出        │                   │
│         │            ▼             │                   │
│         ▼      ┌──────────┐       │                   │
│    ┌────────┐  │  Combat  │───────┘                   │
│    │ Patrol │  └────┬─────┘                           │
│    └────────┘       │                                  │
│                     │HP < 20%                          │
│                     ▼                                  │
│               ┌──────────┐                             │
│               │   Flee   │                             │
│               └──────────┘                             │
│                                                         │
│    追従命令                                             │
│         │                                               │
│         ▼                                               │
│    ┌────────┐                                          │
│    │ Follow │  ← プレイヤーに追従                      │
│    └────────┘                                          │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 2. 状態別行動詳細

### 2.1 Idle（待機）

```csharp
private void ExecuteIdle()
{
    _stateTimer += Time.deltaTime;

    // たまに周囲を見回す
    if (_stateTimer > 3f)
    {
        LookAround();
        _stateTimer = 0f;
    }
}
```

| パラメータ | 値 | 説明 |
|-----------|-----|------|
| 見回し間隔 | 3秒 | ランダムな方向を見る |

### 2.2 Patrol（巡回）

```csharp
private void ExecutePatrol()
{
    if (patrolPoints.Length == 0) return;

    var target = patrolPoints[_currentPatrolIndex];

    if (!character.Navigator.IsMoving)
    {
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist < 0.5f)
        {
            // 到着、次のポイントへ
            _stateTimer += Time.deltaTime;
            if (_stateTimer > patrolWaitTime)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
                _stateTimer = 0f;
            }
        }
        else
        {
            character.Navigator.MoveTo(target.position);
        }
    }
}
```

| パラメータ | 値 | 説明 |
|-----------|-----|------|
| patrolWaitTime | 2秒 | 各ポイントでの待機時間 |
| 到着判定距離 | 0.5m | この距離以内で到着とみなす |

### 2.3 Follow（追従）

```csharp
private void ExecuteFollow()
{
    if (followTarget == null) return;

    float dist = Vector3.Distance(transform.position, followTarget.position);
    if (dist > followDistance)
    {
        character.Navigator.MoveTo(followTarget.position);
    }
}
```

| パラメータ | 値 | 説明 |
|-----------|-----|------|
| followDistance | 2m | この距離を超えたら追従開始 |

### 2.4 Combat（戦闘）

```csharp
private void ExecuteCombat()
{
    var enemies = DetectEnemies();
    if (enemies.Count == 0)
    {
        TransitionTo(defaultState);
        return;
    }

    var target = enemies[0];
    float dist = Vector3.Distance(transform.position, target.transform.position);

    if (dist < 2f)
    {
        // 近接攻撃
        executor?.TryExecuteAction("Attack", context);
    }
    else if (dist < combatRange)
    {
        // 魔法攻撃
        executor?.TryExecuteAction("Magic", context);
    }
    else
    {
        // 接近
        character.Navigator.MoveTo(target.transform.position);
    }
}
```

| 距離 | アクション | 説明 |
|------|-----------|------|
| < 2m | Attack | 近接攻撃 |
| 2m - 5m | Magic | 魔法攻撃 |
| >= 5m | 移動 | 敵に接近 |

### 2.5 Flee（逃走）

```csharp
private void ExecuteFlee()
{
    var enemies = DetectEnemies();
    if (enemies.Count > 0)
    {
        // 敵の反対方向へ逃げる
        Vector3 fleeDir = transform.position - enemies[0].transform.position;
        Vector3 fleePos = transform.position + fleeDir.normalized * 10f;
        character.Navigator.MoveTo(fleePos);
    }
}
```

| 発動条件 | 行動 |
|---------|------|
| HP < 20% かつ Combat中 | 敵の反対方向へ10m逃走 |

---

## 3. 状態遷移条件

```csharp
private void CheckStateTransitions()
{
    // 敵検出
    var nearbyEnemies = DetectEnemies();
    if (nearbyEnemies.Count > 0 && CurrentState != NPCState.Combat)
    {
        TransitionTo(NPCState.Combat);
        return;
    }

    // HP低下で逃走
    if (character.HPRatio < 0.2f && CurrentState == NPCState.Combat)
    {
        TransitionTo(NPCState.Flee);
        return;
    }
}
```

| 条件 | 遷移先 | 優先度 |
|------|--------|--------|
| 敵検出 (範囲10m) | Combat | 高 |
| HP < 20% (Combat中) | Flee | 高 |
| 敵消滅 | defaultState | 中 |

---

## 4. 敵検出システム

```csharp
private List<GameObject> DetectEnemies()
{
    var enemies = new List<GameObject>();
    var colliders = Physics.OverlapSphere(transform.position, detectionRange);

    foreach (var col in colliders)
    {
        if (col.CompareTag("Enemy"))
        {
            enemies.Add(col.gameObject);
        }
    }

    return enemies;
}
```

| パラメータ | 値 | 説明 |
|-----------|-----|------|
| detectionRange | 10m | 敵検出範囲 |
| combatRange | 5m | 魔法攻撃可能範囲 |

---

## 5. NPCが実行可能なアクション

### 5.1 アクション一覧

| カテゴリ | アクション | 対象 | 説明 |
|---------|-----------|------|------|
| **戦闘** | Attack | Enemy | 近接攻撃 + 怒り表情 |
| | Magic | 地面/Enemy | 詠唱 + RFX4エフェクト |
| **日常** | Grab | Grabbable | サイズに応じた掴み方 |
| | Eat | Eatable | 口へ運ぶ/かぶりつく |
| | Sit | Sittable | 座る + リラックス表情 |
| | Touch | Touchable | ボタン押し等 |
| | Stomp | Stompable | 踏みつける |
| **感情** | Talk | Chara/Player | 視線合わせ + 口パク |
| | Kiss | Chara | 肩に手 + 顔を近づける |
| | Hug | Chara | 両腕で抱きしめる |

### 5.2 アクション優先度

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

## 6. 感情システム

### 6.1 感情enum

```csharp
public enum Emotion
{
    Neutral,      // 通常
    Joy,          // 喜び
    Anger,        // 怒り
    Sadness,      // 悲しみ
    Fear,         // 恐怖
    Surprise,     // 驚き
    Disgust,      // 嫌悪
    Trust,        // 信頼
    Anticipation  // 期待
}
```

### 6.2 感情トリガー

| トリガー | 感情 | 強度 | 表情プリセット |
|---------|------|------|---------------|
| 戦闘勝利 | Joy | +0.5 | happy |
| ダメージ受け | Anger | +0.3 | angry |
| HP低下 | Fear | +0.2 | shocked |
| 成功 | Joy | +0.3 | happy |
| 失敗 | Sadness | +0.3 | sad |
| 強敵遭遇 | Fear | +0.4 | shocked |
| 仲間支援 | Trust | +0.2 | smile |

### 6.3 感情→表情マッピング

```csharp
private static readonly Dictionary<Emotion, string> EmotionPresetMap = new()
{
    { Emotion.Neutral, "neutral" },
    { Emotion.Joy, "happy" },
    { Emotion.Anger, "furious" },
    { Emotion.Sadness, "dejected" },
    { Emotion.Fear, "shocked" },
    { Emotion.Surprise, "confused" },
    { Emotion.Disgust, "grumpy" },
    { Emotion.Trust, "smile" },
    { Emotion.Anticipation, "expectant" }
};
```

### 6.4 感情減衰

```csharp
private void DecayEmotions()
{
    foreach (var emotion in _emotionValues.Keys.ToList())
    {
        if (emotion != baseEmotion)
        {
            _emotionValues[emotion] = Mathf.Max(0,
                _emotionValues[emotion] - emotionDecayRate * Time.deltaTime);
        }
    }
}
```

| パラメータ | 値 | 説明 |
|-----------|-----|------|
| emotionDecayRate | 0.1/秒 | 感情値の減衰速度 |
| baseEmotion | Neutral | デフォルト感情（減衰しない） |

---

## 7. HP連動表情

```
┌───────────────────────────────────────────┐
│              HP連動表情                    │
├───────────────────────────────────────────┤
│                                           │
│  HP 100-70%: 通常表情                     │
│  ┌─────────────────────────────┐         │
│  │ Expression: neutral         │         │
│  │ Blend: なし                 │         │
│  └─────────────────────────────┘         │
│                                           │
│  HP 70-40%: やや辛そう                    │
│  ┌─────────────────────────────┐         │
│  │ Expression: sad (0.2)       │         │
│  │ Blend: neutral + sad        │         │
│  └─────────────────────────────┘         │
│                                           │
│  HP 40-20%: 苦しそう                      │
│  ┌─────────────────────────────┐         │
│  │ Expression: sad (0.4)       │         │
│  │            angry (0.2)      │         │
│  │ Blend: sad + angry          │         │
│  └─────────────────────────────┘         │
│                                           │
│  HP 20-0%: 瀕死                           │
│  ┌─────────────────────────────┐         │
│  │ Expression: sad (0.6)       │         │
│  │            blink (0.3)      │         │
│  │ Blend: sad + 目を細める     │         │
│  └─────────────────────────────┘         │
│                                           │
└───────────────────────────────────────────┘
```

---

## 8. NPCコンポーネント構成

```
NPCキャラクター (Prefab)
├── GameCharacter
│   ├── CharacterType: NPC
│   ├── characterName: string
│   ├── maxHP: 100
│   └── VRM制御コンポーネント参照
│
├── VRM Model
│   ├── VRMExpressionController (表情制御)
│   ├── VRMAnimationController (アニメーション)
│   ├── VRMFinalIKController (IK制御)
│   └── VRMEyeGazeController (視線制御)
│
├── InteractionPoints
│   ├── eye_point      (Head子要素)
│   ├── mouth_point    (Head子要素)
│   ├── shoulder_R     (RightShoulder子要素)
│   ├── shoulder_L     (LeftShoulder子要素)
│   ├── hip_point      (Hips子要素)
│   ├── hand_R         (RightHand子要素)
│   ├── hand_L         (LeftHand子要素)
│   ├── foot_R         (RightFoot子要素)
│   └── foot_L         (LeftFoot子要素)
│
├── CharacterNavigator + NavMeshAgent (移動制御)
├── ActionExecutor (アクション実行)
├── EmotionSystem (感情管理)
│
└── NPCBehaviorController (NPC専用AI)
    ├── defaultState: NPCState.Idle
    ├── detectionRange: 10m
    ├── combatRange: 5m
    ├── patrolPoints: Transform[]
    ├── patrolWaitTime: 2秒
    ├── followTarget: Transform
    └── followDistance: 2m
```

---

## 9. 公開API

### 9.1 NPCBehaviorController

```csharp
// 追従ターゲットを設定
public void SetFollowTarget(Transform target)

// パトロールを開始
public void StartPatrol()

// 現在の状態
public NPCState CurrentState { get; }
```

### 9.2 GameCharacter

```csharp
// ダメージを受ける
public void TakeDamage(float damage)

// HP回復
public void Heal(float amount)

// 感情を設定
public void SetEmotion(Emotion emotion)

// インタラクションポイント取得
public InteractionPoint GetInteractionPoint(InteractionPointType type)

// イベント
public event Action<float, float> OnHPChanged;
public event Action<Emotion> OnEmotionChanged;
public event Action OnDeath;
```

---

## 10. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | NPC生成 | 全コンポーネント正常初期化 |
| 2 | Idle状態3秒経過 | 周囲を見回す |
| 3 | Patrol開始 | ポイント間を巡回 |
| 4 | 敵検出 (範囲内) | Combat状態に遷移 |
| 5 | 敵との距離 < 2m | 近接攻撃実行 |
| 6 | 敵との距離 2-5m | 魔法攻撃実行 |
| 7 | HP < 20% (Combat中) | Flee状態に遷移 |
| 8 | 感情Joy追加 | happy表情に変化 |
| 9 | HP 50% | sad(0.2)表情ブレンド |
| 10 | SetFollowTarget呼び出し | Follow状態に遷移 |

---

## 11. 今後の拡張

| 項目 | 状態 | 備考 |
|------|------|------|
| スケジュール行動 | 未定義 | 時間帯別行動 |
| 関係性システム | 未定義 | 好感度・信頼度 |
| 対話AI | 未定義 | 会話内容の動的生成 |
| グループ行動 | 未定義 | 複数NPCの連携 |
| 記憶システム | 未定義 | 過去の出来事を記憶 |

