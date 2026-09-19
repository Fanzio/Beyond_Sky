using System.Collections.Generic;
using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private BossManager bossManager;

    [Header("Chunks")]
    [SerializeField] private Chunk openingChunkPrefab;
    [SerializeField] private Chunk[] chunkPrefabs;
    [SerializeField] private int activeChunkCount = 3;
    [SerializeField] private float chunkWidth = 40f;
    [SerializeField] private float startX = -30f;
    [SerializeField] private float recycleX = -45f;

    [Header("Clear opening after takeoff")]
    [Min(0f)]
    [SerializeField] private float openingClearDuration = 2f;
    [Min(1f)]
    [SerializeField] private float openingScreenMargin = 2f;

    private float openingTimer;
    private bool openingClear = true;

    public bool IsOpeningClear
    {
        get { return openingClear; }
    }

    private Chunk openingChunk;
    private List<Chunk> pool = new List<Chunk>();
    private List<Chunk> activeChunks = new List<Chunk>();
    private List<Chunk> candidates = new List<Chunk>();
    private float distanceTravelled;
    private bool worldPrepared;

    public float DistanceTravelled
    {
        get { return distanceTravelled; }
    }

    public void ClearActiveChunks()
    {
        openingClear = false;
        for (int i = 0; i < activeChunks.Count; i++)
        {
            activeChunks[i].gameObject.SetActive(false);
        }
        activeChunks.Clear();
        RefillWorld();
    }

    private void Start()
    {
        PrepareWorld();
    }

    public void PrepareWorld()
    {
        if (worldPrepared)
        {
            return;
        }

        BuildPool();
        openingTimer = Mathf.Max(0f, openingClearDuration);
        worldPrepared = true;
    }

    private void Update()
    {
        float distanceThisFrame = speedManager.CurrentSpeed * Time.deltaTime;
        distanceTravelled = distanceTravelled + distanceThisFrame;

        if (openingClear)
        {
            openingTimer = openingTimer - Time.deltaTime;
            if (openingTimer <= 0f)
            {
                FillOpeningOffscreen();
                openingClear = false;
            }
            return;
        }

        MoveChunks(distanceThisFrame);
        RecycleIfNeeded();
    }

    private void BuildPool()
    {
        int arenaCount = 0;

        if (openingChunkPrefab != null)
        {
            openingChunk = Instantiate(openingChunkPrefab, transform);
            openingChunk.gameObject.SetActive(false);
        }

        for (int i = 0; i < chunkPrefabs.Length; i++)
        {
            int copies = 1;

            if (chunkPrefabs[i].IsArena)
            {
                copies = activeChunkCount;
                arenaCount = arenaCount + 1;
            }

            for (int copy = 0; copy < copies; copy++)
            {
                Chunk chunk = Instantiate(chunkPrefabs[i], transform);
                chunk.gameObject.SetActive(false);
                pool.Add(chunk);
            }
        }
        if (arenaCount == 0)
        {
            Debug.LogWarning("WorldScroller: nessun chunk con Is Arena. " + "Durante il boss il mondo restera' vuoto.", this);
        }
        if (pool.Count <= activeChunkCount)
        {
            Debug.LogWarning("WorldScroller: not enough chunks in the pool to fill the world.", this);
        }
    }

    private void FillWorld()
    {
        float spawnX = startX;
        int startIndex = 0;

        if (openingChunk != null)
        {
            PlaceChunk(openingChunk, spawnX);
            spawnX = spawnX + chunkWidth;
            startIndex = 1;
        }

        for (int i = startIndex; i < activeChunkCount; i++)
        {
            TrySpawnAt(spawnX);
            spawnX = spawnX + chunkWidth;
        }
    }

    private void FillOpeningOffscreen()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("WorldScroller: Main Camera mancante per l'ingresso dei chunk.", this);
            return;
        }

        FillWorld();
        float rightEdge = camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, Mathf.Abs(camera.transform.position.z))).x;
        float leftEdge = float.MaxValue;
        for (int i = 0; i < activeChunks.Count; i++)
        {
            Chunk chunk = activeChunks[i];
            leftEdge = Mathf.Min(leftEdge, chunk.transform.position.x - chunkWidth * 0.5f);
            SpriteRenderer[] sprites = chunk.GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < sprites.Length; j++)
            {
                leftEdge = Mathf.Min(leftEdge, sprites[j].bounds.min.x);
            }
            Collider2D[] colliders = chunk.GetComponentsInChildren<Collider2D>();
            for (int j = 0; j < colliders.Length; j++)
            {
                if (colliders[j].enabled)
                {
                    leftEdge = Mathf.Min(leftEdge, colliders[j].bounds.min.x);
                }
            }
        }

        float shift = Mathf.Max(0f, rightEdge + openingScreenMargin - leftEdge);
        for (int i = 0; i < activeChunks.Count; i++)
        {
            activeChunks[i].transform.position += Vector3.right * shift;
        }
    }

    private void MoveChunks(float distance)
    {
        for (int i = 0; i < activeChunks.Count; i++)
        {
            Vector3 position = activeChunks[i].transform.position;
            position.x = position.x - distance;
            activeChunks[i].transform.position = position;
        }
    }

    private void RecycleIfNeeded()
    {
        if (activeChunks.Count == 0)
        {
            RefillWorld();
            return;
        }

        Chunk oldest = activeChunks[0];

        if (oldest.transform.position.x >= recycleX)
        {
            return;
        }

        float nextX = activeChunks[activeChunks.Count - 1].transform.position.x + chunkWidth;

        oldest.gameObject.SetActive(false);
        activeChunks.RemoveAt(0);

        if (TrySpawnAt(nextX))
        {
            return;
        }

        oldest.transform.position = new Vector3(nextX, 0f, 0f);
        oldest.gameObject.SetActive(true);
        activeChunks.Add(oldest);
    }

    private void RefillWorld()
    {
        float spawnX = startX;

        for (int i = 0; i < activeChunkCount; i++)
        {
            TrySpawnAt(spawnX);
            spawnX = spawnX + chunkWidth;
        }
    }

    private bool TrySpawnAt(float x)
    {
        Chunk chunk = PickAvailableChunk();

        if (chunk == null)
        {
            return false;
        }
        PlaceChunk(chunk, x);
        return true;
    }

    private void PlaceChunk(Chunk chunk, float x)
    {
        chunk.transform.position = new Vector3(x, 0f, 0f);
        chunk.gameObject.SetActive(true);
        activeChunks.Add(chunk);
    }

    private Chunk PickAvailableChunk()
    {
        CollectAvailableChunks();
        if (candidates.Count == 0)
        {
            return null;
        }
        return candidates[Random.Range(0, candidates.Count)];
    }

    private void CollectAvailableChunks()
    {
        candidates.Clear();
        int floorIndex = floorManager.CurrentFloorIndex;

        for (int i = 0; i < pool.Count; i++)
        {
            Chunk chunk = pool[i];

            if (chunk.gameObject.activeSelf)
            {
                continue;
            }
            if (chunk.IsArena != bossManager.IsArenaActive)
            {
                continue;
            }

            if (chunk.IsArena)
            {
                candidates.Add(chunk);
                continue;
            }
            if (chunk.BandIndex >= 0 && chunk.BandIndex != floorIndex)
            {
                continue;
            }
            candidates.Add(chunk);
        }
    }
}
