using UnityEngine;

// Marks a prefab as one slice of world and says when it is allowed to appear.
// Leave bandIndex at -1 for a chunk that fits any floor.
//
// Chunks are recycled, so everything inside has to be put back each time the chunk
// returns: pickups collected, obstacles destroyed, and moving obstacles that have
// drifted away from where they started. Forget any of these and the world quietly
// empties out over a long run, with nothing obvious to point at as the cause.
//
// The starting positions are captured here rather than in each MovingObstacle. Awake
// order between separate GameObjects is undefined, so an obstacle could still hold an
// uninitialised zero when this OnEnable fired. Awake and OnEnable on the SAME object
// always run in that order, so capturing here removes the dependency instead of hoping.
public class Chunk : MonoBehaviour
{
    [Header("Spawn rules")]
    [SerializeField] private float minDistance;
    [SerializeField] private int bandIndex = -1;
    [SerializeField] private bool isArena;

    private Pickup[] pickups;
    private Obstacle[] obstacles;
    private MovingObstacle[] movingObstacles;
    private VerticalObstacle[] verticalObstacles;

    public float MinDistance
    {
        get { return minDistance; }
    }
    public int BandIndex
    {
        get { return bandIndex; }
    }
    public bool IsArena
    {
        get { return isArena; }
    }

    private void Awake()
    {
        pickups = GetComponentsInChildren<Pickup>(true);
        obstacles = GetComponentsInChildren<Obstacle>(true);
        movingObstacles = GetComponentsInChildren<MovingObstacle>(true);
        verticalObstacles = GetComponentsInChildren<VerticalObstacle>(true);

        for (int i = 0; i < movingObstacles.Length; i++)
        {
            movingObstacles[i].CaptureStart();
        }
        for (int i = 0; i < verticalObstacles.Length; i++)
        {
            verticalObstacles[i].CaptureStart();
        }
    }
    private void OnEnable()
    {
        for (int i = 0; i < pickups.Length; i++)
        {
            pickups[i].ResetPickup();
        }
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].ResetObstacle();
        }
        for (int i = 0; i < movingObstacles.Length; i++)
        {
            movingObstacles[i].ResetPosition();
        }
        for (int i = 0; i < verticalObstacles.Length; i++)
        {
            verticalObstacles[i].ResetPosition();
        }
    }
}