# 仕様書: 魔法/スキルシステム共有設計

## 概要

Player/Enemy間で魔法・スキルシステムを共有する設計方針。
同じMagicData/SkillData、エフェクト、ダメージ処理を流用し、開発効率を最大化する。

---

## 1. 現在の実装状況

### 1.1 既存ファイル

| ファイル | 状態 | 内容 |
|---------|------|------|
| `Projectile.cs` | シンプル | 速度・寿命のみ。ダメージ値なし |
| `PlayerAttack.cs` | シンプル | 最寄りの敵を自動狙い、Projectile生成 |
| `EnemyController.cs` | シンプル | 移動+Die()のみ。HP・攻撃なし |
| `SkillData.cs` | **充実** | アニメ、ダメージ、表情、エフェクト、コンボ等 |

### 1.2 SkillData.cs の機能一覧

```csharp
// 既に実装済みの機能
- skillName, description, icon          // 基本情報
- skillType (Light/Heavy/Skill/Ultimate) // スキル種類
- animation, animationSpeed, animationTrigger  // アニメーション
- baseDamage, damageMultiplier          // ダメージ
- criticalRate, criticalMultiplier      // クリティカル
- knockbackForce                        // ノックバック
- cooldown, cost                        // クールダウン・コスト
- expressionName, expressionWeight      // VRM表情
- effectPrefab, effectTiming, effectOffset  // エフェクト
- hitEffectPrefab, hitShakeIntensity    // ヒットエフェクト
- activationSound, hitSound, soundVolume // サウンド
- useCameraCutIn, cutInData             // カメラ演出
- nextComboSkill, comboWindow           // コンボ
```

---

## 2. 共有設計アーキテクチャ

### 2.1 全体図

```
┌─────────────────────────────────────────────────────────┐
│            魔法/スキルシステムの共有設計                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│                   SkillData / MagicData                 │
│                   (ScriptableObject)                    │
│                          │                              │
│            ┌─────────────┴─────────────┐               │
│            ▼                           ▼               │
│      ┌──────────┐                ┌──────────┐         │
│      │  Player  │                │  Enemy   │         │
│      │          │                │          │         │
│      │ 同じデータ│                │ 同じデータ│         │
│      │ を参照   │                │ を参照   │         │
│      └────┬─────┘                └────┬─────┘         │
│           │                           │                │
│           ▼                           ▼                │
│      ┌──────────────────────────────────────┐         │
│      │         共通エフェクト (RFX4)         │         │
│      │    ・chargeEffectPrefab (詠唱)       │         │
│      │    ・mainEffectPrefab (投射物)       │         │
│      │    ・impactEffectPrefab (着弾)       │         │
│      └──────────────────────────────────────┘         │
│                          │                              │
│                          ▼                              │
│      ┌──────────────────────────────────────┐         │
│      │         DamageSystem (共通)           │         │
│      │    ・ダメージ計算                     │         │
│      │    ・属性相性                         │         │
│      │    ・ダメージ数値表示                 │         │
│      └──────────────────────────────────────┘         │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 2.2 共有可能な項目

| 項目 | Player | Enemy | 共有可能か |
|------|--------|-------|-----------|
| **MagicData/SkillData** | 参照 | 参照 | **共有可能** |
| **エフェクトPrefab** | 同じPrefab | 同じPrefab | **共有可能** |
| **DamageSystem** | 呼び出し | 呼び出し | **共有可能** |
| **Projectile** | 生成 | 生成 | **共有可能** |
| **発動ロジック** | ActionExecutor経由 | EnemyController経由 | 別実装 |

---

## 3. MagicData ScriptableObject

### 3.1 定義（仕様）

```csharp
public enum MagicElement
{
    Fire,       // 炎
    Ice,        // 氷
    Lightning,  // 雷
    Wind,       // 風
    Earth,      // 土
    Light,      // 光
    Dark        // 闇
}

[CreateAssetMenu(fileName = "NewMagic", menuName = "RPG/Magic Data")]
public class MagicData : ScriptableObject
{
    [Header("基本情報")]
    public string magicName;
    public string description;
    public Sprite icon;
    public MagicElement element;

