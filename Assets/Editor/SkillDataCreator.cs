using UnityEngine;
using UnityEditor;
using RPG.Combat;

public class SkillDataCreator
{
    [MenuItem("Tools/Create Test Skills")]
    public static void CreateTestSkills()
    {
        // IceBolt
        CreateSkill(
            "IceBolt",
            "Ice Bolt",
            "氷の弾を発射する",
            SkillType.Skill,
            25f,
            DamageType.Magical,
            MagicElement.Ice,
            2f
        );

        // Fireball
        CreateSkill(
            "Fireball",
            "Fireball",
            "炎の弾を発射する",
            SkillType.Skill,
            30f,
            DamageType.Magical,
            MagicElement.Fire,
            3f
        );

        // Lightning
        CreateSkill(
            "Lightning",
            "Lightning",
            "雷撃を放つ",
            SkillType.Heavy,
            40f,
            DamageType.Magical,
            MagicElement.Lightning,
            4f
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SkillDataCreator] Created 3 test skills in Assets/Data/Skills/");
    }

    private static void CreateSkill(
        string fileName,
        string skillName,
        string description,
        SkillType skillType,
        float baseDamage,
        DamageType damageType,
        MagicElement element,
        float cooldown)
    {
        string path = $"Assets/Data/Skills/{fileName}.asset";

        // 既存チェック
        var existing = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (existing != null)
        {
            Debug.Log($"[SkillDataCreator] {fileName} already exists, updating...");
            UpdateSkill(existing, skillName, description, skillType, baseDamage, damageType, element, cooldown);
            EditorUtility.SetDirty(existing);
            return;
        }

        // 新規作成
        var skill = ScriptableObject.CreateInstance<SkillData>();
        UpdateSkill(skill, skillName, description, skillType, baseDamage, damageType, element, cooldown);

        // フォルダ確認
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Data/Skills"))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Skills");
        }

        AssetDatabase.CreateAsset(skill, path);
        Debug.Log($"[SkillDataCreator] Created {fileName}");
    }

    private static void UpdateSkill(
        SkillData skill,
        string skillName,
        string description,
        SkillType skillType,
        float baseDamage,
        DamageType damageType,
        MagicElement element,
        float cooldown)
    {
        skill.skillName = skillName;
        skill.description = description;
        skill.skillType = skillType;
        skill.baseDamage = baseDamage;
        skill.damageMultiplier = 1f;
        skill.damageType = damageType;
        skill.element = element;
        skill.criticalRate = 0.1f;
        skill.criticalMultiplier = 1.5f;
        skill.knockbackForce = 3f;
        skill.useProjectile = true;
        skill.projectileSpeed = 15f;
        skill.projectileLifetime = 3f;
        skill.projectileHoming = false;
        skill.cooldown = cooldown;

        // ProjectilePrefabを設定
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/MagicProjectile.prefab");
        if (prefab != null)
        {
            skill.projectilePrefab = prefab;
        }
    }
}
