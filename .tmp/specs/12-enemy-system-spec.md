# 仕様書: Enemyシステム

## 概要

敵キャラクターの状態管理、AI、ダメージ処理、ラグドール、引きずりシステムを統合した仕様。
プレイヤー/NPCとは異なる専用のEnemyControllerで管理する。

---

## 1. Enemy状態管理

### 1.1 状態enum

```csharp
public enum EnemyState
{
    Idle,      // 待機
    Patrol,    // 巡回
    Chase,     // 追跡
    Attack,    // 攻撃
    Stagger,   // ひるみ
    Dead       // 死亡
}
```

### 1.2 状態遷移図

```
┌─────────────────────────────────────────────────────────┐
│                   Enemy状態遷移図                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│              ┌──────┐                                   │
│              │ Idle │◀───────┐                         │
│              └──┬───┘        │ターゲット消失            │
│                 │            │                         │
│     プレイヤー検出            │                         │
│     (範囲10m)   │            │                         │
│                 ▼            │                         │
│              ┌───────┐       │                         │
│         ┌────│ Chase │───────┘                         │
│         │    └───┬───┘                                 │
│         │        │                                     │
│         │  攻撃範囲内(2m)                              │
│         │        │                                     │
│         │        ▼                                     │
│         │    ┌────────┐                                │
│         │    │ Attack │◀──┐                           │
│         │    └───┬────┘   │クールダウン完了            │
│         │        │        │                           │
│         │        └────────┘                           │
│         │                                              │
│    ダメージ受け                                         │
│         │                                              │
│         ▼                                              │
│    ┌─────────┐                                         │
│    │ Stagger │──────────▶ 0.5秒後に復帰               │
│    └────┬────┘                                         │
│         │                                              │
│    HP <= 0                                             │
│         │                                              │
│         ▼                                              │
│    ┌────────┐                                          │
│    │  Dead  │  ← ラグドール化、引きずり可能            │
│    └────────┘                                          │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Enemyパラメータ

### 2.1 基本パラメータ

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `enemyName` | string | 敵の名前 |
| `maxHP` | 100 | 最大HP |
| `currentHP` | 100 | 現在HP |

### 2.2 戦闘パラメータ

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `attackRange` | 2m | 攻撃可能距離 |
| `detectionRange` | 10m | プレイヤー検出範囲 |
| `attackDamage` | 10 | 攻撃ダメージ |
| `attackCooldown` | 2秒 | 攻撃間隔 |

### 2.3 物理パラメータ

| パラメータ | デフォルト値 | 説明 |
|-----------|-------------|------|
| `ragdollHPThreshold` | 0.3 (30%) | 部分ラグドール発動HP割合 |
| `knockbackForce` | 10 | ノックバック力 |

---

## 3. EnemyController クラス

```csharp
public class EnemyController : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private string enemyName;
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("戦闘設定")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("物理設定")]
    [SerializeField] private float ragdollHPThreshold = 0.3f;
    [SerializeField] private float knockbackForce = 10f;

    [Header("インタラクションポイント")]
    [SerializeField] private InteractionPoint targetPoint;
    [SerializeField] private InteractionPoint weakPoint;
    [SerializeField] private InteractionPoint dragPoint;

    [Header("エフェクト")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;

    // プロパティ
    public EnemyState State { get; private set; }
    public float HP => currentHP;
    public float HPRatio => currentHP / maxHP;
    public bool IsDead => State == EnemyState.Dead;
    public bool IsDraggable => IsDead && dragPoint != null;

    // イベント
    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;
    public event Action<float> OnDamaged;
}
```

---

## 4. インタラクションポイント

### 4.1 Enemy用ポイント

```
Enemy
├── target_point    ← 攻撃目標位置（胴体中央）
├── weak_point      ← 弱点（頭部、コア等）
└── drag_point      ← 引きずり用（足首、腕等）
    ※ 死亡後に有効化
