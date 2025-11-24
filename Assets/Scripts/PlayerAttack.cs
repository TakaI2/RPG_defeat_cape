using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private LayerMask enemyLayer;

    private float _nextFireTime;

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager Instance is null!");
            return;
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            // Debug.Log($"Game State is not Playing: {GameManager.Instance.CurrentState}"); // Commented out to avoid spam
            return;
        }

        if (Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + fireRate;
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is not assigned in PlayerAttack!");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("Fire Point is not assigned in PlayerAttack!");
            return;
        }

        // Find nearest enemy
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        Transform nearestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = hitCollider.transform;
            }
        }

        Quaternion fireRotation = firePoint.rotation;
        if (nearestEnemy != null)
        {
            Vector3 direction = (nearestEnemy.position - firePoint.position).normalized;
            direction.y = 0; // Keep projectile level
            fireRotation = Quaternion.LookRotation(direction);
        }

        Instantiate(projectilePrefab, firePoint.position, fireRotation);
        // Debug.Log("Fired projectile!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
