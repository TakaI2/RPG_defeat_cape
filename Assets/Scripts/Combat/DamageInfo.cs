using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// ダメージ情報を格納する構造体
    /// </summary>
    [System.Serializable]
    public struct DamageInfo
    {
        /// <summary>ダメージ量</summary>
        public float damage;
        
        /// <summary>クリティカルヒットか</summary>
        public bool isCritical;
        
        /// <summary>ノックバック力</summary>
        public float knockbackForce;
        
        /// <summary>ノックバック方向</summary>
        public Vector3 knockbackDirection;
        
        /// <summary>攻撃者</summary>
        public GameObject attacker;
        
        /// <summary>使用したスキル</summary>
        public SkillData skill;
        
        /// <summary>ヒット位置</summary>
        public Vector3 hitPoint;
        
        /// <summary>ヒット法線</summary>
        public Vector3 hitNormal;

        public DamageInfo(
            float damage,
            bool isCritical,
            float knockbackForce,
            Vector3 knockbackDirection,
            GameObject attacker,
            SkillData skill,
            Vector3 hitPoint,
            Vector3 hitNormal)
        {
            this.damage = damage;
            this.isCritical = isCritical;
            this.knockbackForce = knockbackForce;
            this.knockbackDirection = knockbackDirection;
            this.attacker = attacker;
            this.skill = skill;
            this.hitPoint = hitPoint;
            this.hitNormal = hitNormal;
        }

        /// <summary>
        /// SkillDataからDamageInfoを生成
        /// </summary>
        public static DamageInfo FromSkill(
            SkillData skill,
            GameObject attacker,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 attackDirection)
        {
            bool isCritical = skill.RollCritical();
            float damage = skill.CalculateDamage(isCritical);
            
            return new DamageInfo(
                damage,
                isCritical,
                skill.knockbackForce,
                attackDirection.normalized,
                attacker,
                skill,
                hitPoint,
                hitNormal
            );
        }
    }

    /// <summary>
    /// ダメージを受けることができるインターフェース
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// ダメージを受ける
        /// </summary>
        /// <param name="damageInfo">ダメージ情報</param>
        void TakeDamage(DamageInfo damageInfo);
        
        /// <summary>
        /// 現在のHP
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// 最大HP
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// 生存しているか
        /// </summary>
        bool IsAlive { get; }
    }
}