    [Header("性能")]
    public float baseDamage = 20f;
    public float castTime = 1.5f;
    public float cooldown = 2f;
    public float manaCost = 10f;

    [Header("エフェクト")]
    public GameObject chargeEffectPrefab;  // 詠唱エフェクト
    public GameObject mainEffectPrefab;    // メインエフェクト（投射物など）
    public GameObject impactEffectPrefab;  // 着弾エフェクト

    [Header("オプション")]
    public bool isInstant = false;         // 即時ダメージか
    public float areaOfEffect = 0f;        // 範囲（0なら単体）
    public float projectileSpeed = 20f;
}
```

### 3.2 魔法データ例

| 魔法名 | 属性 | 詠唱時間 | ダメージ | 特徴 |
|--------|------|---------|---------|------|
| Fireball | Fire | 1.5s | 30 | 投射、爆発 |
| Ice Lance | Ice | 1.0s | 25 | 高速、貫通 |
| Thunder | Lightning | 2.0s | 50 | 即時、範囲 |
| Wind Blade | Wind | 0.8s | 15 | 高速連射 |
| Meteor | Fire | 3.0s | 80 | 広範囲 |

---

## 4. 発動フロー比較

### 4.1 Player魔法発動

```
┌─────────────────────────────────────────────────────────┐
│                    Player魔法発動                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  入力検出 → ActionExecutor → MagicAction.Execute()      │
│                                    │                    │
│                                    ▼                    │
│                         ┌──────────────────┐           │
│                         │ 詠唱アニメーション │           │
│                         │ 詠唱エフェクト     │           │
│                         │ 表情変化 (VRM)    │           │
│                         │ IK制御 (手を前へ)  │           │
│                         └────────┬─────────┘           │
│                                  │                      │
│                                  ▼                      │
│                         Projectile生成 or 即時ダメージ   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 4.2 Enemy魔法発動

```
┌─────────────────────────────────────────────────────────┐
│                    Enemy魔法発動                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  AI判定 → EnemyController → PerformMagicAttack()        │
│                                    │                    │
│                                    ▼                    │
│                         ┌──────────────────┐           │
│                         │ 詠唱アニメーション │           │
│                         │ 詠唱エフェクト     │  ← 共通  │
│                         │ (表情なし)        │           │
│                         │ (IKなし)          │           │
│                         └────────┬─────────┘           │
│                                  │                      │
│                                  ▼                      │
│                         Projectile生成 or 即時ダメージ   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 4.3 機能差分

| 機能 | Player | Enemy | 備考 |
|------|--------|-------|------|
| 発動トリガー | 入力 + ActionExecutor | AI判定 | 異なる |
| アニメーション | VRM + Mixamoリターゲット | 専用アニメ | 異なる |
| 表情変化 | あり (VRMExpressionController) | なし | Player専用 |
| IK制御 | あり (VRMFinalIKController) | なし | Player専用 |
| エフェクト | **共通** | **共通** | 同じPrefab |
| ダメージ計算 | **共通** | **共通** | DamageSystem |
| Projectile | **共通** | **共通** | 同じクラス |

---

## 5. 共通コンポーネント

### 5.1 DamageSystem

```csharp
public class DamageSystem : MonoBehaviour
{
    public static DamageSystem Instance { get; private set; }

    /// <summary>
    /// ダメージを与える（Player/Enemy共通）
    /// </summary>
    public void ApplyDamage(GameObject target, float damage,
        DamageType type = DamageType.Physical,
        MagicElement element = MagicElement.Fire,
        Vector3? hitPoint = null)
    {
        // 敵へのダメージ
        var enemy = target.GetComponent<EnemyController>();
        if (enemy != null)
        {
            float finalDamage = CalculateDamage(damage, type, element, target);
            enemy.TakeDamage(finalDamage, element);
            ShowDamageNumber(hitPoint ?? target.transform.position, finalDamage);
            return;
        }

        // キャラクターへのダメージ
        var character = target.GetComponent<GameCharacter>();
        if (character != null)
        {
            float finalDamage = CalculateDamage(damage, type, element, target);
            character.TakeDamage(finalDamage);
            ShowDamageNumber(hitPoint ?? target.transform.position, finalDamage);
        }
    }

