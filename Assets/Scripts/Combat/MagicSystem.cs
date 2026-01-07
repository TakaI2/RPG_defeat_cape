using System.Collections;
using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// Player/Enemy共通の魔法発射システム
    /// MagicProjectileの生成とエフェクト管理を統一的に行う
    /// </summary>
    public class MagicSystem : MonoBehaviour
    {
        public static MagicSystem Instance { get; private set; }

        [Header("デフォルト設定")]
        [SerializeField] private GameObject defaultProjectilePrefab;
        [SerializeField] private float defaultProjectileSpeed = 20f;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;

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
        /// 魔法を発射する（SkillData使用）
        /// </summary>
        /// <param name="skillData">スキルデータ</param>
        /// <param name="firePoint">発射位置</param>
        /// <param name="target">ターゲット（nullの場合は正面方向）</param>
        /// <param name="owner">発射者</param>
        /// <returns>生成されたProjectile</returns>
        public MagicProjectile FireMagic(
            SkillData skillData,
            Transform firePoint,
            Transform target,
            GameObject owner)
        {
            if (skillData == null)
            {
                Debug.LogWarning("[MagicSystem] SkillData is null");
                return null;
            }

            // プレハブ決定
            GameObject prefab = skillData.projectilePrefab != null
                ? skillData.projectilePrefab
                : defaultProjectilePrefab;

            if (prefab == null)
            {
                Debug.LogWarning("[MagicSystem] No projectile prefab available");
                return null;
            }

            // 方向計算
            Vector3 direction = target != null
                ? (target.position - firePoint.position).normalized
                : firePoint.forward;

            // 投射物生成
            var projectileObj = Instantiate(prefab, firePoint.position, Quaternion.LookRotation(direction));
            var magicProjectile = projectileObj.GetComponent<MagicProjectile>();

            if (magicProjectile != null)
            {
                magicProjectile.Initialize(skillData, owner, direction, target);

                if (showDebugLog)
                {
                    Debug.Log($"[MagicSystem] Fired {skillData.skillName} from {owner?.name ?? "unknown"} to {target?.name ?? "forward"}");
                }
            }
            else
            {
                Debug.LogWarning("[MagicSystem] Prefab does not have MagicProjectile component");
                Destroy(projectileObj);
                return null;
            }

            return magicProjectile;
        }

        /// <summary>
        /// 魔法を発射する（直接パラメータ指定）
        /// </summary>
        public MagicProjectile FireMagic(
            GameObject projectilePrefab,
            Transform firePoint,
            Transform target,
            GameObject owner,
            float damage,
            DamageType damageType = DamageType.Magical,
            MagicElement element = MagicElement.None)
        {
            GameObject prefab = projectilePrefab != null ? projectilePrefab : defaultProjectilePrefab;

            if (prefab == null)
            {
                Debug.LogWarning("[MagicSystem] No projectile prefab available");
                return null;
            }

            Vector3 direction = target != null
                ? (target.position - firePoint.position).normalized
                : firePoint.forward;

            var projectileObj = Instantiate(prefab, firePoint.position, Quaternion.LookRotation(direction));
            var magicProjectile = projectileObj.GetComponent<MagicProjectile>();

            if (magicProjectile != null)
            {
                magicProjectile.Initialize(direction, owner, damage, damageType, element, target);

                if (showDebugLog)
                {
                    Debug.Log($"[MagicSystem] Fired {element} magic ({damage} dmg) from {owner?.name ?? "unknown"}");
                }
            }
            else
            {
                Debug.LogWarning("[MagicSystem] Prefab does not have MagicProjectile component");
                Destroy(projectileObj);
                return null;
            }

            return magicProjectile;
        }

        /// <summary>
        /// 詠唱付き魔法発射（コルーチン）
        /// </summary>
        public IEnumerator FireMagicWithCast(
            SkillData skillData,
            Transform firePoint,
            Transform target,
            GameObject owner,
            float castTime,
            GameObject chargeEffectPrefab = null,
            System.Action onCastComplete = null)
        {
            // 詠唱エフェクト
            GameObject chargeEffect = null;
            if (chargeEffectPrefab != null)
            {
                chargeEffect = Instantiate(chargeEffectPrefab, firePoint.position, firePoint.rotation, firePoint);
            }

            if (showDebugLog)
            {
                Debug.Log($"[MagicSystem] Casting {skillData?.skillName ?? "magic"}... ({castTime}s)");
            }

            // 詠唱待機
            yield return new WaitForSeconds(castTime);

            // 詠唱エフェクト削除
            if (chargeEffect != null)
            {
                Destroy(chargeEffect);
            }

            // 魔法発射
            FireMagic(skillData, firePoint, target, owner);

            onCastComplete?.Invoke();
        }

        /// <summary>
        /// 即時ダメージ魔法（投射物なし）
        /// </summary>
        public void CastInstantMagic(
            SkillData skillData,
            Transform target,
            GameObject owner)
        {
            if (target == null || skillData == null) return;

            if (DamageSystem.Instance != null)
            {
                var damageInfo = DamageInfo.FromSkill(
                    skillData,
                    owner,
                    target.position,
                    Vector3.up,
                    Vector3.zero
                );

                DamageSystem.Instance.ApplyDamage(target.gameObject, damageInfo);

                if (showDebugLog)
                {
                    Debug.Log($"[MagicSystem] Instant magic {skillData.skillName} hit {target.name}");
                }
            }
        }

        /// <summary>
        /// 範囲魔法
        /// </summary>
        public void CastAreaMagic(
            SkillData skillData,
            Vector3 center,
            float radius,
            GameObject owner,
            LayerMask targetLayers = default)
        {
            if (skillData == null) return;

            if (DamageSystem.Instance != null)
            {
                DamageSystem.Instance.ApplyAreaDamage(
                    center,
                    radius,
                    skillData.baseDamage,
                    skillData.damageType,
                    skillData.element,
                    targetLayers,
                    owner
                );

                if (showDebugLog)
                {
                    Debug.Log($"[MagicSystem] Area magic {skillData.skillName} at {center}, radius={radius}");
                }
            }
        }
    }
}
