# 仕様書: 戦闘システム

## 概要

魔法攻撃、敵キャラクター、ダメージ処理を統合する戦闘システム。
KriptoFXエフェクトと物理演算を活用してリアルな戦闘を実現する。

---

## 1. 魔法システム

### 1.1 MagicSystem クラス

```csharp
public enum MagicElement
{
    Fire,
    Ice,
    Lightning,
    Wind,
    Earth,
    Light,
    Dark
}

public class MagicSystem : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameCharacter caster;
    [SerializeField] private RFX4_EffectEvent effectEvent;

    [Header("魔法設定")]
    [SerializeField] private MagicData[] availableMagics;
    [SerializeField] private int currentMagicIndex = 0;

    public MagicData CurrentMagic => availableMagics.Length > 0 ?
        availableMagics[currentMagicIndex] : null;

    /// <summary>
    /// 魔法を発動
    /// </summary>
    public IEnumerator CastMagic(Vector3 targetPosition, GameObject target = null)
    {
        var magic = CurrentMagic;
        if (magic == null) yield break;

        // 1. 詠唱エフェクト（手元）
        if (effectEvent != null)
        {
            effectEvent.CharacterEffect = magic.chargeEffectPrefab;
            effectEvent.ActivateCharacterEffect();
        }

        // 2. 詠唱待機
        yield return new WaitForSeconds(magic.castTime);

        // 3. メインエフェクト発射
        if (effectEvent != null)
        {
            effectEvent.MainEffect = magic.mainEffectPrefab;
            effectEvent.Target = target?.transform;
            effectEvent.ActivateEffect();
        }

        // 4. ダメージ処理（Projectileで処理、またはここで直接）
        if (magic.isInstant && target != null)
        {
            ApplyMagicDamage(target, magic);
        }
    }

    /// <summary>
    /// ダメージ適用
    /// </summary>
    private void ApplyMagicDamage(GameObject target, MagicData magic)
    {
        var enemy = target.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(magic.baseDamage, magic.element);
        }

        var character = target.GetComponent<GameCharacter>();
        if (character != null)
        {
            character.TakeDamage(magic.baseDamage);
        }
    }

    /// <summary>
    /// 魔法を切り替え
    /// </summary>
    public void SwitchMagic(int index)
    {
        if (index >= 0 && index < availableMagics.Length)
        {
            currentMagicIndex = index;
        }
    }

    /// <summary>
    /// 次の魔法へ
    /// </summary>
    public void NextMagic()
    {
        currentMagicIndex = (currentMagicIndex + 1) % availableMagics.Length;
    }
}
```

### 1.2 MagicData ScriptableObject

