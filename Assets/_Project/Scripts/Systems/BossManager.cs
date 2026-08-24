using UnityEngine;

// Runs the boss encounter: empties the world, sends the boss in, and promotes the floor
// when it goes down.
//
// The arena switches on a couple of seconds BEFORE the boss arrives. That lead time does
// two jobs: it tells the player something is about to happen while they can still react,
// and it gives the normal chunks already on screen time to scroll away. Without it the
// fight would start in the middle of a crane.
//
// It also stays up until the ascension has finished, so an empty world and a silent
// spawner during the climb come for free from checks that already exist.
public class BossManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BossPlane bossPlane;
    [SerializeField] private Transform player;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private AscensionSequence ascensionSequence;

    [Header("Timing")]
    [SerializeField] private float arenaLeadTime = 2.5f;

    [Header("Spawn position")]
    [SerializeField] private float spawnX = 30f;
    [SerializeField] private float spawnY = 20f;

    [SerializeField] private bool isArenaActive;
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
    public BossPlane Boss
    {
        get { return bossPlane; }
    }

    private void Awake()
    {
        bossPlane.Setup(player, bulletPool);
        bossPlane.gameObject.SetActive(false);
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

        bossPlane.Activate(new Vector3(spawnX, spawnY, 0f));
        isFightActive = true;
    }
    private void UpdateFight()
    {
        if (bossPlane.IsFinished == false)
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
    }
}