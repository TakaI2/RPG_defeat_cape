using UnityEngine;
using System;

namespace RPG.Combat
{
    /// <summary>
    /// ダメージの種類
    /// </summary>
    public enum DamageType
    {
        Physical,   // 物理ダメージ
        Magical     // 魔法ダメージ
    }

    /// <summary>
    /// 魔法属性
    /// </summary>
    public enum MagicElement
    {
        None,       // 無属性
        Fire,       // 炎
        Ice,        // 氷
        Lightning,  // 雷
        Wind,       // 風
        Earth,      // 土
        Light,      // 光
        Dark        // 闇
    }

    /// <summary>
    /// 統合ダメージ処理システム
    /// Player/Enemy共通で使用
    /// </summary>
    public class DamageSystem : MonoBehaviour
    {
        public static DamageSystem Instance { get; private set; }

        [Header("ダメージ表示")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private float damageNumberDuration = 1f;
        [SerializeField] private float damageNumberOffsetY = 1.5f;

        [Header("エフェクト")]
        [SerializeField] private GameObject defaultHitEffectPrefab;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = false;

        // イベント
        public event Action<GameObject, float, bool> OnDamageDealt; // target, damage, isCritical

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// ダメージを与える（シンプル版）
        /// </summary>
        public void ApplyDamage(GameObject target, float damage)
        {
            ApplyDamage(target, damage, DamageType.Physical, MagicElement.None, null, null);
        }

        /// <summary>
        /// ダメージを与える（フル版）
        /// </summary>
        public void ApplyDamage(
            GameObject target,
            float damage,
            DamageType damageType = DamageType.Physical,
            MagicElement element = MagicElement.None,
            Vector3? hitPoint = null,
            GameObject attacker = null)
        {
            if (target == null) return;

            // ダメージ計算
            float finalDamage = CalculateDamage(damage, damageType, element, target);
            bool isCritical = false; // 将来拡張用

            // ヒットポイント決定
            Vector3 effectPoint = hitPoint ?? target.transform.position + Vector3.up * damageNumberOffsetY;

            // IDamageableインターフェースを探す
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                var damageInfo = new DamageInfo(
                    finalDamage,
                    isCritical,
                    0f, // knockbackForce
                    attacker != null ? (target.transform.position - attacker.transform.position).normalized : Vector3.zero,
                    attacker,
                    null, // skill
                    effectPoint,
                    Vector3.up
                );

                damageable.TakeDamage(damageInfo);

                if (showDebugLog)
                {
                    Debug.Log($"[DamageSystem] {target.name} took {finalDamage} damage via IDamageable");
                }
            }
            else
            {
                // EnemyController直接呼び出し（後方互換）
                var enemy = target.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    // 既存のEnemyControllerにはTakeDamageがないので、Dieを呼ぶ（暫定）
                    // TODO: EnemyController拡張後に修正
                    if (showDebugLog)
                    {
                        Debug.Log($"[DamageSystem] {target.name} (Enemy) took {finalDamage} damage");
                    }
                }
            }

            // ダメージ数値表示
            ShowDamageNumber(effectPoint, finalDamage, isCritical, element);

            // ヒットエフェクト
            SpawnHitEffect(effectPoint, element);

            // イベント発火
            OnDamageDealt?.Invoke(target, finalDamage, isCritical);
        }

