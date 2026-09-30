// Seleziona e attiva i boss, prepara l’arena e gestisce la fine dello scontro.
using System.Collections.Generic;
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.Enemies;
using BeyondSky.Systems;
using BeyondSky.UI;

namespace BeyondSky.Boss
{
    [System.Serializable]
    public class BossEntry
    {
        [SerializeField] private BossBase boss;

        [SerializeField] private Vector2 spawnPosition = new Vector2(30f, 20f);

        public BossBase Boss
        {
            get { return boss; }
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
        [SerializeField] private BossEntry[] bosses;

        public BossEntry[] Bosses
        {
            get { return bosses; }
        }
    }

    public class BossManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FloorManager floorManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Transform player;
        [SerializeField] private BulletPool bulletPool;
        [SerializeField] private AscensionSequence ascensionSequence;
        [SerializeField] private UIFlow uiFlow;

        [Header("Cleanup after a boss defeat")]
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private BulletPool[] additionalBulletPools;
        [SerializeField] private ExplosiveMissilePool[] explosiveMissilePools;
        [SerializeField] private AerialMinePool[] aerialMinePools;

        [Header("Boss pools - 0 city, 1 sky, 2 space")]
        [SerializeField] private FloorBossPool[] bossPoolsByFloor;

        [Header("Timing")]
        [SerializeField] private float arenaLeadTime = 2.5f;

        private BossEntry[] selectedEntriesByFloor;
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
            if (gameManager.IsGameOver || Time.timeScale == 0f)
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
                        BossEntry bossEntry = floorPool.Bosses[entry];

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
                selectedEntriesByFloor = new BossEntry[0];
                return;
            }

            selectedEntriesByFloor = new BossEntry[bossPoolsByFloor.Length];

            for (int floor = 0; floor < bossPoolsByFloor.Length; floor++)
            {
                selectedEntriesByFloor[floor] = SelectRandomEntry(bossPoolsByFloor[floor], null);
            }
        }

        private BossEntry SelectRandomEntry(FloorBossPool floorPool, BossBase excludedBoss)
        {
            if (floorPool == null || floorPool.Bosses == null)
            {
                return null;
            }

            List<BossEntry> validEntries = new List<BossEntry>();

            for (int i = 0; i < floorPool.Bosses.Length; i++)
            {
                BossEntry entry = floorPool.Bosses[i];

                if (entry != null && entry.Boss != null && entry.Boss != excludedBoss)
                {
                    validEntries.Add(entry);
                }
            }

            if (validEntries.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, validEntries.Count);
            return validEntries[index];
        }

        private void SelectNextSpaceBoss()
        {
            int floorIndex = floorManager.CurrentFloorIndex;
            BossEntry nextEntry = SelectRandomEntry(bossPoolsByFloor[floorIndex], activeBoss);

            if (nextEntry != null)
            {
                selectedEntriesByFloor[floorIndex] = nextEntry;
            }
        }

        private void CheckForBoss()
        {
            if (floorManager.IsBossDue == false)
            {
                return;
            }

            isArenaActive = true;
            AudioSignals.Play(GameSound.BossIncoming);
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
                bool wasTopFloor = floorManager.IsTopFloor;
                ascensionStarted = true;
                ascensionSequence.Begin();

                if (wasTopFloor)
                {
                    SelectNextSpaceBoss();

                    if (uiFlow != null)
                    {
                        uiFlow.ShowSpaceReached();
                    }
                }
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
            BossEntry selectedEntry = GetSelectedEntryForFloor(floorIndex);

            if (selectedEntry != null && selectedEntry.Boss != null)
            {
                return selectedEntry.Boss;
            }

            return null;
        }

        private Vector3 GetSpawnPositionForFloor(int floorIndex)
        {
            BossEntry selectedEntry = GetSelectedEntryForFloor(floorIndex);

            if (selectedEntry != null)
            {
                return new Vector3(
                    selectedEntry.SpawnPosition.x,
                    selectedEntry.SpawnPosition.y,
                    0f);
            }

            return Vector3.zero;
        }

        private BossEntry GetSelectedEntryForFloor(int floorIndex)
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
}
