using UnityEngine;

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
    private int progressToAmmo;
    private float lastCloseCallTime = -10f;
    private bool lastAwardedAmmo;

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
        progressToAmmo = progressToAmmo + 1;
        AudioSignals.PlayCloseCall(progressToAmmo, closeCallsPerAmmo);
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
