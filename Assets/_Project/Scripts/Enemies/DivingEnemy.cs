using UnityEngine;

public enum DiveState
{
    Enter,
    Telegraph,
    Dive,
    Explode
}

// An enemy that enters, marks a target point, flashes, then charges straight at it.
// With explodesOnArrival off it is a diving bird that flies past; with it on it is a
// kamikaze that detonates where it was aiming.
//
// The aim is locked when the telegraph BEGINS, not when the dive starts. That is the
// whole fairness of the move: the flash is the window in which the player reads where it
// is going and gets out of the way. An enemy that tracked until launch would arrive
// wherever the player is, and no amount of skill would change that - only the dodge.
//
// It also never turns during the dive, so once it has committed the player can read its
// path perfectly. Same rule as the shooting enemy: commit, then no more information
// changes hands.
public class DivingEnemy : EnemyBase
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Transform visual;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color telegraphColor = Color.red;
    [SerializeField] private float flashSpeed = 10f;

    [Header("Timing")]
    [SerializeField] private float telegraphDuration = 1f;

    [Header("Movement")]
    [SerializeField] private float enterSpeed = 26f;
    [SerializeField] private float holdX = 13f;
    [SerializeField] private float diveSpeed = 34f;
    [SerializeField] private float despawnX = -30f;

    [Header("Band limits")]
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    [Header("Explosion - leave off for a plain diver")]
    [SerializeField] private bool explodesOnArrival;
    [SerializeField] private GameObject explosionVisual;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDuration = 0.35f;
    [SerializeField] private LayerMask playerLayers;

    private Transform target;
    private DiveState state;
    private float stateTimer;
    private Vector2 diveDirection;
    private Vector3 aimPoint;
    private bool hasDamaged;

    public override void Setup(Transform playerTarget, BulletPool pool)
    {
        target = playerTarget;
    }
    public override void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        bodyRenderer.color = normalColor;
        bodyRenderer.enabled = true;
        hasDamaged = false;

        if (explosionVisual != null)
        {
            explosionVisual.SetActive(false);
        }
        ChangeState(DiveState.Enter);
        gameObject.SetActive(true);
    }
    // A shot kamikaze still goes off. Killing one next to you gets you killed anyway,
    // which turns "shoot it" into "shoot it EARLY" - a decision instead of a reflex.
    public override void TakeBulletHit()
    {
        if (explodesOnArrival && state == DiveState.Dive)
        {
            BeginExplosion();
            return;
        }
        gameObject.SetActive(false);
    }
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stateTimer = stateTimer + deltaTime;

        if (state == DiveState.Enter)
        {
            UpdateEnter(deltaTime);
            return;
        }
        if (state == DiveState.Telegraph)
        {
            UpdateTelegraph();
            return;
        }
        if (state == DiveState.Explode)
        {
            UpdateExplosion();
            return;
        }
        UpdateDive(deltaTime);
    }
    private void UpdateEnter(float deltaTime)
    {
        MoveBy(-enterSpeed * deltaTime, 0f);

        if (transform.position.x > holdX)
        {
            return;
        }

        LockAim();
        ChangeState(DiveState.Telegraph);
    }
    private void LockAim()
    {
        float targetY = Mathf.Clamp(target.position.y, minY, maxY);
        aimPoint = new Vector3(target.position.x, targetY, 0f);

        Vector2 toTarget = new Vector2(
            aimPoint.x - transform.position.x,
            aimPoint.y - transform.position.y);

        diveDirection = toTarget.normalized;
        PointVisualAlong(diveDirection);
    }
    private void UpdateTelegraph()
    {
        float blend = Mathf.PingPong(Time.time * flashSpeed, 1f);
        bodyRenderer.color = Color.Lerp(normalColor, telegraphColor, blend);

        if (stateTimer < telegraphDuration)
        {
            return;
        }

        bodyRenderer.color = normalColor;
        ChangeState(DiveState.Dive);
    }
    private void UpdateDive(float deltaTime)
    {
        MoveBy(diveDirection.x * diveSpeed * deltaTime, diveDirection.y * diveSpeed * deltaTime);

        if (explodesOnArrival && HasReachedAimPoint())
        {
            BeginExplosion();
            return;
        }
        if (transform.position.x > despawnX)
        {
            return;
        }
        gameObject.SetActive(false);
    }
    private bool HasReachedAimPoint()
    {
        return transform.position.x <= aimPoint.x;
    }

    // The blast is a one-off overlap on the frame it goes off, not a lingering trigger.
    // A kamikaze that kept killing for the length of its animation would take out players
    // who had already cleared it, and there would be no way to tell that from a bug.
    private void BeginExplosion()
    {
        ChangeState(DiveState.Explode);
        hasDamaged = false;
        bodyRenderer.enabled = false;

        if (explosionVisual != null)
        {
            explosionVisual.SetActive(true);
        }
        DealBlastDamage();
    }
    private void DealBlastDamage()
    {
        if (hasDamaged)
        {
            return;
        }
        hasDamaged = true;

        Collider2D hit = Physics2D.OverlapCircle(
            transform.position, explosionRadius, playerLayers);

        if (hit == null)
        {
            return;
        }

        PlaneCollision plane = hit.GetComponentInParent<PlaneCollision>();

        if (plane == null)
        {
            return;
        }
        plane.TakeHit();
    }
    private void UpdateExplosion()
    {
        if (stateTimer < explosionDuration)
        {
            return;
        }

        if (explosionVisual != null)
        {
            explosionVisual.SetActive(false);
        }
        bodyRenderer.enabled = true;
        gameObject.SetActive(false);
    }
    private void PointVisualAlong(Vector2 direction)
    {
        if (visual == null)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        visual.localRotation = Quaternion.Euler(0f, 0f, angle + 180f);
    }
    private void ChangeState(DiveState nextState)
    {
        state = nextState;
        stateTimer = 0f;
    }
    private void OnDrawGizmosSelected()
    {
        if (explodesOnArrival == false)
        {
            return;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
    private void MoveBy(float deltaX, float deltaY)
    {
        Vector3 position = transform.position;
        position.x = position.x + deltaX;
        position.y = position.y + deltaY;
        transform.position = position;
    }
}