    /// <summary>
    /// 範囲ダメージ（Player/Enemy共通）
    /// </summary>
    public void ApplyAreaDamage(Vector3 center, float radius, float damage,
        DamageType type = DamageType.Magical,
        MagicElement element = MagicElement.Fire)
    {
        var colliders = Physics.OverlapSphere(center, radius);
        foreach (var col in colliders)
        {
            ApplyDamage(col.gameObject, damage, type, element);
        }
    }
}
```

### 5.2 MagicProjectile（共通）

```csharp
public class MagicProjectile : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private MagicElement element;
    [SerializeField] private GameObject impactEffectPrefab;

    [Header("ホーミング")]
    [SerializeField] private bool isHoming = false;
    [SerializeField] private float homingStrength = 5f;

    // 発射者のタグ（自分には当たらない）
    private string _ownerTag;

    public void Initialize(Vector3 direction, Transform target, float dmg, string ownerTag)
    {
        _direction = direction.normalized;
        _target = target;
        if (dmg > 0) damage = dmg;
        _ownerTag = ownerTag;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 発射者には当たらない
        if (other.CompareTag(_ownerTag)) return;

        // ダメージ適用（共通システム）
        DamageSystem.Instance?.ApplyDamage(
            other.gameObject,
            damage,
            DamageType.Magical,
            element,
            transform.position
        );

        // 着弾エフェクト
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
```

---

## 6. Enemy専用で必要なもの

### 6.1 アニメーション

```
┌─────────────────────────────────────────────────────────┐
│              Enemy専用アニメーション一覧                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  【必須】                                               │
│  ├── Idle       ← 待機                                 │
│  ├── Walk/Run   ← 移動                                 │
│  ├── Attack     ← 攻撃 (近接)                          │
│  ├── Cast       ← 魔法詠唱                             │
│  ├── Hit        ← 被ダメージリアクション                │
│  └── Death      ← 死亡                                 │
│                                                         │
│  【推奨】                                               │
│  ├── Chase      ← 追跡 (Walk/Runと共用可)              │
│  └── Stagger    ← ひるみ (Hit共用可)                   │
│                                                         │
│  【入手方法】                                           │
│  ├── Mixamo (無料)                                     │
│  │   検索: "zombie attack", "monster", "cast spell"    │
│  ├── Unity Asset Store                                 │
│  │   敵キャラパック購入                                │
│  └── 自作 (Blenderなど)                                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 6.2 Mixamo検索キーワード

| 用途 | 検索キーワード |
|------|---------------|
| 待機 | idle, breathing idle |
| 移動 | walk, run, zombie walk |
| 近接攻撃 | attack, punch, claw attack |
| 魔法詠唱 | casting, magic, spell |
| 被ダメージ | hit reaction, impact |
| 死亡 | death, dying, falling |

### 6.3 Enemy発動ロジック

```csharp
// EnemyController拡張（新規実装）
public class EnemyMagicController : MonoBehaviour
{
    [SerializeField] private MagicData[] availableMagics;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;

    private float _lastCastTime;

    /// <summary>
    /// 魔法攻撃実行
    /// </summary>
    public IEnumerator CastMagic(MagicData magic, Transform target)
    {
        // 1. 詠唱アニメーション
        animator?.SetTrigger("Cast");

        // 2. 詠唱エフェクト（共通）
        if (magic.chargeEffectPrefab != null)
        {
            Instantiate(magic.chargeEffectPrefab, firePoint.position, firePoint.rotation);
        }

        // 3. 詠唱待機
        yield return new WaitForSeconds(magic.castTime);

        // 4. 投射物生成（共通Projectile）
        if (magic.mainEffectPrefab != null)
        {
            var proj = Instantiate(magic.mainEffectPrefab, firePoint.position, firePoint.rotation);
            var projScript = proj.GetComponent<MagicProjectile>();

            Vector3 direction = (target.position - firePoint.position).normalized;
            projScript?.Initialize(direction, target, magic.baseDamage, "Enemy");
        }

        _lastCastTime = Time.time;
    }
}
```

---

## 7. 実装優先度

```
┌─────────────────────────────────────────────────────────┐
│                   推奨実装優先度                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  【Phase 1】既存活用（すぐにできる）                    │
│  ──────────────────────────────────────                 │
│  ☑ SkillData.cs をEnemy攻撃にも流用                   │
│  ☑ 同じエフェクトPrefabを共有                         │
│  ☑ DamageSystemで統一ダメージ処理                     │
│  ☑ MagicProjectileを共通化                            │
│                                                         │
│  【Phase 2】Enemy基盤強化                               │
│  ──────────────────────────────────────                 │
│  □ EnemyController拡張（HP、状態管理）                 │
│  □ Enemyアニメーション追加（Mixamoから）               │
│  □ EnemyMagicController実装（MagicData参照）           │
│                                                         │
│  【Phase 3】高度な機能                                  │
│  ──────────────────────────────────────                 │
│  □ 属性相性システム                                    │
│  □ ボスAI（特殊パターン）                             │
│  □ 敵専用スキル/魔法                                   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 8. エフェクト共有例

### 8.1 KriptoFX (RFX4) 設定

```
Player用:
┌──────────────────────────────────────┐
│ RFX4_EffectEvent                     │
│ ├─ CharacterEffect: 詠唱エフェクト  │
│ ├─ MainEffect: 投射物              │
│ └─ Target: Enemy                    │
└──────────────────────────────────────┘

Enemy用:
┌──────────────────────────────────────┐
│ 同じPrefabを使用                     │
│ ├─ CharacterEffect: 詠唱エフェクト  │  ← 共通
│ ├─ MainEffect: 投射物              │  ← 共通
│ └─ Target: Player                   │  ← ターゲット変更のみ
└──────────────────────────────────────┘
```

### 8.2 エフェクト流用パターン

| エフェクト種類 | Player使用 | Enemy使用 | 共有 |
|---------------|------------|-----------|------|
| 炎魔法エフェクト | Fireball発射 | Fireball発射 | 可能 |
| 雷魔法エフェクト | Thunder詠唱 | Thunder詠唱 | 可能 |
| ヒットエフェクト | 敵にヒット時 | Playerにヒット時 | 可能 |
| 詠唱エフェクト | 手元に表示 | 手元に表示 | 可能 |

---

## 9. まとめ

### 9.1 質問への回答

| 質問 | 回答 |
|------|------|
| Playerの魔法を流用できる？ | **できる**。MagicData/SkillData、エフェクト、DamageSystemは共通利用可能 |
| 専用エフェクトは必要？ | **不要**。同じPrefabを共有可能 |
| 専用プログラムは必要？ | **一部必要**。Enemy発動ロジック（EnemyMagicController）のみ |
| Enemyに固有アニメーションは必要？ | **必要**。Idle, Walk, Attack, Cast, Hit, Death等をMixamoから取得推奨 |

### 9.2 共有可能なもの

```
共有可能（そのまま使える）:
├── MagicData / SkillData (ScriptableObject)
├── エフェクトPrefab (RFX4等)
├── DamageSystem
├── MagicProjectile
├── ダメージ数値表示
└── サウンド

専用実装が必要:
├── 発動ロジック（EnemyMagicController）
├── アニメーション（Mixamoから取得）
└── AI判定（いつ魔法を使うか）
```

---

## 10. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | Player Fireball発射 | エフェクト表示、敵にダメージ |
| 2 | Enemy Fireball発射 | 同じエフェクト、Playerにダメージ |
| 3 | 同じMagicData使用 | 同じダメージ値、詠唱時間 |
| 4 | DamageSystem呼び出し | Player/Enemy両方でダメージ処理 |
| 5 | エフェクトPrefab共有 | 見た目が同一 |
| 6 | Projectile共有 | 同じ挙動（速度、ホーミング等） |

---

## 11. 関連仕様書

- `05-combat-system-spec.md` - 戦闘システム詳細
- `12-enemy-system-spec.md` - Enemyシステム詳細
- `03-action-system-spec.md` - アクションシステム

