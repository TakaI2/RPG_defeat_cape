using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Combat;

namespace RPGDefete.Character
{
    /// <summary>
    /// キャラクタータイプ
    /// </summary>
    public enum CharacterType
    {
        Player,
        NPC,
        Enemy
    }

    /// <summary>
    /// キャラクター状態
    /// </summary>
    public enum CharacterState
    {
        Idle,
        Walking,
        Running,
        Sitting,
        InCombat,
        Talking,
        Interacting,
        Incapacitated
    }

    /// <summary>
    /// 感情タイプ
    /// </summary>
    public enum Emotion
    {
        Neutral,
        Joy,
        Anger,
        Sadness,
        Fear,
        Surprise,
        Disgust,
        Trust,
        Anticipation
    }

    /// <summary>
    /// 手のサイド
    /// </summary>
    public enum HandSide
    {
        Right,
        Left
    }

    /// <summary>
    /// プレイヤーとNPCを統一的に管理するキャラクターシステム
    /// VRM制御コンポーネント群を統合し、HP・感情による表情変化を実現
    /// </summary>
    public class GameCharacter : MonoBehaviour, IDamageable
    {
        [Header("基本設定")]
        [SerializeField] private CharacterType characterType = CharacterType.NPC;
        [SerializeField] private string characterName = "Character";
        [SerializeField] private GameObject vrmModel;

        [Header("ステータス")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float currentHP;
        [SerializeField] private Emotion currentEmotion = Emotion.Neutral;

        [Header("コントローラー参照")]
        [SerializeField] private VRMExpressionController expressionController;
        [SerializeField] private VRMAnimationController animationController;
        [SerializeField] private VRMFinalIKController ikController;
        [SerializeField] private VRMEyeGazeController eyeGazeController;
        [SerializeField] private CharacterNavigator navigator;

        [Header("インタラクション")]
        [SerializeField] private List<InteractionPoint> interactionPoints = new List<InteractionPoint>();

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;

        // プロパティ
        public CharacterType Type => characterType;
        public string CharacterName => characterName;
        public CharacterState State { get; set; } = CharacterState.Idle;
        public float HP => currentHP;
        public float MaxHP => maxHP;
        public float HPRatio => maxHP > 0 ? currentHP / maxHP : 0f;
        public bool IsDead => currentHP <= 0;
        public Emotion CurrentEmotion => currentEmotion;

        // IDamageable実装プロパティ
        public float CurrentHealth => currentHP;
        public float MaxHealth => maxHP;
        public bool IsAlive => !IsDead;
        public CharacterNavigator Navigator => navigator;
        public VRMExpressionController ExpressionController => expressionController;
        public VRMAnimationController AnimationController => animationController;
        public VRMFinalIKController IKController => ikController;
        public VRMEyeGazeController EyeGazeController => eyeGazeController;

        // イベント
        public event Action<float, float> OnHPChanged;
        public event Action<Emotion> OnEmotionChanged;
        public event Action OnDeath;

        private void Awake()
        {
            InitializeComponents();
            CollectInteractionPoints();
        }

        private void Start()
        {
            currentHP = maxHP;

            if (showDebugLog)
            {
                Debug.Log($"[GameCharacter] {characterName} initialized. Type={characterType}, HP={currentHP}/{maxHP}");
            }
        }

        private void Update()
        {
            UpdateExpressionByHP();
        }

        /// <summary>
        /// コンポーネント参照の初期化
        /// </summary>
        private void InitializeComponents()
        {
            if (expressionController == null)
                expressionController = GetComponentInChildren<VRMExpressionController>();
            if (animationController == null)
                animationController = GetComponentInChildren<VRMAnimationController>();
            if (ikController == null)
                ikController = GetComponentInChildren<VRMFinalIKController>();
            if (eyeGazeController == null)
                eyeGazeController = GetComponentInChildren<VRMEyeGazeController>();
            if (navigator == null)
                navigator = GetComponent<CharacterNavigator>();
        }

        /// <summary>
        /// インタラクションポイントを収集
        /// </summary>
        private void CollectInteractionPoints()
        {
            var points = GetComponentsInChildren<InteractionPoint>();
            interactionPoints = new List<InteractionPoint>(points);
        }

        /// <summary>
        /// 指定タイプのインタラクションポイントを取得
        /// </summary>
        public InteractionPoint GetInteractionPoint(InteractionPointType type)
        {
            return interactionPoints.Find(p => p.PointType == type);
        }

        /// <summary>
        /// 全インタラクションポイントを取得
        /// </summary>
        public IReadOnlyList<InteractionPoint> GetAllInteractionPoints()
        {
            return interactionPoints;
        }

        /// <summary>
        /// 手のTransformを取得
        /// </summary>
        public Transform GetHandTransform(HandSide side)
        {
            var type = InteractionPointType.Hand;
            var point = interactionPoints.Find(p =>
                p.PointType == type && p.name.Contains(side.ToString()));
            return point?.transform;
        }

        #region IDamageable実装

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (IsDead) return;

            float previousHP = currentHP;
            currentHP = Mathf.Max(0, currentHP - damageInfo.damage);

            if (showDebugLog)
            {
                Debug.Log($"[GameCharacter] {characterName} took {damageInfo.damage} damage! HP: {previousHP} -> {currentHP}");
            }

            OnHPChanged?.Invoke(currentHP, maxHP);
            PlayDamageReaction();

            if (currentHP <= 0)
            {
                Die();
            }
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        #endregion

        /// <summary>
        /// 直接ダメージを与える
        /// </summary>
        public void TakeDamage(float damage)
        {
            TakeDamage(new DamageInfo(
                damage,
                false,  // isCritical
                0f,     // knockbackForce
                Vector3.zero,  // knockbackDirection
                null,   // attacker
                null,   // skill
                transform.position,  // hitPoint
                Vector3.up   // hitNormal
            ));
        }

        /// <summary>
        /// HP回復
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead) return;

