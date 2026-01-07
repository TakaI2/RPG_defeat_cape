using UnityEngine;
using UnityEngine.AI;
using System;
using RPG.Combat;

/// <summary>
/// 敵の状態
/// </summary>
public enum EnemyState
{
    Idle,       // 待機
    Patrol,     // 巡回
    Chase,      // 追跡
    Attack,     // 攻撃
    Stagger,    // ひるみ
    Dead        // 死亡
}

/// <summary>
/// 敵キャラクターコントローラー
/// IDamageableを実装し、HP管理、状態管理、ラグドールを含む
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("基本設定")]
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float speed = 3f;

    [Header("戦闘設定")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("物理設定")]
    [SerializeField] private float ragdollHPThreshold = 0.3f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float staggerDuration = 0.5f;

    [Header("エフェクト")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("オプション")]
    [SerializeField] private bool useNavMesh = false;
    [SerializeField] private bool useRagdoll = false;

    // 状態
    private float _currentHP;
    private EnemyState _currentState = EnemyState.Idle;
    private float _lastAttackTime;
    private float _staggerEndTime;

    // ターゲット
    private Transform _target;
    private System.Action<EnemyController> _onDeath;

    // コンポーネント
    private Rigidbody _rigidbody;
    private Animator _animator;
    private NavMeshAgent _agent;
    private Collider[] _ragdollColliders;
    private Rigidbody[] _ragdollRigidbodies;
    private Collider _mainCollider;

    // プロパティ
    public EnemyState State => _currentState;
    public float HP => _currentHP;
    public float HPRatio => _currentHP / maxHP;
    public bool IsDead => _currentState == EnemyState.Dead;
    public string EnemyName => enemyName;

    // IDamageable実装
    public float CurrentHealth => _currentHP;
    public float MaxHealth => maxHP;
    public bool IsAlive => !IsDead;

    // イベント
    public event Action<float, float> OnHPChanged;  // current, max
    public event Action OnDeathEvent;
    public event Action<float> OnDamaged;

    #region Unity Lifecycle

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _mainCollider = GetComponent<Collider>();

        // ラグドール用コンポーネント収集
        if (useRagdoll)
        {
            CollectRagdollComponents();
            SetRagdollEnabled(false);
        }
    }

    private void Start()
    {
        _currentHP = maxHP;

        // NavMeshAgent設定
        if (_agent != null && useNavMesh)
        {
            _agent.speed = speed;
        }
    }

    private void Update()
    {
        // ゲーム状態チェック（後方互換）
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }

        if (IsDead) return;

        // ひるみ中
        if (_currentState == EnemyState.Stagger)
        {
            if (Time.time >= _staggerEndTime)
            {
                _currentState = EnemyState.Idle;
            }
            return;
        }

        UpdateState();
        ExecuteState();
    }

    #endregion

    #region Initialization (後方互換)

    /// <summary>
    /// 初期化（後方互換メソッド）
    /// </summary>
    public void Initialize(Transform target, System.Action<EnemyController> onDeath)
    {
        _target = target;
        _onDeath = onDeath;
        _currentState = EnemyState.Chase;
    }

    /// <summary>
    /// 拡張初期化
    /// </summary>
    public void Initialize(Transform target, System.Action<EnemyController> onDeath, float hp)
    {
        Initialize(target, onDeath);
        maxHP = hp;
        _currentHP = hp;
    }

    #endregion

    #region State Management

    private void UpdateState()
    {
        // ターゲットがない場合は検出を試みる
        if (_target == null)
        {
            _target = FindPlayer();
        }

        if (_target == null)
        {
            _currentState = EnemyState.Idle;
            return;
        }

        float dist = Vector3.Distance(transform.position, _target.position);

        if (dist < attackRange)
        {
            _currentState = EnemyState.Attack;
        }
        else if (dist < detectionRange)
        {
            _currentState = EnemyState.Chase;
        }
        else
        {
            _currentState = EnemyState.Idle;
        }
    }

    private void ExecuteState()
    {
        switch (_currentState)
        {
            case EnemyState.Idle:
                ExecuteIdle();
                break;
            case EnemyState.Chase:
                ExecuteChase();
                break;
            case EnemyState.Attack:
                ExecuteAttack();
                break;
        }
    }

    private void ExecuteIdle()
    {
        // 待機（アニメーション再生など）
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", false);
        }
    }

    private void ExecuteChase()
    {
        if (_target == null) return;

        if (_animator != null)
        {
            _animator.SetBool("IsMoving", true);
        }

        if (useNavMesh && _agent != null && _agent.enabled)
        {
            _agent.SetDestination(_target.position);
        }
        else
        {
            // シンプル移動（既存ロジック）
            Vector3 direction = (_target.position - transform.position).normalized;
            direction.y = 0;
            transform.position += direction * speed * Time.deltaTime;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void ExecuteAttack()
    {
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", false);
        }

        // ターゲットの方を向く
        if (_target != null)
        {
            Vector3 lookDir = (_target.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // 攻撃クールダウン
        if (Time.time - _lastAttackTime > attackCooldown)
        {
            PerformAttack();
            _lastAttackTime = Time.time;
        }
    }

    private void PerformAttack()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        // ダメージ適用（DamageSystemを使用）
        if (_target != null && DamageSystem.Instance != null)
        {
            DamageSystem.Instance.ApplyDamage(
                _target.gameObject,
                attackDamage,
                DamageType.Physical,
                MagicElement.None,
                null,
                gameObject
            );
        }
    }

    #endregion

    #region IDamageable Implementation

    /// <summary>
    /// ダメージを受ける（IDamageable）
    /// </summary>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead) return;

        Debug.Log($"[EnemyController] {enemyName} took {damageInfo.damage} damage! HP: {_currentHP} -> {_currentHP - damageInfo.damage}");
        _currentHP = Mathf.Max(0, _currentHP - damageInfo.damage);
        OnHPChanged?.Invoke(_currentHP, maxHP);
        OnDamaged?.Invoke(damageInfo.damage);

        // ヒットエフェクト
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, damageInfo.hitPoint, Quaternion.identity);
        }

        // ノックバック
        if (damageInfo.knockbackForce > 0 && _rigidbody != null)
        {
            ApplyKnockback(damageInfo.knockbackDirection, damageInfo.knockbackForce);
        }

        // ひるみ
        if (!IsDead)
        {
            StartStagger();
        }

        // 部分ラグドール（HP閾値）
        if (useRagdoll && HPRatio < ragdollHPThreshold && !IsDead)
        {
            EnablePartialRagdoll();
        }

        // 死亡判定
        if (_currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// シンプルダメージ（後方互換）
    /// </summary>
    public void TakeDamage(float damage)
    {
        var damageInfo = new DamageInfo(
            damage,
            false,
            0,
            Vector3.zero,
            null,
            null,
            transform.position + Vector3.up,
            Vector3.up
        );
        TakeDamage(damageInfo);
    }

    #endregion

    #region Damage Reactions

    private void StartStagger()
    {
        _currentState = EnemyState.Stagger;
        _staggerEndTime = Time.time + staggerDuration;
        if (_animator != null)
        {
            _animator.SetTrigger("Hit");
        }

        if (_agent != null)
        {
            _agent.isStopped = true;
        }
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            _rigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
    }

    #endregion

    #region Death

    /// <summary>
    /// 死亡処理
    /// </summary>
    public void Die()
    {
        if (_currentState == EnemyState.Dead) return;

        _currentState = EnemyState.Dead;
        _currentHP = 0;

        // NavMesh無効化
        if (_agent != null)
        {
            _agent.enabled = false;
        }

        // Animator無効化
        if (_animator != null)
        {
            _animator.SetTrigger("Death");
            // 完全ラグドール時はAnimator無効化
            if (useRagdoll)
            {
                _animator.enabled = false;
            }
        }

        // 完全ラグドール化
        if (useRagdoll)
        {
            SetRagdollEnabled(true);
        }

        // 死亡エフェクト
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // イベント発火
        OnDeathEvent?.Invoke();
        _onDeath?.Invoke(this);

        // メインコライダーをトリガーに（オプション）
        if (_mainCollider != null && !useRagdoll)
        {
            _mainCollider.enabled = false;
        }
    }

    #endregion

    #region Ragdoll

    private void CollectRagdollComponents()
    {
        _ragdollColliders = GetComponentsInChildren<Collider>();
        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
    }

    private void SetRagdollEnabled(bool enabled)
    {
        if (_ragdollColliders == null) return;

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
            _rigidbody.isKinematic = enabled; // ルートは逆
        }
    }

    private void EnablePartialRagdoll()
    {
        // 上半身のみラグドールにするなど（将来実装）
    }

    #endregion

    #region Utility

    private Transform FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        return player?.transform;
    }

    #endregion

    #region Editor

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 検出範囲
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 攻撃範囲
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif

    #endregion
}
