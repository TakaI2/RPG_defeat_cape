using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// Enemy用の魔法制御コンポーネント
    /// SkillData/MagicSystemを使用してPlayerと同じエフェクト・ダメージ処理を共有
    /// </summary>
    public class EnemyMagicController : MonoBehaviour
    {
        [Header("魔法設定")]
        [SerializeField] private SkillData[] availableSkills;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float attackRange = 15f;
        [SerializeField] private float minAttackInterval = 2f;

        [Header("詠唱エフェクト")]
        [SerializeField] private GameObject defaultChargeEffectPrefab;

        [Header("アニメーション")]
        [SerializeField] private Animator animator;
        [SerializeField] private string castTriggerName = "Cast";
        [SerializeField] private string attackTriggerName = "Attack";

        [Header("AI設定")]
        [SerializeField] private bool autoTargetPlayer = true;
        [SerializeField] private string playerTag = "Player";

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;

        [Header("テスト用")]
        [SerializeField] private bool autoAttack = false;
        [SerializeField] private float autoAttackInterval = 3f;
        [SerializeField] private KeyCode testAttackKey = KeyCode.M;

        // 状態管理
        private Transform _currentTarget;
        private float _lastAttackTime;
        private bool _isCasting;
        private Coroutine _castCoroutine;

        public bool IsCasting => _isCasting;
        public Transform CurrentTarget => _currentTarget;
        public float AttackRange => attackRange;

        private void Start()
        {
            if (firePoint == null)
            {
                firePoint = transform;
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // 自動でPlayerをターゲット
            if (autoTargetPlayer)
            {
                FindPlayer();
            }
        }

        private void Update()
        {
            // テスト用キー入力
            if (UnityEngine.Input.GetKeyDown(testAttackKey))
            {
                if (showDebugLog)
                {
                    Debug.Log($"[EnemyMagicController] Test attack key pressed (M)");
                }
                AttackWithRandomSkill();
            }

            // 自動攻撃
            if (autoAttack && CanAttack())
            {
                if (Time.time - _lastAttackTime >= autoAttackInterval)
                {
                    AttackWithRandomSkill();
                }
            }
        }

        private void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                _currentTarget = player.transform;
            }
        }

        /// <summary>
        /// ターゲットを設定
        /// </summary>
        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        /// <summary>
        /// 攻撃可能か判定
        /// </summary>
        public bool CanAttack()
        {
            if (_isCasting) return false;
            if (_currentTarget == null) return false;
            if (Time.time - _lastAttackTime < minAttackInterval) return false;

            float distance = Vector3.Distance(transform.position, _currentTarget.position);
            return distance <= attackRange;
        }

        /// <summary>
        /// ランダムなスキルで攻撃
        /// </summary>
        public void AttackWithRandomSkill()
        {
            if (availableSkills == null || availableSkills.Length == 0) return;

            var skill = availableSkills[Random.Range(0, availableSkills.Length)];
            AttackWithSkill(skill);
        }

        /// <summary>
        /// 指定スキルで攻撃
        /// </summary>
        public void AttackWithSkill(SkillData skill)
        {
            if (skill == null || _isCasting) return;

            _castCoroutine = StartCoroutine(CastMagicCoroutine(skill));
        }

        /// <summary>
        /// インデックスでスキルを使用
        /// </summary>
        public void AttackWithSkillIndex(int index)
        {
            if (availableSkills == null || index < 0 || index >= availableSkills.Length) return;

            AttackWithSkill(availableSkills[index]);
        }

        /// <summary>
        /// 魔法詠唱コルーチン
        /// </summary>
        private IEnumerator CastMagicCoroutine(SkillData skill)
        {
            _isCasting = true;

            if (showDebugLog)
            {
                Debug.Log($"[EnemyMagicController] {gameObject.name} casting {skill.skillName}...");
            }

            // ターゲット方向を向く
            if (_currentTarget != null)
            {
                Vector3 lookDir = (_currentTarget.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            // 詠唱アニメーション
            if (animator != null)
            {
                animator.SetTrigger(castTriggerName);
            }

            // 詠唱エフェクト
            GameObject chargeEffect = null;
            var chargePrefab = skill.effectPrefab != null ? skill.effectPrefab : defaultChargeEffectPrefab;
            if (chargePrefab != null)
            {
                chargeEffect = Instantiate(chargePrefab, firePoint.position, firePoint.rotation, firePoint);
            }

            // 詠唱時間（SkillDataのcooldownの半分を詠唱時間として使用、または固定値）
            float castTime = skill.cooldown > 0 ? skill.cooldown * 0.3f : 1f;
            yield return new WaitForSeconds(castTime);

            // 詠唱エフェクト削除
            if (chargeEffect != null)
            {
                Destroy(chargeEffect);
            }

            // 魔法発射
            FireMagic(skill);

            _lastAttackTime = Time.time;
            _isCasting = false;
        }

        /// <summary>
        /// 魔法発射
        /// </summary>
        private void FireMagic(SkillData skill)
        {
            // MagicSystemを使用（共通処理）
            if (MagicSystem.Instance != null)
            {
                MagicSystem.Instance.FireMagic(skill, firePoint, _currentTarget, gameObject);
            }
            else
            {
                // MagicSystemがない場合は直接生成
                FireMagicDirect(skill);
            }

            // 攻撃アニメーション（発射時）
            if (animator != null)
            {
                animator.SetTrigger(attackTriggerName);
            }

            if (showDebugLog)
            {
                Debug.Log($"[EnemyMagicController] {gameObject.name} fired {skill.skillName}!");
            }
        }

        /// <summary>
        /// MagicSystemなしで直接発射
        /// </summary>
        private void FireMagicDirect(SkillData skill)
        {
            if (skill.projectilePrefab == null) return;

            Vector3 direction = _currentTarget != null
                ? (_currentTarget.position - firePoint.position).normalized
                : firePoint.forward;

            var projectileObj = Instantiate(
                skill.projectilePrefab,
                firePoint.position,
                Quaternion.LookRotation(direction)
            );

            var magicProjectile = projectileObj.GetComponent<MagicProjectile>();
            if (magicProjectile != null)
            {
                magicProjectile.Initialize(skill, gameObject, direction, _currentTarget);
            }
        }

        /// <summary>
        /// 詠唱キャンセル
        /// </summary>
        public void CancelCast()
        {
            if (_castCoroutine != null)
            {
                StopCoroutine(_castCoroutine);
                _castCoroutine = null;
            }
            _isCasting = false;
        }

        /// <summary>
        /// 利用可能なスキル一覧を取得
        /// </summary>
        public SkillData[] GetAvailableSkills()
        {
            return availableSkills;
        }

        /// <summary>
        /// スキルを追加
        /// </summary>
        public void AddSkill(SkillData skill)
        {
            var list = new List<SkillData>(availableSkills ?? new SkillData[0]);
            list.Add(skill);
            availableSkills = list.ToArray();
        }

        private void OnDrawGizmosSelected()
        {
            // 攻撃範囲を表示
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // ターゲットへの線
            if (_currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
    }
}
