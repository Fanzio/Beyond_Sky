using UnityEngine;

public class Chunk : MonoBehaviour
{
    [Header("Spawn rules")]
    [SerializeField] private int bandIndex = -1;
    [SerializeField] private bool isArena;

    private Pickup[] pickups;
    private Obstacle[] obstacles;
    private MovingObstacle[] movingObstacles;
    private VerticalObstacle[] verticalObstacles;

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
