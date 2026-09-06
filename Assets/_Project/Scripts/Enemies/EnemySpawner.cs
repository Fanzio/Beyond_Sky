using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyEntry
{
    [SerializeField] private EnemyBase prefab;

    [Header("Floors this enemy appears on - 0 city, 1 sky, 2 space")]
    [SerializeField] private int[] floors;

    [SerializeField] private int poolSize = 3;
    [SerializeField] private bool enabled = true;

    [Header("Leave empty to use the spawner's default pool")]
    [SerializeField] private BulletPool bulletPool;

    [Header("Optional support pools for special enemies")]
    [SerializeField] private ExplosiveMissilePool explosiveMissilePool;
    [SerializeField] private AerialMinePool aerialMinePool;

    public EnemyBase Prefab
    {
        get { return prefab; }
    }

    public int[] Floors
    {
        get { return floors; }
    }

    public int PoolSize
    {
        get { return poolSize; }
    }

    public bool IsEnabled
    {
        get { return enabled; }
    }

    public BulletPool BulletPool
    {
        get { return bulletPool; }
    }

    public ExplosiveMissilePool ExplosiveMissilePool
    {
        get { return explosiveMissilePool; }
    }

    public AerialMinePool AerialMinePool
    {
        get { return aerialMinePool; }
    }
}

// Creates every enemy pool before gameplay, handles normal random spawning and lets
// bosses request an exact prefab while bypassing the normal spawn limits.
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private BulletPool defaultBulletPool;
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BossManager bossManager;

    [Header("Roster")]
    [SerializeField] private EnemyEntry[] entries;

    [Header("Limits - one entry per floor, 0 city, 1 sky, 2 space")]
    [SerializeField] private int[] maxActivePerFloor = new int[] { 2, 3, 3 };

    [Header("Timing")]
    [SerializeField] private float firstSpawnDistance = 500f;
    [SerializeField] private float minInterval = 6f;
    [SerializeField] private float maxInterval = 11f;

    [Header("Spawn position")]
    [SerializeField] private float spawnX = 26f;
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;
    [SerializeField] private float minOffsetFromPlayer = 5f;

    private List<EnemyBase> pool = new List<EnemyBase>();
    private List<int> poolEntryIndices = new List<int>();
    private List<EnemyBase> availableEnemies = new List<EnemyBase>();
    private float spawnTimer;

    private void Awake()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            EnemyEntry entry = entries[i];

            if (entry.IsEnabled == false)
            {
                continue;
            }

            if (entry.Prefab == null)
            {
                continue;
            }

            for (int copy = 0; copy < entry.PoolSize; copy++)
            {
                EnemyBase enemy = Instantiate(entry.Prefab, transform);
                enemy.Setup(player, GetBulletPool(entry));
                enemy.SetupExplosiveMissilePool(entry.ExplosiveMissilePool);
                enemy.SetupAerialMinePool(entry.AerialMinePool);

                enemy.gameObject.SetActive(false);
                pool.Add(enemy);
                poolEntryIndices.Add(i);
            }
        }

        spawnTimer = Random.Range(minInterval, maxInterval);
    }

    private void Update()
    {
        if (gameManager.IsGameOver)
        {
            return;
        }

        if (gameManager.IsAscending)
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

        spawnTimer = spawnTimer - Time.deltaTime;

        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer = Random.Range(minInterval, maxInterval);
        TrySpawnRandomEnemy();
    }

    public EnemyBase SpawnForBoss(EnemyBase requestedPrefab, Vector3 startPosition)
    {
        if (requestedPrefab == null)
        {
            return null;
        }

        if (IsInRoster(requestedPrefab) == false)
        {
            Debug.LogWarning("EnemySpawner: il prefab " + requestedPrefab.name
                + " non e' nel roster, il summon non puo' funzionare.", this);
            return null;
        }

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].gameObject.activeSelf)
            {
                continue;
            }

            int entryIndex = poolEntryIndices[i];

            if (entries[entryIndex].Prefab != requestedPrefab)
            {
                continue;
            }

            pool[i].Activate(startPosition);
            return pool[i];
        }

        return null;
    }

    public void DespawnAllEnemies()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            pool[i].gameObject.SetActive(false);
        }
    }

    private bool IsInRoster(EnemyBase prefab)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Prefab == prefab)
            {
                return true;
            }
        }

        return false;
    }

    private void TrySpawnRandomEnemy()
    {
        if (CountActiveEnemies() >= GetMaximumActiveEnemies())
        {
            return;
        }

        EnemyBase enemy = PickAvailableEnemy();

        if (enemy == null)
        {
            return;
        }

        enemy.Activate(new Vector3(spawnX, PickSpawnHeight(), 0f));
    }

    private EnemyBase PickAvailableEnemy()
    {
        availableEnemies.Clear();
        int floor = floorManager.CurrentFloorIndex;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].gameObject.activeSelf)
            {
                continue;
            }

            if (CanAppearOnFloor(entries[poolEntryIndices[i]], floor) == false)
            {
                continue;
            }

            availableEnemies.Add(pool[i]);
        }

        if (availableEnemies.Count == 0)
        {
            return null;
        }

        return availableEnemies[Random.Range(0, availableEnemies.Count)];
    }

    private BulletPool GetBulletPool(EnemyEntry entry)
    {
        if (entry.BulletPool != null)
        {
            return entry.BulletPool;
        }

        return defaultBulletPool;
    }

    private int GetMaximumActiveEnemies()
    {
        if (maxActivePerFloor.Length == 0)
        {
            return 1;
        }

        int floor = floorManager.CurrentFloorIndex;

        if (floor >= maxActivePerFloor.Length)
        {
            return maxActivePerFloor[maxActivePerFloor.Length - 1];
        }

        return maxActivePerFloor[floor];
    }

    private bool CanAppearOnFloor(EnemyEntry entry, int floor)
    {
        for (int i = 0; i < entry.Floors.Length; i++)
        {
            if (entry.Floors[i] == floor)
            {
                return true;
            }
        }

        return false;
    }

    private float PickSpawnHeight()
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

    private int CountActiveEnemies()
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
