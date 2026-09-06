using UnityEngine;

// Uses a swept cast because fast obstacles can cross the plane between two frames.
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
        if (ShouldIgnoreHit())
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
        if (ShouldIgnoreHit())
        {
            return;
        }

        if (planeShield.ConsumeShield())
        {
            return;
        }

        gameManager.GameOver();
    }

    private bool ShouldIgnoreHit()
    {
        if (gameManager.IsGameOver)
        {
            return true;
        }

        if (gameManager.IsAscending)
        {
            return true;
        }

        if (planeDodge.IsInvulnerable)
        {
            return true;
        }

        if (planeShield.IsInvulnerable)
        {
            return true;
        }

        return false;
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
