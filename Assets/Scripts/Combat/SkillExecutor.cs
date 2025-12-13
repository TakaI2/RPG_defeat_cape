using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RPG.Combat
{
    /// <summary>
    /// スキル実行を管理するコンポーネント
    /// アニメーション再生、エフェクト生成、表情変更、カメラ演出を統括
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("Animatorコンポーネント")]
        [SerializeField] private Animator animator;
        
        [Tooltip("ヒットボックス (複数可)")]
        [SerializeField] private HitBox[] hitBoxes;
        
        [Tooltip("SE再生用AudioSource")]
        [SerializeField] private AudioSource audioSource;

        [Header("設定")]
        [Tooltip("スキル実行中は移動を無効化")]
        [SerializeField] private bool disableMovementDuringSkill = true;
        
        [Tooltip("デバッグモード")]
        [SerializeField] private bool debugMode = false;

        // 状態
        private bool isExecutingSkill = false;
        private SkillData currentSkill = null;
        private Coroutine skillCoroutine = null;
        
        // クールダウン管理
        private Dictionary<SkillData, float> cooldowns = new Dictionary<SkillData, float>();
        
        // コンボ管理
        private SkillData pendingComboSkill = null;
        private float comboWindowEndTime = 0f;
        private bool comboInputReceived = false;

        /// <summary>スキル実行中か</summary>
        public bool IsExecutingSkill => isExecutingSkill;
        
        /// <summary>現在のスキル</summary>
        public SkillData CurrentSkill => currentSkill;

        // イベント
        public event System.Action<SkillData> OnSkillStarted;
        public event System.Action<SkillData> OnSkillEnded;
        public event System.Action<SkillData, DamageInfo, GameObject> OnSkillHit;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // ヒットボックスのイベント登録
            foreach (var hitBox in hitBoxes)
            {
                if (hitBox != null)
                {
                    hitBox.OnHit += HandleHit;
                }
            }
        }

        private void Update()
        {
            // クールダウン更新
            UpdateCooldowns();
            
            // コンボ入力チェック
            if (comboInputReceived && pendingComboSkill != null && Time.time < comboWindowEndTime)
            {
                // コンボウィンドウ内で入力があれば次のスキルを実行
                comboInputReceived = false;
                ExecuteSkillInternal(pendingComboSkill);
            }
        }

        private void UpdateCooldowns()
        {
            var keysToRemove = new List<SkillData>();
            
            foreach (var kvp in cooldowns)
            {
                if (Time.time >= kvp.Value)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                cooldowns.Remove(key);
            }
        }

        /// <summary>
        /// スキルを実行
        /// </summary>
        /// <param name="skill">実行するスキル</param>
        /// <returns>実行開始できたか</returns>
        public bool ExecuteSkill(SkillData skill)
        {
            if (skill == null)
            {
                Debug.LogWarning("[SkillExecutor] Skill is null");
                return false;
            }
            
            // クールダウンチェック
            if (IsOnCooldown(skill))
            {
                if (debugMode) Debug.Log($"[SkillExecutor] {skill.skillName} is on cooldown");
                return false;
            }
            
            // コンボ中の入力
            if (isExecutingSkill && pendingComboSkill != null)
            {
                if (skill == pendingComboSkill || skill == currentSkill?.nextComboSkill)
                {
                    comboInputReceived = true;
                    if (debugMode) Debug.Log($"[SkillExecutor] Combo input received for {skill.skillName}");
                    return true;
                }
                else if (debugMode)
                {
                    Debug.Log($"[SkillExecutor] Cannot execute {skill.skillName} during skill execution");
                }
                return false;
            }
            
            // 通常実行
            if (isExecutingSkill)
            {
                if (debugMode) Debug.Log($"[SkillExecutor] Already executing skill");
                return false;
            }
            
            return ExecuteSkillInternal(skill);
        }

        private bool ExecuteSkillInternal(SkillData skill)
        {
            if (skillCoroutine != null)
            {
                StopCoroutine(skillCoroutine);
            }
            
            skillCoroutine = StartCoroutine(ExecuteSkillCoroutine(skill));
            return true;
        }

        private IEnumerator ExecuteSkillCoroutine(SkillData skill)
        {
            isExecutingSkill = true;
            currentSkill = skill;
            
            if (debugMode) Debug.Log($"[SkillExecutor] Starting skill: {skill.skillName}");
            
            OnSkillStarted?.Invoke(skill);
            
            // アニメーション開始
            if (animator != null && !string.IsNullOrEmpty(skill.animationTrigger))
            {
                animator.speed = skill.animationSpeed;
                animator.SetTrigger(skill.animationTrigger);
            }
            
            // 発動SE再生
            if (skill.activationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(skill.activationSound, skill.soundVolume);
            }
            
            // 表情変更 (VRM用 - 別途実装)
            // TODO: VRM ExpressionController と連携
            
            // エフェクト生成タイミングを待つ
            if (skill.effectPrefab != null && skill.animation != null)
            {
                float effectTime = skill.animation.length * skill.effectTiming / skill.animationSpeed;
                yield return new WaitForSeconds(effectTime);
                SpawnEffect(skill);
            }
            
            // アニメーション終了を待つ
            if (skill.animation != null)
            {
                float remainingTime = skill.animation.length / skill.animationSpeed;
                if (skill.effectPrefab != null)
                {
                    remainingTime -= skill.animation.length * skill.effectTiming / skill.animationSpeed;
                }
                
                // コンボウィンドウ設定
                if (skill.nextComboSkill != null)
                {
                    pendingComboSkill = skill.nextComboSkill;
                    comboWindowEndTime = Time.time + remainingTime + skill.comboWindow;
                    comboInputReceived = false;
                }
                
                yield return new WaitForSeconds(Mathf.Max(0, remainingTime));
            }
            else
            {
                yield return new WaitForSeconds(0.5f); // デフォルト待機
            }
            
            // クールダウン開始
            if (skill.cooldown > 0)
            {
                cooldowns[skill] = Time.time + skill.cooldown;
            }
            
            // 終了処理
            if (animator != null)
            {
                animator.speed = 1f;
            }
            
            // コンボが成立しなかった場合はリセット
            if (!comboInputReceived)
            {
                pendingComboSkill = null;
            }
            
            isExecutingSkill = false;
            currentSkill = null;
            
            if (debugMode) Debug.Log($"[SkillExecutor] Finished skill: {skill.skillName}");
            
            OnSkillEnded?.Invoke(skill);
        }

        /// <summary>
        /// スキルを中断
        /// </summary>
        public void CancelSkill()
        {
            if (skillCoroutine != null)
            {
                StopCoroutine(skillCoroutine);
                skillCoroutine = null;
            }
            
            DeactivateAllHitBoxes();
            
            if (animator != null)
            {
                animator.speed = 1f;
            }
            
            var previousSkill = currentSkill;
            isExecutingSkill = false;
            currentSkill = null;
            pendingComboSkill = null;
            
            if (previousSkill != null)
            {
                OnSkillEnded?.Invoke(previousSkill);
            }
        }

        /// <summary>
        /// スキルがクールダウン中か確認
        /// </summary>
        public bool IsOnCooldown(SkillData skill)
        {
            return cooldowns.ContainsKey(skill) && Time.time < cooldowns[skill];
        }

        /// <summary>
        /// クールダウン残り時間を取得
        /// </summary>
        public float GetCooldownRemaining(SkillData skill)
        {
            if (!cooldowns.ContainsKey(skill)) return 0f;
            return Mathf.Max(0, cooldowns[skill] - Time.time);
        }

        private void SpawnEffect(SkillData skill)
        {
            if (skill.effectPrefab == null) return;
            
            Vector3 position = transform.position + transform.TransformDirection(skill.effectOffset);
            Quaternion rotation = transform.rotation;
            
            GameObject effect = Instantiate(skill.effectPrefab, position, rotation);
            
            if (skill.effectFollowTarget)
            {
                effect.transform.SetParent(transform);
            }
            
            // 自動削除 (ParticleSystemの場合)
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null && !ps.main.loop)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }

        private void HandleHit(DamageInfo damageInfo, GameObject target)
        {
            if (currentSkill == null) return;
            
            // ヒットSE
            if (currentSkill.hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(currentSkill.hitSound, currentSkill.soundVolume);
            }
            
            // ヒットエフェクト
            if (currentSkill.hitEffectPrefab != null)
            {
                Instantiate(
                    currentSkill.hitEffectPrefab,
                    damageInfo.hitPoint,
                    Quaternion.LookRotation(damageInfo.hitNormal)
                );
            }
            
            OnSkillHit?.Invoke(currentSkill, damageInfo, target);
        }

        /// <summary>
        /// ヒットボックスを有効化 (アニメーションイベントから呼ばれる)
        /// </summary>
        public void ActivateHitBox(int index = 0)
        {
            if (index >= 0 && index < hitBoxes.Length && hitBoxes[index] != null)
            {
                hitBoxes[index].Activate(currentSkill);
            }
        }

        /// <summary>
        /// ヒットボックスを無効化 (アニメーションイベントから呼ばれる)
        /// </summary>
        public void DeactivateHitBox(int index = 0)
        {
            if (index >= 0 && index < hitBoxes.Length && hitBoxes[index] != null)
            {
                hitBoxes[index].Deactivate();
            }
        }

        /// <summary>
        /// 全ヒットボックスを無効化
        /// </summary>
        public void DeactivateAllHitBoxes()
        {
            foreach (var hitBox in hitBoxes)
            {
                if (hitBox != null)
                {
                    hitBox.Deactivate();
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var hitBox in hitBoxes)
            {
                if (hitBox != null)
                {
                    hitBox.OnHit -= HandleHit;
                }
            }
        }
    }
}
