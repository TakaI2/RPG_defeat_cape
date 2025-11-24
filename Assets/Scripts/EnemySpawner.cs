using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float spawnRate = 1f;
    [SerializeField] private int poolSize = 100;

    private ObjectPool<EnemyController> _enemyPool;
    private float _nextSpawnTime;

    private void Start()
    {
        _enemyPool = new ObjectPool<EnemyController>(enemyPrefab, poolSize, transform);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (Time.time >= _nextSpawnTime)
        {
            SpawnEnemy();
            _nextSpawnTime = Time.time + spawnRate;
        }
    }

    private void SpawnEnemy()
    {
        if (player == null) return;

        // Random position on circle edge
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPos = new Vector3(randomCircle.x, 0, randomCircle.y) + player.position;

        EnemyController enemy = _enemyPool.Get();
        enemy.transform.position = spawnPos;
        enemy.Initialize(player, OnEnemyDeath);
    }

    private void OnEnemyDeath(EnemyController enemy)
    {
        _enemyPool.Return(enemy);
    }
}
