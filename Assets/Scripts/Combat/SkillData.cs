using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// スキルの種類
    /// </summary>
    public enum SkillType
    {
        Light,      // 軽攻撃
        Heavy,      // 重攻撃
        Skill,      // スキル
        Ultimate    // 必殺技
    }

    /// <summary>
    /// カメラカットイン演出データ
    /// </summary>
    [System.Serializable]
    public class CameraCutInData
    {
        [Tooltip("カットイン用スプライト")]
        public Sprite cutInSprite;
        
        [Tooltip("カットイン表示時間")]
        public float duration = 0.5f;
        
        [Tooltip("ズーム倍率")]
        public float zoomMultiplier = 1.5f;
        
        [Tooltip("スローモーション倍率 (0.1 = 10%速度)")]
        [Range(0.01f, 1f)]
        public float slowMotionScale = 0.3f;
        
        [Tooltip("画面揺れの強さ")]
        public float shakeIntensity = 0.5f;
    }

    /// <summary>
    /// スキルデータ (ScriptableObject)
    /// VRoidキャラクター + Mixamoアニメーション用の技定義
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/Combat/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("基本情報")]
        [Tooltip("スキル名")]
        public string skillName;
        
        [Tooltip("スキルの説明")]
        [TextArea(2, 4)]
        public string description;
        
        [Tooltip("スキルの種類")]
        public SkillType skillType = SkillType.Light;
        
        [Tooltip("スキルアイコン")]
        public Sprite icon;

        [Header("アニメーション")]
        [Tooltip("再生するアニメーションクリップ")]
        public AnimationClip animation;
        
        [Tooltip("アニメーション再生速度")]
        [Range(0.1f, 3f)]
        public float animationSpeed = 1f;
        
        [Tooltip("アニメーショントリガー名 (Animator用)")]
        public string animationTrigger;

        [Header("ダメージ")]
        [Tooltip("基本ダメージ")]
        public float baseDamage = 10f;

        [Tooltip("ダメージ倍率")]
        public float damageMultiplier = 1f;

        [Tooltip("ダメージタイプ")]
        public DamageType damageType = DamageType.Physical;

        [Tooltip("魔法属性")]
        public MagicElement element = MagicElement.None;

        [Tooltip("クリティカル率 (0-1)")]
        [Range(0f, 1f)]
        public float criticalRate = 0.1f;

        [Tooltip("クリティカルダメージ倍率")]
        public float criticalMultiplier = 1.5f;

        [Tooltip("ノックバック力")]
        public float knockbackForce = 5f;

        [Header("投射物設定")]
        [Tooltip("投射物を使用するか")]
        public bool useProjectile = false;

        [Tooltip("投射物プレハブ")]
        public GameObject projectilePrefab;

        [Tooltip("投射物の速度")]
        public float projectileSpeed = 15f;

        [Tooltip("投射物の生存時間")]
        public float projectileLifetime = 3f;

        [Tooltip("ホーミング機能")]
        public bool projectileHoming = false;

        [Tooltip("ホーミング強度")]
        [Range(0f, 20f)]
        public float homingStrength = 5f;

        [Tooltip("投射物発射位置オフセット")]
        public Vector3 projectileSpawnOffset = new Vector3(0, 1.2f, 0.5f);

        [Header("クールダウン")]
        [Tooltip("クールダウン時間 (秒)")]
        public float cooldown = 0f;
        
        [Tooltip("消費スタミナ/MP")]
        public float cost = 0f;

        [Header("表情 (VRM BlendShape)")]
        [Tooltip("VRM表情名 (Angry, Fun, Surprised等)")]
        public string expressionName = "Neutral";
        
        [Tooltip("表情の強さ (0-1)")]
        [Range(0f, 1f)]
        public float expressionWeight = 1f;
        
        [Tooltip("表情の持続時間")]
        public float expressionDuration = 0.5f;

        [Header("エフェクト")]
        [Tooltip("エフェクトプレハブ")]
        public GameObject effectPrefab;
        
        [Tooltip("エフェクト発生タイミング (アニメーション正規化時間 0-1)")]
        [Range(0f, 1f)]
        public float effectTiming = 0.3f;
        
        [Tooltip("エフェクト位置オフセット")]
        public Vector3 effectOffset = Vector3.zero;
        
        [Tooltip("エフェクトを対象に追従させるか")]
        public bool effectFollowTarget = false;

        [Header("ヒットエフェクト")]
        [Tooltip("ヒット時のエフェクトプレハブ")]
        public GameObject hitEffectPrefab;
        
        [Tooltip("ヒット時の画面揺れ")]
        public float hitShakeIntensity = 0.2f;

        [Header("サウンド")]
        [Tooltip("スキル発動SE")]
        public AudioClip activationSound;
        
        [Tooltip("ヒットSE")]
        public AudioClip hitSound;
        
        [Tooltip("SE音量")]
        [Range(0f, 1f)]
        public float soundVolume = 1f;

        [Header("カメラ演出")]
        [Tooltip("カメラカットインを使用するか")]
        public bool useCameraCutIn = false;
        
        [Tooltip("カメラカットインデータ")]
        public CameraCutInData cutInData;

        [Header("コンボ")]
        [Tooltip("次のコンボスキル (null = コンボ終了)")]
        public SkillData nextComboSkill;
        
        [Tooltip("コンボ受付時間 (秒)")]
        public float comboWindow = 0.5f;

        /// <summary>
        /// 最終ダメージを計算
        /// </summary>
        public float CalculateDamage(bool isCritical = false)
        {
            float damage = baseDamage * damageMultiplier;
            if (isCritical)
            {
                damage *= criticalMultiplier;
            }
            return damage;
        }

        /// <summary>
        /// クリティカル判定
        /// </summary>
        public bool RollCritical()
        {
            return Random.value < criticalRate;
        }

        /// <summary>
        /// DamageInfoを生成
        /// </summary>
        public DamageInfo CreateDamageInfo(
            GameObject attacker,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 knockbackDirection)
        {
            bool isCritical = RollCritical();
            float damage = CalculateDamage(isCritical);

            return new DamageInfo(
                damage,
                isCritical,
                knockbackForce,
                knockbackDirection,
                attacker,
                this,
                hitPoint,
                hitNormal
            );
        }
    }
}
