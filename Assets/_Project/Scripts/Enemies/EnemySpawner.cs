using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyEntry
{
    public EnemyBase prefab;

    [Header("Floors this enemy appears on - 0 city, 1 sky, 2 space")]
    public int[] floors;

    public int poolSize = 3;
    public bool enabled = true;

    [Header("Leave empty to use the spawner's default pool")]
    public BulletPool bulletPool;

    [Header("Optional support pools for special enemies")]
    public ExplosiveMissilePool explosiveMissilePool;
    public AerialMinePool aerialMinePool;
}

// Normal random spawning plus an explicit pooled spawn requested by a boss.
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
    private List<int> poolEntryIndex = new List<int>();
    private List<EnemyBase> candidates = new List<EnemyBase>();
    private float nextSpawnTimer;

    private void Awake()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].enabled == false)
            {
                continue;
            }

            if (entries[i].prefab == null)
            {
                continue;
            }

            for (int copy = 0; copy < entries[i].poolSize; copy++)
            {
                EnemyBase enemy = Instantiate(entries[i].prefab, transform);
                enemy.Setup(player, PoolFor(entries[i]));

                IEnemySupportPoolReceiver supportReceiver =
                    enemy as IEnemySupportPoolReceiver;

                if (supportReceiver != null)
                {
                    supportReceiver.SetupSupportPools(
                        entries[i].explosiveMissilePool,
                        entries[i].aerialMinePool);
                }

                enemy.gameObject.SetActive(false);
                pool.Add(enemy);
                poolEntryIndex.Add(i);
            }
        }

        nextSpawnTimer = Random.Range(minInterval, maxInterval);
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

        nextSpawnTimer = nextSpawnTimer - Time.deltaTime;

        if (nextSpawnTimer > 0f)
        {
            return;
        }

        nextSpawnTimer = Random.Range(minInterval, maxInterval);
        TrySpawn();
    }

    // Used by bosses to activate a specific enemy from this same pre-built pool. It
    // deliberately ignores the arena and on-screen limits, because during a summon the
    // boss owns the timing and the quantity.
    //
    // The prefab passed in must be the SAME asset listed in entries, or nothing matches
    // and the summon quietly does nothing. The warning exists because that failure is
    // otherwise invisible: the boss simply never summons and there is nothing to see.
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

            int entryIndex = poolEntryIndex[i];

            if (entries[entryIndex].prefab != requestedPrefab)
            {
                continue;
            }

            pool[i].Activate(startPosition);
            return pool[i];
        }

        return null;
    }

    private bool IsInRoster(EnemyBase prefab)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].prefab == prefab)
            {
                return true;
            }
        }

        return false;
    }

    private void TrySpawn()
    {
        if (CountActive() >= MaxActiveNow())
        {
            return;
        }

        EnemyBase enemy = PickEnemy();

        if (enemy == null)
        {
            return;
        }

        enemy.Activate(new Vector3(spawnX, PickSpawnY(), 0f));
    }

    private EnemyBase PickEnemy()
    {
        candidates.Clear();
        int floor = floorManager.CurrentFloorIndex;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].gameObject.activeSelf)
            {
                continue;
            }

            if (AppearsOnFloor(entries[poolEntryIndex[i]], floor) == false)
            {
                continue;
            }

            candidates.Add(pool[i]);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private BulletPool PoolFor(EnemyEntry entry)
    {
        if (entry.bulletPool != null)
        {
            return entry.bulletPool;
        }

        return defaultBulletPool;
    }

    private int MaxActiveNow()
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

    private bool AppearsOnFloor(EnemyEntry entry, int floor)
    {
        for (int i = 0; i < entry.floors.Length; i++)
        {
            if (entry.floors[i] == floor)
            {
                return true;
            }
        }

        return false;
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
