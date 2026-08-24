using System.Collections.Generic;
using UnityEngine;

// Scrolls the world past the plane and recycles chunks with variety.
//
// The opening chunk is fixed and used once, so every run starts on a known piece of
// world instead of whatever the shuffle happened to pick. After it scrolls away it is
// never used again: it is not part of the pool.
//
// The plane never moves on X, so distance is counted here instead of read from a
// position. That counter never touches a transform, so it is free to grow as large
// as it likes without any precision trouble.
//
// Every chunk is created once at Start and only ever activated or deactivated after
// that. Allocating during play is what causes the stutter you cannot tune away later.
public class WorldScroller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private BossManager bossManager;

    [Header("Chunks")]
    [SerializeField] private Chunk openingChunkPrefab;
    [SerializeField] private Chunk[] chunkPrefabs;
    [SerializeField] private int copiesPerPrefab = 2;
    [SerializeField] private int activeChunkCount = 3;
    [SerializeField] private float chunkWidth = 40f;
    [SerializeField] private float startX = -30f;
    [SerializeField] private float recycleX = -45f;

    private Chunk openingChunk;
    private List<Chunk> pool = new List<Chunk>();
    private List<Chunk> activeChunks = new List<Chunk>();
    private List<Chunk> candidates = new List<Chunk>();
    private float distanceTravelled;

    public float DistanceTravelled
    {
        get { return distanceTravelled; }
    }

    public void SetStartDistance(float distance)
    {
        distanceTravelled = distance;
    }

    // Called when an ascension ends: retires every chunk still on screen and rebuilds the
    // world from the new floor. Without it the previous floor's chunks stay in play until
    // they happen to scroll off, so the first stretch of a new floor is a mix of the two.
    public void ClearActiveChunks()
    {
        for (int i = 0; i < activeChunks.Count; i++)
        {
            activeChunks[i].gameObject.SetActive(false);
        }
        activeChunks.Clear();
        RefillWorld();
    }

    private void Start()
    {
        BuildPool();
        FillWorld();
    }
    private void Update()
    {
        float delta = speedManager.CurrentSpeed * Time.deltaTime;
        distanceTravelled = distanceTravelled + delta;

        MoveChunks(delta);
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
            int copies = copiesPerPrefab;

            // Arenas need enough copies to fill the screen on their own, because during a
            // boss fight they are the only chunks allowed. With one copy each the world
            // runs out of them mid-fight.
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
            Debug.LogWarning("WorldScroller: nessun chunk con Is Arena. "
                + "Durante il boss il mondo restera' vuoto.", this);
        }
        if (pool.Count <= activeChunkCount)
        {
            Debug.LogWarning("WorldScroller: not enough chunks in the pool to fill the world.", this);
        }
    }
    private void FillWorld()
    {
        float x = startX;
        int startIndex = 0;

        if (openingChunk != null)
        {
            PlaceChunk(openingChunk, x);
            x = x + chunkWidth;
            startIndex = 1;
        }

        for (int i = startIndex; i < activeChunkCount; i++)
        {
            SpawnAt(x);
            x = x + chunkWidth;
        }
    }
    private void MoveChunks(float delta)
    {
        for (int i = 0; i < activeChunks.Count; i++)
        {
            Vector3 position = activeChunks[i].transform.position;
            position.x = position.x - delta;
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

        // If no replacement can be found, put the old one back rather than letting the
        // list shrink. Losing chunks one at a time empties activeChunks, and an empty
        // list means RecycleIfNeeded returns immediately: the world would stay blank
        // for the rest of the run with nothing to recover it.
        if (SpawnAt(nextX))
        {
            return;
        }

        oldest.transform.position = new Vector3(nextX, 0f, 0f);
        oldest.gameObject.SetActive(true);
        activeChunks.Add(oldest);
    }

    // Last resort, in case the list still ends up empty: rebuild from the player's left
    // so the run can carry on instead of scrolling through nothing.
    private void RefillWorld()
    {
        float x = startX;

        for (int i = 0; i < activeChunkCount; i++)
        {
            SpawnAt(x);
            x = x + chunkWidth;
        }
    }
    private bool SpawnAt(float x)
    {
        Chunk chunk = PickChunk();

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
    private Chunk PickChunk()
    {
        CollectCandidates(true);

        // The fallback deliberately does NOT apply during a boss fight. Reusing a normal
        // chunk there would fill the arena with the very obstacles it exists to clear.
        // An empty stretch is the correct outcome when no arena is free.
        if (candidates.Count == 0 && bossManager.IsArenaActive == false)
        {
            CollectCandidates(false);
        }
        if (candidates.Count == 0)
        {
            return null;
        }
        return candidates[Random.Range(0, candidates.Count)];
    }
    private void CollectCandidates(bool applyRules)
    {
        candidates.Clear();
        int floor = floorManager.CurrentFloorIndex;

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

            // Arena chunks skip the floor filter. The floor is promoted at the START of
            // the ascension while the arena stays up until it ends, so for those few
            // seconds the game would be looking for a chunk that is both an arena AND
            // on the new floor - and no such chunk exists. An arena is an arena: all it
            // has to do is leave the boss some room.
            if (chunk.IsArena)
            {
                candidates.Add(chunk);
                continue;
            }
            // The floor filter applies even in the fallback pass. Without it, running out
            // of candidates on a new floor would quietly bring the previous floor's
            // chunks back, which is worse than a gap: the player sees city buildings in
            // the sky and nothing explains why.
            if (chunk.BandIndex >= 0 && chunk.BandIndex != floor)
            {
                continue;
            }
            if (applyRules == false)
            {
                candidates.Add(chunk);
                continue;
            }
            if (chunk.MinDistance > distanceTravelled)
            {
                continue;
            }
            candidates.Add(chunk);
        }
    }
}