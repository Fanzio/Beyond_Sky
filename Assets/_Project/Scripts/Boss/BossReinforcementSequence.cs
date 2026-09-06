using UnityEngine;

// Runs the identical one-at-a-time reinforcement sequence shared by several bosses.
public class BossReinforcementSequence
{
    private EnemySpawner enemySpawner;
    private EnemyBase singlePrefab;
    private EnemyBase[] prefabChoices;
    private int enemyCount;
    private float spawnInterval;
    private float spawnX;
    private float minY;
    private float maxY;

    private int enemiesSpawned;
    private float spawnTimer;
    private EnemyBase activeEnemy;
    private bool isActive;
    private bool useRandomPrefabs;

    public bool IsActive
    {
        get { return isActive; }
    }

    public void BeginSingle(
        EnemySpawner spawner,
        EnemyBase prefab,
        int count,
        float interval,
        float positionX,
        float minimumY,
        float maximumY)
    {
        enemySpawner = spawner;
        singlePrefab = prefab;
        prefabChoices = null;
        useRandomPrefabs = false;

        Begin(count, interval, positionX, minimumY, maximumY);

        if (singlePrefab == null)
        {
            Stop();
        }
    }

    public void BeginRandom(
        EnemySpawner spawner,
        EnemyBase[] prefabs,
        int count,
        float interval,
        float positionX,
        float minimumY,
        float maximumY)
    {
        enemySpawner = spawner;
        singlePrefab = null;
        prefabChoices = prefabs;
        useRandomPrefabs = true;

        Begin(count, interval, positionX, minimumY, maximumY);

        if (prefabChoices == null || prefabChoices.Length == 0)
        {
            Stop();
        }
    }

    public void Tick(float deltaTime)
    {
        if (isActive == false)
        {
            return;
        }

        if (activeEnemy != null)
        {
            if (activeEnemy.gameObject.activeSelf)
            {
                return;
            }

            activeEnemy = null;
            spawnTimer = 0f;
        }

        if (enemiesSpawned >= enemyCount)
        {
            Stop();
            return;
        }

        spawnTimer = spawnTimer + deltaTime;

        if (spawnTimer < spawnInterval)
        {
            return;
        }

        spawnTimer = 0f;
        TrySpawnNextEnemy();
    }

    public void Stop()
    {
        isActive = false;
        activeEnemy = null;
    }

    private void Begin(
        int count,
        float interval,
        float positionX,
        float minimumY,
        float maximumY)
    {
        enemyCount = Mathf.Max(0, count);
        spawnInterval = Mathf.Max(0f, interval);
        spawnX = positionX;
        minY = minimumY;
        maxY = maximumY;
        enemiesSpawned = 0;
        spawnTimer = 0f;
        activeEnemy = null;
        isActive = enemySpawner != null && enemyCount > 0;

        if (isActive)
        {
            TrySpawnNextEnemy();
        }
    }

    private void TrySpawnNextEnemy()
    {
        EnemyBase selectedPrefab = GetNextPrefab();

        if (selectedPrefab == null)
        {
            Stop();
            return;
        }

        float progress = (enemiesSpawned + 0.5f) / enemyCount;
        float spawnY = Mathf.Lerp(minY, maxY, progress);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

        EnemyBase spawnedEnemy = enemySpawner.SpawnForBoss(
            selectedPrefab,
            spawnPosition);

        if (spawnedEnemy == null)
        {
            return;
        }

        activeEnemy = spawnedEnemy;
        enemiesSpawned = enemiesSpawned + 1;
    }

    private EnemyBase GetNextPrefab()
    {
        if (useRandomPrefabs == false)
        {
            return singlePrefab;
        }

        if (prefabChoices == null || prefabChoices.Length == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, prefabChoices.Length);
        return prefabChoices[randomIndex];
    }
}
