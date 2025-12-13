using UnityEngine;
using System.Collections.Generic;

namespace RPG.Combat
{
    /// <summary>
    /// 攻撃判定用ヒットボックス
    /// アニメーションイベントで有効化/無効化を制御
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitBox : MonoBehaviour
    {
        [Header("設定")]
        [Tooltip("このヒットボックスの所有者")]
        [SerializeField] private GameObject owner;
        
        [Tooltip("ヒット可能なレイヤー")]
        [SerializeField] private LayerMask targetLayers = ~0;
        
        [Tooltip("1回の攻撃で同じ対象に複数回ヒットを許可するか")]
        [SerializeField] private bool allowMultipleHits = false;
        
        [Tooltip("デバッグ表示")]
        [SerializeField] private bool debugMode = false;

        /// <summary>現在使用中のスキル</summary>
        public SkillData CurrentSkill { get; private set; }
        
        /// <summary>ヒットボックスが有効か</summary>
        public bool IsActive { get; private set; }

        // 既にヒットした対象を記録
        private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
        
        private Collider hitCollider;

        /// <summary>ヒット時のイベント</summary>
        public event System.Action<DamageInfo, GameObject> OnHit;

        private void Awake()
        {
            hitCollider = GetComponent<Collider>();
            hitCollider.isTrigger = true;
            
            if (owner == null)
            {
                owner = transform.root.gameObject;
            }
            
            // 初期状態は無効
            SetActive(false);
        }

        /// <summary>
        /// ヒットボックスを有効化
        /// </summary>
        /// <param name="skill">使用するスキル</param>
        public void Activate(SkillData skill)
        {
            CurrentSkill = skill;
            hitTargets.Clear();
            SetActive(true);
            
            if (debugMode)
            {
                Debug.Log($"[HitBox] Activated with skill: {skill?.skillName ?? "null"}");
            }
        }

        /// <summary>
        /// ヒットボックスを無効化
        /// </summary>
        public void Deactivate()
        {
            SetActive(false);
            CurrentSkill = null;
            
            if (debugMode)
            {
                Debug.Log("[HitBox] Deactivated");
            }
        }

        private void SetActive(bool active)
        {
            IsActive = active;
            hitCollider.enabled = active;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive || CurrentSkill == null) return;
            
            // レイヤーチェック
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
            
            // 自分自身は除外
            if (other.transform.root == owner.transform.root) return;
            
            // 既にヒット済みかチェック
            GameObject targetRoot = other.transform.root.gameObject;
            if (!allowMultipleHits && hitTargets.Contains(targetRoot)) return;
            
            hitTargets.Add(targetRoot);
            
            // ダメージ処理
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = (other.transform.position - transform.position).normalized;
                Vector3 attackDirection = owner.transform.forward;
                
                DamageInfo damageInfo = DamageInfo.FromSkill(
                    CurrentSkill,
                    owner,
                    hitPoint,
                    hitNormal,
                    attackDirection
                );
                
                damageable.TakeDamage(damageInfo);
                OnHit?.Invoke(damageInfo, other.gameObject);
                
                if (debugMode)
                {
                    Debug.Log($"[HitBox] Hit {other.gameObject.name} for {damageInfo.damage} damage" +
                              (damageInfo.isCritical ? " (CRITICAL!)" : ""));
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!debugMode) return;
            
            Gizmos.color = IsActive ? Color.red : Color.gray;
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
