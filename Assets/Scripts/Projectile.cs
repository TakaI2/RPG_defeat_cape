using UnityEngine;
using RPG.Combat;

/// <summary>
/// 投射物（魔法弾、矢など）
/// DamageSystemと連携してダメージを適用
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float damage = 20f;

    [Header("属性")]
    [SerializeField] private DamageType damageType = DamageType.Magical;
    [SerializeField] private MagicElement element = MagicElement.None;

    [Header("エフェクト")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private bool destroyOnImpact = true;

    [Header("ホーミング")]
    [SerializeField] private bool isHoming = false;
    [SerializeField] private float homingStrength = 5f;
    [SerializeField] private float homingRange = 20f;

    // 発射者情報
    private GameObject _owner;
    private string _ownerTag;
    private Transform _target;
    private Vector3 _direction;
    private bool _initialized;

    private void Start()
    {
        // 初期化されていない場合のデフォルト動作
        if (!_initialized)
        {
            _direction = transform.forward;
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // ホーミング
        if (isHoming)
        {
            UpdateHoming();
        }

        // 移動
        transform.position += _direction * speed * Time.deltaTime;

        // 進行方向を向く
        if (_direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_direction);
        }
    }

    /// <summary>
    /// 初期化（拡張版）
    /// </summary>
    public void Initialize(Vector3 direction, GameObject owner, float dmg = 0, Transform target = null)
    {
        _direction = direction.normalized;
        _owner = owner;
        _ownerTag = owner?.tag ?? "";
        _target = target;

        if (dmg > 0)
        {
            damage = dmg;
        }

        _initialized = true;
    }

    /// <summary>
    /// 初期化（属性付き）
    /// </summary>
    public void Initialize(
        Vector3 direction,
        GameObject owner,
        float dmg,
        DamageType type,
        MagicElement elem,
        Transform target = null)
    {
        Initialize(direction, owner, dmg, target);
        damageType = type;
        element = elem;
    }

    private void UpdateHoming()
    {
        // ターゲットがない場合は最も近い敵を探す
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            _target = FindNearestTarget();
        }

        if (_target != null)
        {
            Vector3 toTarget = (_target.position - transform.position).normalized;
            _direction = Vector3.Lerp(_direction, toTarget, homingStrength * Time.deltaTime);
            _direction.Normalize();
        }
    }

    private Transform FindNearestTarget()
    {
        // 発射者と反対のタグを探す
        string searchTag = _ownerTag == "Player" ? "Enemy" : "Player";
        var targets = GameObject.FindGameObjectsWithTag(searchTag);

        Transform nearest = null;
        float minDist = homingRange;

        foreach (var t in targets)
        {
            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = t.transform;
            }
        }

        return nearest;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 発射者には当たらない
        if (_owner != null && other.gameObject == _owner) return;
        if (!string.IsNullOrEmpty(_ownerTag) && other.CompareTag(_ownerTag)) return;

        // ダメージ適用（DamageSystem経由）
        if (DamageSystem.Instance != null)
        {
            DamageSystem.Instance.ApplyDamage(
                other.gameObject,
                damage,
                damageType,
                element,
                transform.position,
                _owner
            );
        }
        else
        {
            // フォールバック：直接EnemyControllerを呼ぶ（後方互換）
            if (other.CompareTag("Enemy"))
            {
                var enemy = other.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        // 着弾エフェクト
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        // 破壊
        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }
}