            float previousHP = currentHP;
            currentHP = Mathf.Min(maxHP, currentHP + amount);

            if (showDebugLog)
            {
                Debug.Log($"[GameCharacter] {characterName} healed {amount}! HP: {previousHP} -> {currentHP}");
            }

            OnHPChanged?.Invoke(currentHP, maxHP);
        }

        /// <summary>
        /// HPを設定（直接）
        /// </summary>
        public void SetHP(float hp)
        {
            currentHP = Mathf.Clamp(hp, 0, maxHP);
            OnHPChanged?.Invoke(currentHP, maxHP);

            if (currentHP <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 感情を設定
        /// </summary>
        public void SetEmotion(Emotion emotion)
        {
            if (currentEmotion != emotion)
            {
                currentEmotion = emotion;
                OnEmotionChanged?.Invoke(emotion);
                ApplyEmotionExpression();

                if (showDebugLog)
                {
                    Debug.Log($"[GameCharacter] {characterName} emotion changed to {emotion}");
                }
            }
        }

        /// <summary>
        /// HP連動表情更新
        /// </summary>
        private void UpdateExpressionByHP()
        {
            if (expressionController == null || IsDead) return;

            float hpRatio = HPRatio;

            // HPに応じた微妙な表情変化（ベースレイヤー）
            if (hpRatio > 0.7f)
            {
                // 通常 - 特に変化なし
            }
            else if (hpRatio > 0.4f)
            {
                // やや辛そう
                expressionController.SetExpression("sad", 0.15f);
            }
            else if (hpRatio > 0.2f)
            {
                // 苦しそう
                expressionController.SetExpression("sad", 0.3f);
            }
            else
            {
                // 瀕死
                expressionController.SetExpression("sad", 0.5f);
            }
        }

        /// <summary>
        /// 感情表情を適用
        /// </summary>
        private void ApplyEmotionExpression()
        {
            if (expressionController == null) return;

            string preset = currentEmotion switch
            {
                Emotion.Joy => "happy",
                Emotion.Anger => "angry",
                Emotion.Sadness => "sad",
                Emotion.Fear => "shocked",
                Emotion.Surprise => "confused",
                Emotion.Trust => "smile",
                _ => "neutral"
            };

            expressionController.StartPresetTransition(preset, 0.3f);
        }

        /// <summary>
        /// ダメージリアクション再生
        /// </summary>
        private void PlayDamageReaction()
        {
            // アニメーション
            animationController?.PlayAnimation("Hit");

            // 表情
            expressionController?.StartPresetTransition("shocked", 0.1f);

            // リセット
            StartCoroutine(ResetAfterDamage());
        }

        private IEnumerator ResetAfterDamage()
        {
            yield return new WaitForSeconds(0.5f);

            if (!IsDead)
            {
                ApplyEmotionExpression();
            }
        }

        /// <summary>
        /// 死亡処理
        /// </summary>
        private void Die()
        {
            State = CharacterState.Incapacitated;

            if (showDebugLog)
            {
                Debug.Log($"[GameCharacter] {characterName} died!");
            }

            OnDeath?.Invoke();

            // 死亡アニメーション
            animationController?.PlayAnimation("Death");

            // 移動停止
            navigator?.Stop();
        }

        /// <summary>
        /// 復活
        /// </summary>
        public void Revive(float hpPercent = 1f)
        {
            currentHP = maxHP * hpPercent;
            State = CharacterState.Idle;

            OnHPChanged?.Invoke(currentHP, maxHP);
            ApplyEmotionExpression();

            if (showDebugLog)
            {
                Debug.Log($"[GameCharacter] {characterName} revived with {currentHP} HP");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // インタラクションポイントを表示
            Gizmos.color = Color.cyan;
            foreach (var point in interactionPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.transform.position, 0.05f);
                }
            }
        }
    }
}
