using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// 被ダメージ判定用ハートボックス
    /// IDamageableを実装したコンポーネントにダメージを転送
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HurtBox : MonoBehaviour
    {
        [Header("設定")]
        [Tooltip("ダメージ転送先 (null = 親階層から自動検索)")]
        [SerializeField] private Component damageReceiver;
        
        [Tooltip("ダメージ倍率 (部位によるダメージ補正)")]
        [SerializeField] private float damageMultiplier = 1f;
        
        [Tooltip("この部位への攻撃は常にクリティカル")]
        [SerializeField] private bool isWeakPoint = false;
        
        [Tooltip("無敵状態か")]
        [SerializeField] private bool isInvincible = false;
        
        [Tooltip("デバッグ表示")]
        [SerializeField] private bool debugMode = false;

        private IDamageable damageable;
        private Collider hurtCollider;

        /// <summary>無敵状態の設定</summary>
        public bool IsInvincible
        {
            get => isInvincible;
            set => isInvincible = value;
        }

        /// <summary>ダメージ受信時のイベント</summary>
        public event System.Action<DamageInfo> OnDamageReceived;

        private void Awake()
        {
            hurtCollider = GetComponent<Collider>();
            hurtCollider.isTrigger = true;
            
            // ダメージ転送先を探す
            if (damageReceiver != null)
            {
                damageable = damageReceiver as IDamageable;
            }
            
            if (damageable == null)
            {
                damageable = GetComponentInParent<IDamageable>();
            }
            
            if (damageable == null && debugMode)
            {
                Debug.LogWarning($"[HurtBox] No IDamageable found for {gameObject.name}");
            }
        }

        /// <summary>
        /// ダメージを受ける (HitBoxから呼ばれる)
        /// </summary>
        public void ReceiveDamage(DamageInfo damageInfo)
        {
            if (isInvincible)
            {
                if (debugMode)
                {
                    Debug.Log($"[HurtBox] {gameObject.name} is invincible, damage blocked");
                }
                return;
            }
            
            if (damageable == null) return;
            
            // ダメージ補正を適用
            DamageInfo modifiedInfo = damageInfo;
            modifiedInfo.damage *= damageMultiplier;
            
            // 弱点ならクリティカル化
            if (isWeakPoint && !modifiedInfo.isCritical)
            {
                modifiedInfo.isCritical = true;
                modifiedInfo.damage *= damageInfo.skill?.criticalMultiplier ?? 1.5f;
            }
            
            // ヒット位置を更新
            modifiedInfo.hitPoint = transform.position;
            
            damageable.TakeDamage(modifiedInfo);
            OnDamageReceived?.Invoke(modifiedInfo);
            
            if (debugMode)
            {
                Debug.Log($"[HurtBox] {gameObject.name} received {modifiedInfo.damage} damage" +
                          (isWeakPoint ? " (WEAK POINT!)" : ""));
            }
        }

        /// <summary>
        /// 一時的に無敵にする
        /// </summary>
        public void SetInvincibleForDuration(float duration)
        {
            if (duration <= 0) return;
            StartCoroutine(InvincibilityCoroutine(duration));
        }

        private System.Collections.IEnumerator InvincibilityCoroutine(float duration)
        {
            isInvincible = true;
            yield return new WaitForSeconds(duration);
            isInvincible = false;
        }

        private void OnDrawGizmos()
        {
            if (!debugMode) return;
            
            Gizmos.color = isInvincible ? Color.cyan : (isWeakPoint ? Color.yellow : Color.green);
            Gizmos.matrix = transform.localToWorldMatrix;
            
            var boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            
            var sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
        }
    }
}
