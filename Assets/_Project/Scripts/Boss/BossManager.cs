using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedBossEntry
{
    public BossBase boss;

    [Min(0f)]
    public float weight = 1f;

    public Vector2 spawnPosition = new Vector2(30f, 20f);
}

[System.Serializable]
public class FloorBossPool
{
    public string floorName = "Floor";
    public WeightedBossEntry[] bosses;
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

    [Header("Weighted boss pools - 0 city, 1 sky, 2 space")]
    [SerializeField] private FloorBossPool[] bossPoolsByFloor;

    [Header("Legacy fallback while pools are being configured")]
    [SerializeField] private BossBase[] bossesByFloor;
    [SerializeField] private float fallbackSpawnX = 30f;
    [SerializeField] private float fallbackSpawnY = 20f;

    [Header("Timing")]
    [SerializeField] private float arenaLeadTime = 2.5f;

    private WeightedBossEntry[] selectedEntriesByFloor;
    private BossBase activeBoss;
    private bool isArenaActive;
    private bool isFightActive;
    private bool ascensionStarted;
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

                if (floorPool == null || floorPool.bosses == null)
                {
                    continue;
                }

                for (int entry = 0; entry < floorPool.bosses.Length; entry++)
                {
                    WeightedBossEntry bossEntry = floorPool.bosses[entry];

                    if (bossEntry == null)
                    {
                        continue;
                    }

                    PrepareBossOnce(bossEntry.boss, preparedBosses);
                }
            }
        }

        if (bossesByFloor == null)
        {
            return;
        }

        for (int i = 0; i < bossesByFloor.Length; i++)
        {
            PrepareBossOnce(bossesByFloor[i], preparedBosses);
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
        if (floorPool == null || floorPool.bosses == null)
        {
            return null;
        }

        float totalWeight = 0f;
        WeightedBossEntry lastWeightedEntry = null;

        for (int i = 0; i < floorPool.bosses.Length; i++)
        {
            WeightedBossEntry entry = floorPool.bosses[i];

            if (entry == null || entry.boss == null)
            {
                continue;
            }

            if (entry.weight <= 0f)
            {
                continue;
            }

            totalWeight = totalWeight + entry.weight;
            lastWeightedEntry = entry;
        }

        if (totalWeight <= 0f)
        {
            return FindFirstValidEntry(floorPool);
        }

        float roll = Random.value * totalWeight;
        float accumulatedWeight = 0f;

        for (int i = 0; i < floorPool.bosses.Length; i++)
        {
            WeightedBossEntry entry = floorPool.bosses[i];

            if (entry == null || entry.boss == null)
            {
                continue;
            }

            if (entry.weight <= 0f)
            {
                continue;
            }

            accumulatedWeight = accumulatedWeight + entry.weight;

            if (roll <= accumulatedWeight)
            {
                return entry;
            }
        }

        return lastWeightedEntry;
    }

    private WeightedBossEntry FindFirstValidEntry(FloorBossPool floorPool)
    {
        for (int i = 0; i < floorPool.bosses.Length; i++)
        {
            if (floorPool.bosses[i] == null)
            {
                continue;
            }

            if (floorPool.bosses[i].boss != null)
            {
                return floorPool.bosses[i];
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

    private BossBase GetBossForFloor(int floorIndex)
    {
        WeightedBossEntry selectedEntry = GetSelectedEntryForFloor(floorIndex);

        if (selectedEntry != null && selectedEntry.boss != null)
        {
            return selectedEntry.boss;
        }

        if (bossesByFloor == null)
        {
            return null;
        }

        if (floorIndex < 0 || floorIndex >= bossesByFloor.Length)
        {
            return null;
        }

        return bossesByFloor[floorIndex];
    }

    private Vector3 GetSpawnPositionForFloor(int floorIndex)
    {
        WeightedBossEntry selectedEntry = GetSelectedEntryForFloor(floorIndex);

        if (selectedEntry != null)
        {
            return new Vector3(
                selectedEntry.spawnPosition.x,
                selectedEntry.spawnPosition.y,
                0f);
        }

        return new Vector3(fallbackSpawnX, fallbackSpawnY, 0f);
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