        /// <summary>
        /// DamageInfoを使ってダメージを与える
        /// </summary>
        public void ApplyDamage(GameObject target, DamageInfo damageInfo)
        {
            if (target == null) return;

            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageInfo);
            }

            // ダメージ数値表示
            ShowDamageNumber(damageInfo.hitPoint, damageInfo.damage, damageInfo.isCritical, MagicElement.None);

            // イベント発火
            OnDamageDealt?.Invoke(target, damageInfo.damage, damageInfo.isCritical);
        }

        /// <summary>
        /// 範囲ダメージを与える
        /// </summary>
        public void ApplyAreaDamage(
            Vector3 center,
            float radius,
            float damage,
            DamageType damageType = DamageType.Magical,
            MagicElement element = MagicElement.None,
            LayerMask targetLayers = default,
            GameObject attacker = null)
        {
            // デフォルトレイヤーマスク（全て）
            if (targetLayers == default)
            {
                targetLayers = ~0;
            }

            var colliders = Physics.OverlapSphere(center, radius, targetLayers);
            int hitCount = 0;

            foreach (var col in colliders)
            {
                // 自分自身は除外
                if (attacker != null && col.gameObject == attacker) continue;

                // 距離による減衰（オプション）
                float distance = Vector3.Distance(center, col.transform.position);
                float distanceMultiplier = 1f - (distance / radius) * 0.5f; // 中心から離れるほど50%まで減衰
                float adjustedDamage = damage * distanceMultiplier;

                // ClosestPointはBox/Sphere/Capsule/ConvexMeshのみ対応
                Vector3 hitPoint;
                if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider ||
                    (col is MeshCollider mc && mc.convex))
                {
                    hitPoint = col.ClosestPoint(center);
                }
                else
                {
                    hitPoint = col.bounds.ClosestPoint(center);
                }
                ApplyDamage(col.gameObject, adjustedDamage, damageType, element, hitPoint, attacker);
                hitCount++;
            }

            if (showDebugLog)
            {
                Debug.Log($"[DamageSystem] Area damage at {center}, radius={radius}, hit {hitCount} targets");
            }
        }

        /// <summary>
        /// ダメージ計算
        /// </summary>
        private float CalculateDamage(
            float baseDamage,
            DamageType damageType,
            MagicElement element,
            GameObject target)
        {
            float damage = baseDamage;

            // 属性相性（将来拡張）
            // float elementMultiplier = GetElementMultiplier(element, target);
            // damage *= elementMultiplier;

            // 防御力計算（将来拡張）
            // var stats = target.GetComponent<CharacterStats>();
            // if (stats != null)
            // {
            //     float defense = damageType == DamageType.Physical ? stats.PhysicalDefense : stats.MagicalDefense;
            //     damage = Mathf.Max(1, damage - defense);
            // }

            return Mathf.Max(0, damage);
        }

        /// <summary>
        /// ダメージ数値表示
        /// </summary>
        private void ShowDamageNumber(Vector3 position, float damage, bool isCritical, MagicElement element)
        {
            if (damageNumberPrefab == null) return;

            var dmgObj = Instantiate(damageNumberPrefab, position, Quaternion.identity);
            var dmgNumber = dmgObj.GetComponent<DamageNumber>();

            if (dmgNumber != null)
            {
                dmgNumber.Initialize(Mathf.RoundToInt(damage), isCritical, GetElementColor(element));
            }

            Destroy(dmgObj, damageNumberDuration);
        }

        /// <summary>
        /// ヒットエフェクト生成
        /// </summary>
        private void SpawnHitEffect(Vector3 position, MagicElement element)
        {
            // TODO: 属性別エフェクトの切り替え
            if (defaultHitEffectPrefab != null)
            {
                var effect = Instantiate(defaultHitEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        /// <summary>
        /// 属性の色を取得
        /// </summary>
        private Color GetElementColor(MagicElement element)
        {
            return element switch
            {
                MagicElement.Fire => new Color(1f, 0.4f, 0.1f),      // オレンジ
                MagicElement.Ice => new Color(0.5f, 0.8f, 1f),       // 水色
                MagicElement.Lightning => new Color(1f, 1f, 0.3f),   // 黄色
                MagicElement.Wind => new Color(0.6f, 1f, 0.6f),      // 薄緑
                MagicElement.Earth => new Color(0.6f, 0.4f, 0.2f),   // 茶色
                MagicElement.Light => Color.white,                    // 白
                MagicElement.Dark => new Color(0.5f, 0.2f, 0.8f),    // 紫
                _ => Color.white
            };
        }

        /// <summary>
        /// 属性相性倍率を取得（将来拡張用）
        /// </summary>
        private float GetElementMultiplier(MagicElement attackElement, MagicElement defenseElement)
        {
            // 相性表（将来実装）
            // Fire > Ice, Ice > Wind, Wind > Earth, Earth > Lightning, Lightning > Fire
            // Light <-> Dark（相互に強い）

            return 1f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // デバッグ用：範囲ダメージの可視化など
        }
#endif
    }
}