```csharp
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

### 1.3 魔法タイプ例

| 魔法名 | 属性 | 詠唱時間 | ダメージ | 特徴 |
|--------|------|---------|---------|------|
| Fireball | Fire | 1.5s | 30 | 投射、爆発 |
| Ice Lance | Ice | 1.0s | 25 | 高速、貫通 |
| Thunder | Lightning | 2.0s | 50 | 即時、範囲 |
| Wind Blade | Wind | 0.8s | 15 | 高速連射 |
| Meteor | Fire | 3.0s | 80 | 広範囲 |

---

## 2. 敵システム

### 2.1 EnemyController クラス

```csharp
public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Stagger,
    Dead
}

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
    [SerializeField] private float ragdollHPThreshold = 0.3f;  // HP30%以下でラグドール
    [SerializeField] private float knockbackForce = 10f;

    [Header("インタラクションポイント")]
    [SerializeField] private InteractionPoint targetPoint;  // 攻撃目標
    [SerializeField] private InteractionPoint weakPoint;    // 弱点
    [SerializeField] private InteractionPoint dragPoint;    // 引きずり用

    [Header("エフェクト")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;

    // 参照
    private Rigidbody _rigidbody;
    private Animator _animator;
    private NavMeshAgent _agent;
    private Collider[] _ragdollColliders;
    private Rigidbody[] _ragdollRigidbodies;

    // 状態
    public EnemyState State { get; private set; } = EnemyState.Idle;
    public float HP => currentHP;
    public float HPRatio => currentHP / maxHP;
    public bool IsDead => State == EnemyState.Dead;
    public bool IsDraggable => IsDead && dragPoint != null;

    // ターゲット
    private Transform _currentTarget;
    private float _lastAttackTime;

    // イベント
    public event Action<float, float> OnHPChanged;
    public event Action OnDeath;
    public event Action<float> OnDamaged;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();

        // ラグドール用コンポーネント収集
        _ragdollColliders = GetComponentsInChildren<Collider>();
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        // 初期状態：ラグドール無効
        SetRagdollEnabled(false);
    }

    private void Start()
    {
        currentHP = maxHP;
    }

    private void Update()
    {
        if (IsDead) return;

        UpdateState();
        ExecuteState();
    }

    /// <summary>
    /// 状態更新
    /// </summary>
    private void UpdateState()
    {
        // プレイヤー検出
        var player = FindPlayer();

        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist < attackRange)
            {
                State = EnemyState.Attack;
                _currentTarget = player;
            }
            else if (dist < detectionRange)
            {
                State = EnemyState.Chase;
                _currentTarget = player;
            }
        }
        else
        {
            State = EnemyState.Idle;
            _currentTarget = null;
        }
    }

    /// <summary>
    /// 状態実行
    /// </summary>
    private void ExecuteState()
    {
        switch (State)
        {
            case EnemyState.Idle:
                // 待機アニメーション
                break;

            case EnemyState.Chase:
                if (_currentTarget != null && _agent != null)
                {
                    _agent.SetDestination(_currentTarget.position);
                }
                break;

            case EnemyState.Attack:
                if (Time.time - _lastAttackTime > attackCooldown)
                {
                    PerformAttack();
                    _lastAttackTime = Time.time;
                }
                break;
        }
    }

    /// <summary>
    /// 攻撃実行
    /// </summary>
    private void PerformAttack()
    {
        _animator?.SetTrigger("Attack");

        // ダメージ判定（アニメーションイベントで呼ぶのが望ましい）
        if (_currentTarget != null)
        {
            var character = _currentTarget.GetComponent<GameCharacter>();
            character?.TakeDamage(attackDamage);
        }
    }

    /// <summary>
    /// ダメージを受ける
    /// </summary>
    public void TakeDamage(float damage, MagicElement element = MagicElement.Fire)
    {
        if (IsDead) return;

        // 弱点ヒットでダメージ増加
        // (弱点判定は別途Collider判定で)

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

    /// <summary>
    /// ヒットリアクション
    /// </summary>
    private IEnumerator HitReaction()
    {
        State = EnemyState.Stagger;
        _animator?.SetTrigger("Hit");

        if (_agent != null)
        {
            _agent.isStopped = true;
        }

        yield return new WaitForSeconds(0.5f);

        if (_agent != null)
        {
            _agent.isStopped = false;
        }

        State = EnemyState.Idle;
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private void Die()
    {
        State = EnemyState.Dead;
        OnDeath?.Invoke();

        // NavMesh無効化
        if (_agent != null)
        {
            _agent.enabled = false;
        }

        // Animator無効化
        if (_animator != null)
        {
            _animator.enabled = false;
        }

        // 完全ラグドール化
        SetRagdollEnabled(true);

        // 死亡エフェクト
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // drag_pointを有効化（引きずり可能に）
        if (dragPoint != null)
        {
            dragPoint.isActive = true;
        }
    }

    /// <summary>
    /// ラグドール有効/無効
    /// </summary>
    private void SetRagdollEnabled(bool enabled)
    {
        foreach (var col in _ragdollColliders)
        {
            if (col.gameObject != gameObject) // ルートは除く
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

    /// <summary>
    /// 部分的ラグドール（HP低下時）
    /// </summary>
    private void EnablePartialRagdoll()
    {
        // 上半身だけラグドールにするなど
        // 実装は敵の種類に依存
    }

    /// <summary>
    /// ノックバック
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            _rigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// プレイヤーを検索
    /// </summary>
    private Transform FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        return player?.transform;
    }
}
```

---

## 3. ダメージシステム

### 3.1 DamageSystem クラス

```csharp
public class DamageSystem : MonoBehaviour
{
    public static DamageSystem Instance { get; private set; }

    [Header("ダメージ表示")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private float damageNumberDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ダメージを与える
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
    /// ダメージ計算
    /// </summary>
    private float CalculateDamage(float baseDamage, DamageType type,
        MagicElement element, GameObject target)
    {
        float damage = baseDamage;

        // 属性相性（将来拡張）
        // damage *= GetElementMultiplier(element, target);

        // クリティカル（将来拡張）
        // if (Random.value < criticalChance) damage *= criticalMultiplier;

        return damage;
    }

    /// <summary>
    /// ダメージ数値表示
    /// </summary>
    private void ShowDamageNumber(Vector3 position, float damage)
    {
        if (damageNumberPrefab == null) return;

        var dmgObj = Instantiate(damageNumberPrefab, position, Quaternion.identity);
        var dmgUI = dmgObj.GetComponent<DamageNumber>();
        dmgUI?.SetValue(Mathf.RoundToInt(damage));

        Destroy(dmgObj, damageNumberDuration);
    }

    /// <summary>
    /// 範囲ダメージ
    /// </summary>
    public void ApplyAreaDamage(Vector3 center, float radius, float damage,
        DamageType type = DamageType.Magical,
        MagicElement element = MagicElement.Fire,
        LayerMask targetLayers = default)
    {
        var colliders = Physics.OverlapSphere(center, radius, targetLayers);

        foreach (var col in colliders)
        {
            ApplyDamage(col.gameObject, damage, type, element, col.ClosestPoint(center));
        }
    }
}

public enum DamageType
{
    Physical,
    Magical
}
```

### 3.2 DamageNumber UI

```csharp
public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private AnimationCurve fadeCurve;

    private float _startTime;
    private Vector3 _startPosition;

    public void SetValue(int damage)
    {
        text.text = damage.ToString();
        _startTime = Time.time;
        _startPosition = transform.position;
    }

    private void Update()
    {
        // 上に浮かぶ
        transform.position = _startPosition + Vector3.up * floatSpeed * (Time.time - _startTime);

        // フェードアウト
        float t = (Time.time - _startTime) / 1f;
        Color c = text.color;
        c.a = fadeCurve.Evaluate(t);
        text.color = c;

        // カメラに向ける
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0);
    }
}
```

---

## 4. ダメージフロー

```
┌─────────────────────────────────────────────────────────────┐
│                    ダメージ発生フロー                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  攻撃/魔法ヒット                                            │
│       │                                                     │
│       ▼                                                     │
│  ┌──────────────┐                                           │
│  │DamageSystem  │                                           │
│  │.ApplyDamage()│                                           │
│  └──────┬───────┘                                           │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐                                           │
│  │ダメージ計算   │                                           │
│  │・基礎ダメージ │                                           │
│  │・属性相性     │                                           │
│  │・クリティカル │                                           │
│  └──────┬───────┘                                           │
│         │                                                   │
│         ▼                                                   │
│  ┌──────────────┐                                           │
│  │ターゲット判定 │                                           │
│  └──────┬───────┘                                           │
│         │                                                   │
│    ┌────┴────┐                                              │
│    ▼         ▼                                              │
│ ┌──────┐  ┌──────┐                                          │
│ │Enemy │  │Chara │                                          │
│ └──┬───┘  └──┬───┘                                          │
│    │         │                                              │
│    ▼         ▼                                              │
│ TakeDamage() TakeDamage()                                   │
│    │         │                                              │
│    ▼         ▼                                              │
│ ┌────────────────────────────────────┐                     │
│ │            HP減少                   │                     │
│ └────────────────────────────────────┘                     │
│    │                                                        │
│    ├─── HP > 0 ───▶ ヒットリアクション                     │
│    │                 ・アニメーション                       │
│    │                 ・表情変化                             │
│    │                 ・ノックバック                         │
│    │                                                        │
│    └─── HP <= 0 ──▶ 死亡処理                               │
│                      ├─ NavMesh無効化                       │
│                      ├─ Animator無効化                      │
│                      ├─ ラグドール化                        │
│                      ├─ 死亡エフェクト                      │
│                      └─ drag_point有効化                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. 投射物（Projectile）

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

    private Transform _target;
    private Vector3 _direction;

    public void Initialize(Vector3 direction, Transform target = null, float dmg = 0)
    {
        _direction = direction.normalized;
        _target = target;
        if (dmg > 0) damage = dmg;

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
        // 自分（Caster）には当たらない
        if (other.CompareTag("Player")) return;

        // ダメージ適用
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

## 6. 引きずりシステム

```csharp
public class DragSystem : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameCharacter dragger;

    [Header("設定")]
    [SerializeField] private float dragForce = 5f;
    [SerializeField] private float maxDragDistance = 3f;

    private EnemyController _dragTarget;
    private InteractionPoint _grabPoint;
    private Joint _dragJoint;

    /// <summary>
    /// 引きずり開始
    /// </summary>
    public bool StartDragging(EnemyController enemy)
    {
        if (!enemy.IsDraggable) return false;

        _dragTarget = enemy;
        _grabPoint = enemy.GetComponent<InteractionPoint>();

        // 固定ジョイントで接続
        _dragJoint = dragger.gameObject.AddComponent<FixedJoint>();
        _dragJoint.connectedBody = _grabPoint.GetComponent<Rigidbody>();

        return true;
    }

    /// <summary>
    /// 引きずり終了
    /// </summary>
    public void StopDragging()
    {
        if (_dragJoint != null)
        {
            Destroy(_dragJoint);
        }

        _dragTarget = null;
        _grabPoint = null;
    }

    /// <summary>
    /// 引きずり中か
    /// </summary>
    public bool IsDragging => _dragTarget != null;
}
```

---

## 7. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | 魔法詠唱 | 詠唱エフェクト→メインエフェクト発射 |
| 2 | 魔法ヒット | ダメージ数値表示 + 敵HP減少 |
| 3 | 敵HP閾値 | HP30%以下で部分ラグドール |
| 4 | 敵死亡 | 完全ラグドール + drag_point有効化 |
| 5 | 死体引きずり | drag_pointを掴んで移動可能 |
| 6 | 範囲魔法 | 範囲内の全敵にダメージ |
| 7 | ホーミング | ターゲットに追尾 |
| 8 | 属性ダメージ | 属性相性でダメージ変化（将来） |

