using UnityEngine;

// One hit and the run is over, unless the dodge is active or a shield absorbs it.
//
// The check is a SWEPT cast, not an overlap test. Obstacles slide left fast enough that
// on a slow phone they travel more than a unit between frames, so asking "am I touching
// something right now" would let the plane pass clean through them.
[RequireComponent(typeof(Rigidbody2D))]
public class PlaneCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneController planeController;
    [SerializeField] private PlaneDodge planeDodge;
    [SerializeField] private PlaneShield planeShield;
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private GameManager gameManager;

    [Header("Collision")]
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float hitRadius = 0.5f;

    private void Awake()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;
    }
    private void Update()
    {
        if (gameManager.IsGameOver)
        {
            return;
        }
        if (gameManager.IsAscending)
        {
            return;
        }
        if (planeDodge.IsInvulnerable)
        {
            return;
        }
        if (planeShield.IsInvulnerable)
        {
            return;
        }
        if (HasHitObstacle() == false)
        {
            return;
        }
        TakeHit();
    }
    public void TakeHit()
    {
        if (gameManager.IsGameOver)
        {
            return;
        }
        if (planeDodge.IsInvulnerable)
        {
            return;
        }
        if (planeShield.IsInvulnerable)
        {
            return;
        }
        if (planeShield.ConsumeShield())
        {
            return;
        }
        gameManager.GameOver();
    }
    private bool HasHitObstacle()
    {
        Vector2 relativeStep = new Vector2(
            speedManager.CurrentSpeed * Time.deltaTime,
            planeController.VerticalVelocity * Time.deltaTime);

        float distance = relativeStep.magnitude;

        if (distance < 0.0001f)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, hitRadius, relativeStep.normalized, distance, obstacleLayers);

        return hit.collider != null;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}