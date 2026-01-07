using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// SkillData統合の魔法投射物
    /// Player/Enemy共通で使用可能
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MagicProjectile : MonoBehaviour
    {
        [Header("基本設定（SkillDataで上書き可能）")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private DamageType damageType = DamageType.Magical;
        [SerializeField] private MagicElement element = MagicElement.None;

        [Header("ホーミング")]
        [SerializeField] private bool isHoming = false;
        [SerializeField] private float homingStrength = 5f;
        [SerializeField] private float homingRange = 20f;

        [Header("ビジュアル")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem mainParticle;
        [SerializeField] private Light projectileLight;
        [SerializeField] private GameObject impactEffectPrefab;

        [Header("属性別エフェクト")]
        [SerializeField] private ElementVisualConfig[] elementVisuals;

        // 内部状態
        private GameObject _owner;
        private string _ownerTag;
        private Transform _target;
        private Vector3 _direction;
        private SkillData _skillData;
        private bool _initialized;

        private void Start()
        {
            if (!_initialized)
            {
                _direction = transform.forward;
            }

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (isHoming)
            {
                UpdateHoming();
            }

            transform.position += _direction * speed * Time.deltaTime;

            if (_direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }
        }

        /// <summary>
        /// SkillDataから初期化（推奨）
        /// </summary>
        public void Initialize(SkillData skill, GameObject owner, Vector3 direction, Transform target = null)
        {
            _skillData = skill;
            _owner = owner;
            _ownerTag = owner?.tag ?? "";
            _direction = direction.normalized;
            _target = target;

            if (skill != null)
            {
                damage = skill.CalculateDamage(false);
                damageType = skill.damageType;
                element = skill.element;
                speed = skill.projectileSpeed;
                lifetime = skill.projectileLifetime;
                isHoming = skill.projectileHoming;
                homingStrength = skill.homingStrength;
            }

            ApplyElementVisuals(element);
            _initialized = true;
        }

        /// <summary>
        /// シンプル初期化（後方互換）
        /// </summary>
        public void Initialize(Vector3 direction, GameObject owner, float dmg = 0, Transform target = null)
        {
            _owner = owner;
            _ownerTag = owner?.tag ?? "";
            _direction = direction.normalized;
            _target = target;

            if (dmg > 0)
            {
                damage = dmg;
            }

            ApplyElementVisuals(element);
            _initialized = true;
        }

        /// <summary>
        /// 属性指定初期化
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
            ApplyElementVisuals(element);
        }

        private void ApplyElementVisuals(MagicElement elem)
        {
            Color elementColor = GetElementColor(elem);

            if (trailRenderer != null)
            {
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(elementColor, 0f),
                        new GradientColorKey(elementColor * 0.5f, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                trailRenderer.colorGradient = gradient;
            }

            if (mainParticle != null)
            {
                var main = mainParticle.main;
                main.startColor = elementColor;
            }

            if (projectileLight != null)
            {
                projectileLight.color = elementColor;
            }

            if (elementVisuals != null)
            {
                foreach (var config in elementVisuals)
                {
                    if (config.element == elem)
                    {
                        ApplyElementConfig(config);
                        break;
                    }
                }
            }
        }

        private void ApplyElementConfig(ElementVisualConfig config)
        {
            if (config.overrideTrailMaterial && trailRenderer != null)
            {
                trailRenderer.material = config.trailMaterial;
            }

            if (config.overrideParticle && mainParticle != null)
            {
                var main = mainParticle.main;
                main.startColor = config.particleColor;
            }

            if (config.impactEffectPrefab != null)
            {
                impactEffectPrefab = config.impactEffectPrefab;
            }
        }

        private void UpdateHoming()
        {
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
            string searchTag = _ownerTag == "Player" ? "Enemy" : "Player";
            var targets = GameObject.FindGameObjectsWithTag(searchTag);

            Transform nearest = null;
            float minDist = homingRange;

            foreach (var t in targets)
            {
                var damageable = t.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsAlive) continue;

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
            Debug.Log($"[MagicProjectile] OnTriggerEnter: {other.name}, tag={other.tag}");

            if (_owner != null && other.gameObject == _owner)
            {
                Debug.Log($"[MagicProjectile] Skipped: owner ({_owner.name})");
                return;
            }
            if (!string.IsNullOrEmpty(_ownerTag) && other.CompareTag(_ownerTag))
            {
                Debug.Log($"[MagicProjectile] Skipped: same tag ({_ownerTag})");
                return;
            }
            if (other.GetComponent<MagicProjectile>() != null)
            {
                Debug.Log("[MagicProjectile] Skipped: is MagicProjectile");
                return;
            }
            if (other.GetComponent<Projectile>() != null)
            {
                Debug.Log("[MagicProjectile] Skipped: is Projectile");
                return;
            }

            Debug.Log($"[MagicProjectile] Applying damage to: {other.name}");
            ApplyDamageToTarget(other);
            SpawnImpactEffect();
            Destroy(gameObject);
        }

        private void ApplyDamageToTarget(Collider target)
        {
            Vector3 hitPoint = target.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;
            Vector3 knockbackDir = _direction;

            Debug.Log($"[MagicProjectile] ApplyDamageToTarget: _skillData={(_skillData != null ? _skillData.skillName : "null")}, DamageSystem={DamageSystem.Instance != null}");

            if (_skillData != null && DamageSystem.Instance != null)
            {
                Debug.Log($"[MagicProjectile] Using SkillData path, damage={_skillData.baseDamage}");
                var damageInfo = DamageInfo.FromSkill(
                    _skillData,
                    _owner,
                    hitPoint,
                    hitNormal,
                    knockbackDir
                );

                DamageSystem.Instance.ApplyDamage(target.gameObject, damageInfo);
            }
            else if (DamageSystem.Instance != null)
            {
                Debug.Log($"[MagicProjectile] Using direct damage path, damage={damage}, element={element}");
                DamageSystem.Instance.ApplyDamage(
                    target.gameObject,
                    damage,
                    damageType,
                    element,
                    hitPoint,
                    _owner
                );
            }
            else
            {
                var damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    var info = new DamageInfo(
                        damage, false, 0f, knockbackDir,
                        _owner, _skillData, hitPoint, hitNormal
                    );
                    damageable.TakeDamage(info);
                }
            }
        }

        private void SpawnImpactEffect()
        {
            GameObject effectPrefab = impactEffectPrefab;

            if (_skillData != null && _skillData.hitEffectPrefab != null)
            {
                effectPrefab = _skillData.hitEffectPrefab;
            }

            if (effectPrefab != null)
            {
                var effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        private Color GetElementColor(MagicElement elem)
        {
            return elem switch
            {
                MagicElement.Fire => new Color(1f, 0.4f, 0.1f),
                MagicElement.Ice => new Color(0.5f, 0.8f, 1f),
                MagicElement.Lightning => new Color(1f, 1f, 0.3f),
                MagicElement.Wind => new Color(0.6f, 1f, 0.6f),
                MagicElement.Earth => new Color(0.6f, 0.4f, 0.2f),
                MagicElement.Light => Color.white,
                MagicElement.Dark => new Color(0.5f, 0.2f, 0.8f),
                _ => Color.cyan
            };
        }

        private void OnDrawGizmosSelected()
        {
            if (isHoming)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, homingRange);
            }
        }
    }

    /// <summary>
    /// 属性別ビジュアル設定
    /// </summary>
    [System.Serializable]
    public class ElementVisualConfig
    {
        public MagicElement element;

        [Header("トレイル")]
        public bool overrideTrailMaterial = false;
        public Material trailMaterial;

        [Header("パーティクル")]
        public bool overrideParticle = false;
        public Color particleColor = Color.white;

        [Header("着弾エフェクト")]
        public GameObject impactEffectPrefab;
    }
}
