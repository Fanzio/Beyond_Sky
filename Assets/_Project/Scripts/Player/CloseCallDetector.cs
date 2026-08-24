using UnityEngine;

// Rewards flying close to obstacles without touching them.
//
// The payout is AMMO, not speed. Speed is the difficulty ramp in this genre, so paying a
// risky move with speed would reward skill by making the game harder, and players stop
// doing that once they notice. Ammo is the one scarce resource, so skilled flying is what
// arms you for the boss: fly well and arrive loaded, play safe and arrive empty handed.
//
// The plane is fixed on X and obstacles scroll past, so the closest approach always
// happens on the frame an obstacle crosses the plane's X. Catch that one frame and award.
// No state has to be tracked over time.
//
// The reward is all or nothing. Anything the overlap finds is already inside the radius,
// so no distance check is needed: you either threaded the gap or you did not.
//
// Close calls are ignored while rolling, or the best play would be to double tap straight
// into every obstacle and collect the bonus every time.
public class CloseCallDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private PlaneDodge planeDodge;
    [SerializeField] private PlaneWeapon planeWeapon;

    [Header("Detection")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float closeCallRadius = 1.8f;

    [Header("Reward")]
    [SerializeField] private int closeCallsPerAmmo = 3;

    private Collider2D[] hits = new Collider2D[8];
    private int closeCallCount;
    private int progressToAmmo;
    private float lastCloseCallTime = -10f;
    private bool lastAwardedAmmo;

    public int CloseCallCount
    {
        get { return closeCallCount; }
    }
    public int ProgressToAmmo
    {
        get { return progressToAmmo; }
    }
    public int CloseCallsPerAmmo
    {
        get { return closeCallsPerAmmo; }
    }
    public bool LastAwardedAmmo
    {
        get { return lastAwardedAmmo; }
    }
    public float TimeSinceLastCloseCall
    {
        get { return Time.time - lastCloseCallTime; }
    }

    private void Update()
    {
        if (planeDodge.IsRolling)
        {
            return;
        }

        float step = speedManager.CurrentSpeed * Time.deltaTime;
        float planeX = transform.position.x;

        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, closeCallRadius, hits, obstacleLayers);

        for (int i = 0; i < count; i++)
        {
            if (HasJustCrossed(hits[i], planeX, step) == false)
            {
                continue;
            }
            Award();
        }
    }
    private bool HasJustCrossed(Collider2D obstacle, float planeX, float step)
    {
        float x = obstacle.bounds.center.x;

        if (x > planeX)
        {
            return false;
        }
        if (x + step <= planeX)
        {
            return false;
        }
        return true;
    }
    private void Award()
    {
        lastCloseCallTime = Time.time;
        closeCallCount = closeCallCount + 1;
        progressToAmmo = progressToAmmo + 1;
        lastAwardedAmmo = false;

        if (progressToAmmo < closeCallsPerAmmo)
        {
            return;
        }

        progressToAmmo = 0;
        lastAwardedAmmo = true;
        planeWeapon.AddAmmo(1);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, closeCallRadius);
    }
}