```

### 4.2 ポイント用途

| ポイント | 用途 | 有効タイミング |
|---------|------|---------------|
| target_point | プレイヤーの攻撃目標 | 常時 |
| weak_point | クリティカルダメージ判定 | 常時 |
| drag_point | 死体引きずり | 死亡後のみ |

---

## 5. AI行動パターン

### 5.1 状態別行動

```csharp
private void ExecuteState()
{
    switch (State)
    {
        case EnemyState.Idle:
            // 待機アニメーション
            break;

        case EnemyState.Chase:
            // NavMeshAgentでプレイヤーを追跡
            _agent.SetDestination(_currentTarget.position);
            break;

        case EnemyState.Attack:
            // クールダウン完了で攻撃
            if (Time.time - _lastAttackTime > attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
            break;
    }
}
```

### 5.2 状態遷移条件

| 条件 | 遷移先 |
|------|--------|
| プレイヤー検出 (10m内) | Chase |
| 攻撃範囲内 (2m内) | Attack |
| ダメージ受け | Stagger (0.5秒) |
| HP <= 0 | Dead |
| ターゲット消失 | Idle |

---

## 6. ダメージ処理

### 6.1 ダメージフロー

```
┌─────────────────────────────────────────────────────────┐
│                  Enemyダメージフロー                     │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  魔法/攻撃ヒット                                        │
│       │                                                 │
│       ▼                                                 │
│  TakeDamage(damage, element)                           │
│       │                                                 │
│       ▼                                                 │
│  HP -= damage                                          │
│  OnHPChanged?.Invoke()                                 │
│  OnDamaged?.Invoke()                                   │
│       │                                                 │
│       ▼                                                 │
│  ヒットエフェクト生成                                   │
│       │                                                 │
│       ├─── HP > 0 ───▶ HitReaction()                  │
│       │                 ├─ State = Stagger             │
│       │                 ├─ アニメーション "Hit"         │
│       │                 ├─ NavMesh停止                 │
│       │                 └─ 0.5秒後復帰                 │
│       │                                                │
│       ├─── HP < 30% ──▶ EnablePartialRagdoll()        │
│       │                 └─ 上半身のみラグドール         │
│       │                                                │
│       └─── HP <= 0 ──▶ Die()                          │
│                         ├─ State = Dead               │
│                         ├─ NavMesh無効                │
│                         ├─ Animator無効               │
│                         ├─ 完全ラグドール化            │
│                         ├─ 死亡エフェクト              │
│                         └─ drag_point有効化           │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 6.2 TakeDamageメソッド

```csharp
public void TakeDamage(float damage, MagicElement element = MagicElement.Fire)
{
    if (IsDead) return;

    currentHP = Mathf.Max(0, currentHP - damage);
    OnHPChanged?.Invoke(currentHP, maxHP);
    OnDamaged?.Invoke(damage);

    // ヒットエフェクト
    if (hitEffectPrefab != null)
    {
        Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
    }

    // ヒットリアクション
    if (!IsDead)
    {
        StartCoroutine(HitReaction());
    }

    // HP閾値チェック
    if (HPRatio < ragdollHPThreshold && !IsDead)
    {
        EnablePartialRagdoll();
    }

    // 死亡判定
    if (currentHP <= 0)
    {
        Die();
    }
}
```

---

## 7. ラグドールシステム

### 7.1 ラグドール状態

| HP範囲 | ラグドール状態 | 説明 |
|--------|---------------|------|
| 100-30% | 無効 | 通常アニメーション |
| 30-0% | 部分有効 | 上半身のみラグドール（ぐったり感） |
| 0% (死亡) | 完全有効 | 全身ラグドール、引きずり可能 |

### 7.2 ラグドール制御

```csharp
private void SetRagdollEnabled(bool enabled)
{
    foreach (var col in _ragdollColliders)
    {
        if (col.gameObject != gameObject)
        {
            col.enabled = enabled;
        }
    }

    foreach (var rb in _ragdollRigidbodies)
    {
        if (rb.gameObject != gameObject)
        {
            rb.isKinematic = !enabled;
        }
    }

    if (_rigidbody != null)
    {
        _rigidbody.isKinematic = !enabled;
    }
}
```

---

## 8. 引きずりシステム

### 8.1 引きずりフロー

```
死亡したEnemy
     │
     ▼
drag_point有効化
     │
     │ ← プレイヤーがdrag_pointをGrab
     ▼
┌─────────────────────────────┐
│ FixedJointで接続             │
│ (プレイヤーの手 ↔ drag_point)│
└─────────────────────────────┘
     │
     │ ← プレイヤー移動
     ▼
物理演算で死体が引きずられる
```

### 8.2 DragSystemクラス

```csharp
public class DragSystem : MonoBehaviour
{
    [SerializeField] private float dragForce = 5f;
    [SerializeField] private float maxDragDistance = 3f;

    private EnemyController _dragTarget;
    private Joint _dragJoint;

    public bool StartDragging(EnemyController enemy)
    {
        if (!enemy.IsDraggable) return false;

        _dragTarget = enemy;
        _dragJoint = gameObject.AddComponent<FixedJoint>();
        _dragJoint.connectedBody = enemy.dragPoint.GetComponent<Rigidbody>();

        return true;
    }

    public void StopDragging()
    {
        if (_dragJoint != null) Destroy(_dragJoint);
        _dragTarget = null;
    }

    public bool IsDragging => _dragTarget != null;
}
```

---

## 9. 魔法システム連携

### 9.1 魔法属性

```csharp
public enum MagicElement
{
    Fire,       // 炎
    Ice,        // 氷
    Lightning,  // 雷
    Wind,       // 風
    Earth,      // 土
    Light,      // 光
    Dark        // 闘
}
```

### 9.2 魔法データ例

| 魔法名 | 属性 | 詠唱時間 | ダメージ | 特徴 |
|--------|------|---------|---------|------|
| Fireball | Fire | 1.5s | 30 | 投射、爆発 |
| Ice Lance | Ice | 1.0s | 25 | 高速、貫通 |
| Thunder | Lightning | 2.0s | 50 | 即時、範囲 |
| Wind Blade | Wind | 0.8s | 15 | 高速連射 |
| Meteor | Fire | 3.0s | 80 | 広範囲 |

### 9.3 ダメージ計算（将来拡張）

```csharp
private float CalculateDamage(float baseDamage, MagicElement element, GameObject target)
{
    float damage = baseDamage;

    // 属性相性（将来実装）
    // damage *= GetElementMultiplier(element, target);

    // クリティカル（将来実装）
    // if (Random.value < criticalChance) damage *= criticalMultiplier;

    return damage;
}
```

---

## 10. 投射物（Projectile）

### 10.1 MagicProjectileクラス

```csharp
public class MagicProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private MagicElement element;
    [SerializeField] private GameObject impactEffectPrefab;

    [Header("ホーミング")]
    [SerializeField] private bool isHoming = false;
    [SerializeField] private float homingStrength = 5f;

    private Transform _target;
    private Vector3 _direction;

    public void Initialize(Vector3 direction, Transform target = null)
    {
        _direction = direction.normalized;
        _target = target;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // ホーミング
        if (isHoming && _target != null)
        {
            Vector3 toTarget = (_target.position - transform.position).normalized;
            _direction = Vector3.Lerp(_direction, toTarget, homingStrength * Time.deltaTime);
        }

        // 移動
        transform.position += _direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(_direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        DamageSystem.Instance?.ApplyDamage(other.gameObject, damage, DamageType.Magical, element);

        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
```

---

## 11. Enemyコンポーネント構成

```
Enemy (Prefab)
├── EnemyController
│   ├── enemyName: string
│   ├── maxHP: 100
│   ├── attackRange: 2m
│   ├── detectionRange: 10m
│   ├── attackDamage: 10
│   ├── attackCooldown: 2s
│   ├── ragdollHPThreshold: 0.3
│   └── knockbackForce: 10
│
├── Rigidbody (物理演算)
├── NavMeshAgent (移動AI)
├── Animator (アニメーション)
│
├── InteractionPoints
│   ├── target_point (胴体中央)
│   ├── weak_point (頭部)
│   └── drag_point (足首) ※死亡後有効
│
├── Ragdoll Setup
│   ├── Colliders (各ボーン)
│   └── Rigidbodies (各ボーン)
│
└── Effects
    ├── hitEffectPrefab
    └── deathEffectPrefab
```

---

## 12. NPC vs Enemy 比較表

| 項目 | NPC | Enemy |
|------|-----|-------|
| 基底クラス | GameCharacter | EnemyController |
| 移動システム | CharacterNavigator | NavMeshAgent |
| 状態数 | 6 | 6 |
| 感情システム | あり | なし |
| 表情制御 | VRMExpressionController | なし |
| HP連動表情 | あり | なし |
| ラグドール | なし | あり |
| 引きずり | なし | あり |
| タグ | "Chara" | "Enemy" |
| 攻撃対象 | Enemy | Player/NPC |

---

## 13. 公開API

### 13.1 プロパティ

```csharp
public EnemyState State { get; }
public float HP { get; }
public float HPRatio { get; }
public bool IsDead { get; }
public bool IsDraggable { get; }
```

### 13.2 メソッド

```csharp
public void TakeDamage(float damage, MagicElement element = MagicElement.Fire)
public void ApplyKnockback(Vector3 direction, float force)
```

### 13.3 イベント

```csharp
public event Action<float, float> OnHPChanged;  // current, max
public event Action<float> OnDamaged;           // damage
public event Action OnDeath;
```

---

## 14. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | プレイヤー検出 (10m内) | Chase状態に遷移 |
| 2 | 攻撃範囲内 (2m内) | Attack状態、攻撃実行 |
| 3 | ダメージ受け | HP減少 + ヒットエフェクト + ひるみ |
| 4 | HP 30%以下 | 部分ラグドール発動 |
| 5 | HP 0% | 死亡 → 完全ラグドール化 |
| 6 | 死亡後drag_point | 引きずり可能 |
| 7 | 魔法ヒット | 属性ダメージ適用 |
| 8 | 範囲魔法 | 範囲内全敵にダメージ |
| 9 | ホーミング魔法 | ターゲットに追尾 |
| 10 | ノックバック | 物理演算で吹き飛ぶ |

---

## 15. 今後の拡張

| 項目 | 状態 | 備考 |
|------|------|------|
| 属性相性 | 未実装 | 弱点属性でダメージ増加 |
| クリティカル | 未実装 | weak_pointヒットで発動 |
| 敵種類別AI | 未実装 | 近接型、遠距離型、ボス等 |
| ドロップアイテム | 未実装 | 死亡時にアイテム生成 |
| 敵同士の連携 | 未実装 | グループ戦術 |

