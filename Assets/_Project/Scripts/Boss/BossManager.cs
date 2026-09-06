using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedBossEntry
{
    [SerializeField] private BossBase boss;

    [Min(0f)]
    [SerializeField] private float weight = 1f;

    [SerializeField] private Vector2 spawnPosition = new Vector2(30f, 20f);

    public BossBase Boss
    {
        get { return boss; }
    }

    public float Weight
    {
        get { return weight; }
    }

    public Vector2 SpawnPosition
    {
        get { return spawnPosition; }
    }
}

[System.Serializable]
public class FloorBossPool
{
    [SerializeField] private string floorName = "Floor";
    [SerializeField] private WeightedBossEntry[] bosses;

    public string FloorName
    {
        get { return floorName; }
    }

    public WeightedBossEntry[] Bosses
    {
        get { return bosses; }
    }
}

// Chooses one weighted boss for every floor at the beginning of the run.
// The choice is cached, so the same floor keeps the same boss for the whole run.
public class BossManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform player;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private AscensionSequence ascensionSequence;

    [Header("Cleanup after a boss defeat")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private BulletPool[] additionalBulletPools;
    [SerializeField] private ExplosiveMissilePool[] explosiveMissilePools;
    [SerializeField] private AerialMinePool[] aerialMinePools;

    [Header("Weighted boss pools - 0 city, 1 sky, 2 space")]
    [SerializeField] private FloorBossPool[] bossPoolsByFloor;

    [Header("Timing")]
    [SerializeField] private float arenaLeadTime = 2.5f;

    private WeightedBossEntry[] selectedEntriesByFloor;
    private BossBase activeBoss;
    private bool isArenaActive;
    private bool isFightActive;
    private bool ascensionStarted;
    private bool defeatCleanupCompleted;
    private float leadTimer;

    public bool IsArenaActive
    {
        get { return isArenaActive; }
    }

    public bool IsFightActive
    {
        get { return isFightActive; }
    }

    public BossBase CurrentBoss
    {
        get
        {
            if (activeBoss != null)
            {
                return activeBoss;
            }

            return GetBossForFloor(floorManager.CurrentFloorIndex);
        }
    }

    private void Awake()
    {
        PrepareAllBosses();
        SelectBossesForRun();
    }

    private void Update()
    {
        if (gameManager.IsGameOver)
        {
            return;
        }

        if (isFightActive)
        {
            UpdateFight();
            return;
        }

        if (isArenaActive)
        {
            UpdateLeadIn(Time.deltaTime);
            return;
        }

        CheckForBoss();
    }

    private void PrepareAllBosses()
    {
        List<BossBase> preparedBosses = new List<BossBase>();

        if (bossPoolsByFloor != null)
        {
            for (int floor = 0; floor < bossPoolsByFloor.Length; floor++)
            {
                FloorBossPool floorPool = bossPoolsByFloor[floor];

                if (floorPool == null || floorPool.Bosses == null)
                {
                    continue;
                }

                for (int entry = 0; entry < floorPool.Bosses.Length; entry++)
                {
                    WeightedBossEntry bossEntry = floorPool.Bosses[entry];

                    if (bossEntry == null)
                    {
                        continue;
                    }

                    PrepareBossOnce(bossEntry.Boss, preparedBosses);
                }
            }
        }
    }

    private void PrepareBossOnce(BossBase boss, List<BossBase> preparedBosses)
    {
        if (boss == null)
        {
            return;
        }

        if (preparedBosses.Contains(boss))
        {
            return;
        }

        boss.Setup(player, bulletPool);
        boss.gameObject.SetActive(false);
        preparedBosses.Add(boss);
    }

    private void SelectBossesForRun()
    {
        if (bossPoolsByFloor == null)
        {
            selectedEntriesByFloor = new WeightedBossEntry[0];
            return;
        }

        selectedEntriesByFloor = new WeightedBossEntry[bossPoolsByFloor.Length];

        for (int floor = 0; floor < bossPoolsByFloor.Length; floor++)
        {
            selectedEntriesByFloor[floor] = SelectWeightedEntry(bossPoolsByFloor[floor]);
        }
    }

    private WeightedBossEntry SelectWeightedEntry(FloorBossPool floorPool)
    {
        if (floorPool == null || floorPool.Bosses == null)
        {
            return null;
        }

        float totalWeight = 0f;
        WeightedBossEntry lastWeightedEntry = null;

        for (int i = 0; i < floorPool.Bosses.Length; i++)
        {
            WeightedBossEntry entry = floorPool.Bosses[i];

            if (entry == null || entry.Boss == null)
            {
                continue;
            }

            if (entry.Weight <= 0f)
            {
                continue;
            }

            totalWeight = totalWeight + entry.Weight;
            lastWeightedEntry = entry;
        }

        if (totalWeight <= 0f)
        {
            return FindFirstValidEntry(floorPool);
        }

        float roll = Random.value * totalWeight;
        float accumulatedWeight = 0f;

        for (int i = 0; i < floorPool.Bosses.Length; i++)
        {
            WeightedBossEntry entry = floorPool.Bosses[i];

            if (entry == null || entry.Boss == null)
            {
                continue;
            }

            if (entry.Weight <= 0f)
            {
                continue;
            }

            accumulatedWeight = accumulatedWeight + entry.Weight;

            if (roll <= accumulatedWeight)
            {
                return entry;
            }
        }

        return lastWeightedEntry;
    }

    private WeightedBossEntry FindFirstValidEntry(FloorBossPool floorPool)
    {
        for (int i = 0; i < floorPool.Bosses.Length; i++)
        {
            if (floorPool.Bosses[i] == null)
            {
                continue;
            }

            if (floorPool.Bosses[i].Boss != null)
            {
                return floorPool.Bosses[i];
            }
        }

        return null;
    }

    private void CheckForBoss()
    {
        if (floorManager.IsBossDue == false)
        {
            return;
        }

        isArenaActive = true;
        leadTimer = arenaLeadTime;
    }

    private void UpdateLeadIn(float deltaTime)
    {
        leadTimer = leadTimer - deltaTime;

        if (leadTimer > 0f)
        {
            return;
        }

        int floorIndex = floorManager.CurrentFloorIndex;
        activeBoss = GetBossForFloor(floorIndex);

        if (activeBoss == null)
        {
            Debug.LogError("BossManager: nessun boss valido per il piano "
                + floorIndex, this);
            isArenaActive = false;
            return;
        }

        activeBoss.Activate(GetSpawnPositionForFloor(floorIndex));
        defeatCleanupCompleted = false;
        isFightActive = true;
    }

    private void UpdateFight()
    {
        if (activeBoss == null)
        {
            isFightActive = false;
            isArenaActive = false;
            return;
        }

        if (activeBoss.IsDefeated && defeatCleanupCompleted == false)
        {
            defeatCleanupCompleted = true;
            ClearArenaHazards();
        }

        if (activeBoss.IsFinished == false)
        {
            return;
        }

        if (ascensionStarted == false)
        {
            ascensionStarted = true;
            ascensionSequence.Begin();
            return;
        }

        if (ascensionSequence.IsPlaying)
        {
            return;
        }

        isFightActive = false;
        isArenaActive = false;
        ascensionStarted = false;
        activeBoss = null;
    }

    private void ClearArenaHazards()
    {
        if (enemySpawner != null)
        {
            enemySpawner.DespawnAllEnemies();
        }

        if (bulletPool != null)
        {
            bulletPool.DespawnAll();
        }

        if (additionalBulletPools != null)
        {
            for (int i = 0; i < additionalBulletPools.Length; i++)
            {
                BulletPool additionalPool = additionalBulletPools[i];

                if (additionalPool == null || additionalPool == bulletPool)
                {
                    continue;
                }

                additionalPool.DespawnAll();
            }
        }

        if (explosiveMissilePools != null)
        {
            for (int i = 0; i < explosiveMissilePools.Length; i++)
            {
                if (explosiveMissilePools[i] != null)
                {
                    explosiveMissilePools[i].DespawnAll();
                }
            }
        }

        if (aerialMinePools != null)
        {
            for (int i = 0; i < aerialMinePools.Length; i++)
            {
                if (aerialMinePools[i] != null)
                {
                    aerialMinePools[i].DespawnAll();
                }
            }
        }
    }

    private BossBase GetBossForFloor(int floorIndex)
    {
        WeightedBossEntry selectedEntry = GetSelectedEntryForFloor(floorIndex);

        if (selectedEntry != null && selectedEntry.Boss != null)
        {
            return selectedEntry.Boss;
        }

        return null;
    }

    private Vector3 GetSpawnPositionForFloor(int floorIndex)
    {
        WeightedBossEntry selectedEntry = GetSelectedEntryForFloor(floorIndex);

        if (selectedEntry != null)
        {
            return new Vector3(
                selectedEntry.SpawnPosition.x,
                selectedEntry.SpawnPosition.y,
                0f);
        }

        return Vector3.zero;
    }

    private WeightedBossEntry GetSelectedEntryForFloor(int floorIndex)
    {
        if (selectedEntriesByFloor == null)
        {
            return null;
        }

        if (floorIndex < 0 || floorIndex >= selectedEntriesByFloor.Length)
        {
            return null;
        }

        return selectedEntriesByFloor[floorIndex];
    }
}
