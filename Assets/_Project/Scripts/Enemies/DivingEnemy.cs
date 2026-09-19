using UnityEngine;

public enum DiveState
{
    Enter,
    Telegraph,
    Dive,
    Explode
}
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

    [Header("Player contact")]
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private float contactRadius = 0.5f;

    [Header("Explosion - leave off for a plain diver")]
    [SerializeField] private bool explodesOnArrival;
    [SerializeField] private GameObject explosionVisual;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDuration = 0.35f;

    private DiveState state;
    private float stateTimer;
    private Vector2 diveDirection;
    private Vector3 aimPoint;

    public override void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        bodyRenderer.color = normalColor;
        bodyRenderer.enabled = true;

        if (explosionVisual != null)
        {
            explosionVisual.SetActive(false);
        }
        ChangeState(DiveState.Enter);
        gameObject.SetActive(true);
    }

    public override void TakeBulletHit()
    {
        if (explodesOnArrival && state == DiveState.Dive)
        {
            BeginExplosion();
            return;
        }
        AudioSignals.Play(GameSound.EnemyDestroyed);
        DeactivateEnemy();
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
        float targetY = Mathf.Clamp(Target.position.y, minY, maxY);
        aimPoint = new Vector3(Target.position.x, targetY, 0f);

        Vector2 toTarget = new Vector2(
            aimPoint.x - transform.position.x,
            aimPoint.y - transform.position.y);

        diveDirection = toTarget.normalized;
        PointVisualAlong(diveDirection);
    }

    private void UpdateTelegraph()
    {
        bodyRenderer.color = FlashBetween(normalColor, telegraphColor, flashSpeed);

        if (stateTimer < telegraphDuration)
        {
            return;
        }

        bodyRenderer.color = normalColor;
        ChangeState(DiveState.Dive);
    }

    private void UpdateDive(float deltaTime)
    {
        Vector2 step = diveDirection * diveSpeed * deltaTime;

        if (CheckPlayerContact(step))
        {
            return;
        }

        MoveBy(step.x, step.y);

        if (explodesOnArrival && HasReachedAimPoint())
        {
            BeginExplosion();
            return;
        }
        if (transform.position.x > despawnX)
        {
            return;
        }
        DeactivateEnemy();
    }

    private bool CheckPlayerContact(Vector2 step)
    {
        if (step.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            contactRadius,
            step.normalized,
            step.magnitude,
            playerLayers);

        if (hit.collider == null)
        {
            return false;
        }

        if (explodesOnArrival)
        {
            Vector3 impactPosition = transform.position;
            impactPosition.x = hit.point.x;
            impactPosition.y = hit.point.y;
            transform.position = impactPosition;
            BeginExplosion();
            return true;
        }

        PlaneCollision plane = hit.collider.GetComponentInParent<PlaneCollision>();

        if (plane != null)
        {
            plane.TakeHit();
        }

        return false;
    }

    private bool HasReachedAimPoint()
    {
        return transform.position.x <= aimPoint.x;
    }

    private void BeginExplosion()
    {
        ChangeState(DiveState.Explode);
        AudioSignals.Play(GameSound.Explosion);
        bodyRenderer.enabled = false;

        if (explosionVisual != null)
        {
            explosionVisual.SetActive(true);
        }
        DealBlastDamage();
    }

    private void DealBlastDamage()
    {
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
        DeactivateEnemy();
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
        if (nextState == DiveState.Telegraph)
        {
            AudioSignals.Play(GameSound.EnemyWarning);
        }

        state = nextState;
        stateTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, contactRadius);

        if (explodesOnArrival)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }

}
