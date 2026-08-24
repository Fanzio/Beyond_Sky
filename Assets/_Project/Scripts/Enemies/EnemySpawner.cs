using System.Collections.Generic;
using UnityEngine;

// Sends enemies in at random intervals, never more than a couple at once.
//
// Two rules matter more than the timing. An enemy never appears at the player's exact
// altitude, because a shot you were already lined up for is not a decision. And they
// only start after a set distance, so the opening of a run stays readable.
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyPlane enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BossManager bossManager;

    [Header("Pool")]
    [SerializeField] private int poolSize = 4;
    [SerializeField] private int maxActive = 2;

    [Header("Timing")]
    [SerializeField] private float firstSpawnDistance = 500f;
    [SerializeField] private float minInterval = 6f;
    [SerializeField] private float maxInterval = 11f;

    [Header("Spawn position")]
    [SerializeField] private float spawnX = 26f;
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;
    [SerializeField] private float minOffsetFromPlayer = 5f;

    private List<EnemyPlane> pool = new List<EnemyPlane>();
    private float nextSpawnTimer;

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            EnemyPlane enemy = Instantiate(enemyPrefab, transform);
            enemy.Setup(player, bulletPool);
            enemy.gameObject.SetActive(false);
            pool.Add(enemy);
        }
        nextSpawnTimer = Random.Range(minInterval, maxInterval);
    }
    private void Update()
    {
        if (gameManager.IsGameOver)
        {
            return;
        }
        if (bossManager.IsArenaActive)
        {
            return;
        }
        if (worldScroller.DistanceTravelled < firstSpawnDistance)
        {
            return;
        }

        nextSpawnTimer = nextSpawnTimer - Time.deltaTime;

        if (nextSpawnTimer > 0f)
        {
            return;
        }

        nextSpawnTimer = Random.Range(minInterval, maxInterval);
        TrySpawn();
    }
    private void TrySpawn()
    {
        if (CountActive() >= maxActive)
        {
            return;
        }

        EnemyPlane enemy = GetIdleEnemy();

        if (enemy == null)
        {
            return;
        }
        enemy.Activate(new Vector3(spawnX, PickSpawnY(), 0f));
    }
    private float PickSpawnY()
    {
        float playerY = player.position.y;
        float above = playerY + minOffsetFromPlayer;
        float below = playerY - minOffsetFromPlayer;

        if (above > maxY)
        {
            return Random.Range(minY, below);
        }
        if (below < minY)
        {
            return Random.Range(above, maxY);
        }
        if (Random.value < 0.5f)
        {
            return Random.Range(minY, below);
        }
        return Random.Range(above, maxY);
    }
    private EnemyPlane GetIdleEnemy()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].gameObject.activeSelf)
            {
                continue;
            }
            return pool[i];
        }
        return null;
    }
    private int CountActive()
    {
        int count = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].gameObject.activeSelf)
            {
                count = count + 1;
            }
        }
        return count;
    }
}