using UnityEngine;

namespace RPG.Combat
{
    /// <summary>
    /// DamageSystemのテスト用スクリプト
    /// キー入力でダメージを発生させる
    /// </summary>
    public class DamageSystemTester : MonoBehaviour
    {
        [Header("テスト対象")]
        [SerializeField] private GameObject target;
        [SerializeField] private float testDamage = 25f;

        [Header("範囲ダメージ")]
        [SerializeField] private float areaRadius = 3f;
        [SerializeField] private float areaDamage = 15f;

        [Header("投射物テスト")]
        [SerializeField] private GameObject magicProjectilePrefab;
        [SerializeField] private float projectileSpeed = 15f;

        [Header("キー設定")]
        [SerializeField] private KeyCode singleDamageKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode areaDamageKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode criticalDamageKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode projectileKey = KeyCode.Alpha4;

        private void Start()
        {
            // ターゲットが設定されていない場合、Enemyタグを探す
            if (target == null)
            {
                target = GameObject.FindGameObjectWithTag("Enemy");
            }

            Debug.Log("[DamageSystemTester] 準備完了");
            Debug.Log($"  1キー: 単体ダメージ ({testDamage})");
            Debug.Log($"  2キー: 範囲ダメージ (半径{areaRadius}m, {areaDamage}ダメージ)");
            Debug.Log($"  3キー: 属性ダメージ (Fire)");
            Debug.Log($"  4キー: 投射物テスト (MagicProjectile)");
        }

        private void Update()
        {
            if (DamageSystem.Instance == null)
            {
                return;
            }

            // 1キー: 単体ダメージ
            if (Input.GetKeyDown(singleDamageKey))
            {
                TestSingleDamage();
            }

            // 2キー: 範囲ダメージ
            if (Input.GetKeyDown(areaDamageKey))
            {
                TestAreaDamage();
            }

            // 3キー: 属性ダメージ
            if (Input.GetKeyDown(criticalDamageKey))
            {
                TestElementalDamage();
            }

            // 4キー: 投射物テスト
            if (Input.GetKeyDown(projectileKey))
            {
                TestProjectile();
            }
        }

        private void TestSingleDamage()
        {
            if (target == null)
            {
                Debug.LogWarning("[DamageSystemTester] ターゲットがありません");
                return;
            }

            Debug.Log($"[DamageSystemTester] 単体ダメージ: {testDamage}");
            DamageSystem.Instance.ApplyDamage(target, testDamage);
        }

        private void TestAreaDamage()
        {
            Vector3 center = target != null ? target.transform.position : transform.position;

            Debug.Log($"[DamageSystemTester] 範囲ダメージ: 中心={center}, 半径={areaRadius}, ダメージ={areaDamage}");
            DamageSystem.Instance.ApplyAreaDamage(
                center,
                areaRadius,
                areaDamage,
                DamageType.Magical,
                MagicElement.Lightning
            );
        }

        private void TestElementalDamage()
        {
            if (target == null)
            {
                Debug.LogWarning("[DamageSystemTester] ターゲットがありません");
                return;
            }

            Debug.Log($"[DamageSystemTester] 炎属性ダメージ: {testDamage * 1.5f}");
            DamageSystem.Instance.ApplyDamage(
                target,
                testDamage * 1.5f,
                DamageType.Magical,
                MagicElement.Fire,
                target.transform.position + Vector3.up,
                gameObject
            );
        }

        private void TestProjectile()
        {
            if (magicProjectilePrefab == null)
            {
                Debug.LogWarning("[DamageSystemTester] 投射物プレハブが設定されていません");
                return;
            }

            Vector3 spawnPos = transform.position + Vector3.up * 1.2f;
            Vector3 direction = transform.forward;

            if (target != null)
            {
                direction = (target.transform.position - spawnPos).normalized;
            }

            var projectileObj = Instantiate(magicProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            var magicProjectile = projectileObj.GetComponent<MagicProjectile>();

            if (magicProjectile != null)
            {
                magicProjectile.Initialize(
                    direction,
                    gameObject,
                    testDamage,
                    DamageType.Magical,
                    MagicElement.Ice,
                    target?.transform
                );
                Debug.Log($"[DamageSystemTester] 氷属性投射物発射");
            }
            else
            {
                Debug.LogWarning("[DamageSystemTester] MagicProjectileコンポーネントがありません");
                Destroy(projectileObj);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 範囲ダメージの可視化
            Vector3 center = target != null ? target.transform.position : transform.position;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(center, areaRadius);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(center, areaRadius);
        }
    }
}
