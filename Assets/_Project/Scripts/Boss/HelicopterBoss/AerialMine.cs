using UnityEngine;

public enum AerialMineState
{
    Travelling,
    Armed,
    Exploding
}

// Pooled mine that moves into position, warns the player and leaves a damaging blast.
public class AerialMine : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer mineRenderer;
    [SerializeField] private GameObject explosionVisual;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningFlashSpeed = 12f;

    [Header("Mine")]
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private float mineContactRadius = 0.45f;
    [SerializeField] private float armedDuration = 1.25f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 1.8f;
    [SerializeField] private float explosionDuration = 0.8f;

    private AerialMineState state;
    private Vector3 travelStart;
    private Vector3 travelDestination;
    private float travelDuration;
    private float stateTimer;
    private SpeedManager speedManager;

    public void Setup(SpeedManager worldSpeedManager)
    {
        speedManager = worldSpeedManager;
    }

    public void Deploy(Vector3 startPosition, Vector3 destination, float duration)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        travelStart = startPosition;
        travelDestination = destination;
        travelDuration = Mathf.Max(0.01f, duration);
        stateTimer = 0f;
        state = AerialMineState.Travelling;

        SetMineColor(normalColor);
        SetVisuals(true, false);
        gameObject.SetActive(true);
    }

    public void Despawn()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        SetMineColor(normalColor);
        SetVisuals(true, false);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stateTimer = stateTimer + deltaTime;

        if (state == AerialMineState.Travelling)
        {
            UpdateTravelling();
        }
        else if (state == AerialMineState.Armed)
        {
            UpdateArmed(deltaTime);
        }
        else if (state == AerialMineState.Exploding)
        {
            UpdateExplosion(deltaTime);
        }
    }

    private void UpdateTravelling()
    {
        float progress = Mathf.Clamp01(stateTimer / travelDuration);
        transform.position = Vector3.Lerp(travelStart, travelDestination, progress);

        if (progress < 1f)
        {
            return;
        }

        transform.position = travelDestination;
        state = AerialMineState.Armed;
        stateTimer = 0f;
    }

    private void UpdateArmed(float deltaTime)
    {
        MoveWithWorld(deltaTime);

        float blend = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);
        SetMineColor(Color.Lerp(normalColor, warningColor, blend));

        if (IsPlayerTouchingMine())
        {
            BeginExplosion();
            return;
        }

        if (stateTimer < armedDuration)
        {
            return;
        }

        BeginExplosion();
    }

    private bool IsPlayerTouchingMine()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            mineContactRadius,
            playerLayers);

        return hit != null;
    }

    private void BeginExplosion()
    {
        state = AerialMineState.Exploding;
        stateTimer = 0f;
        SetMineColor(normalColor);
        SetVisuals(false, true);
        DamagePlayerInsideExplosion();
    }

    private void UpdateExplosion(float deltaTime)
    {
        MoveWithWorld(deltaTime);
        DamagePlayerInsideExplosion();

        if (stateTimer < explosionDuration)
        {
            return;
        }

        Despawn();
    }

    private void MoveWithWorld(float deltaTime)
    {
        if (speedManager == null)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = position.x - speedManager.CurrentSpeed * deltaTime;
        transform.position = position;
    }

    private void DamagePlayerInsideExplosion()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            explosionRadius,
            playerLayers);

        if (hit == null)
        {
            return;
        }

        PlaneCollision plane = hit.GetComponentInParent<PlaneCollision>();

        if (plane != null)
        {
            plane.TakeHit();
        }
    }

    private void SetMineColor(Color color)
    {
        if (mineRenderer == null)
        {
            return;
        }

        mineRenderer.color = color;
    }

    private void SetVisuals(bool showMine, bool showExplosion)
    {
        if (mineRenderer != null)
        {
            mineRenderer.enabled = showMine;
        }

        if (explosionVisual != null && explosionVisual != gameObject)
        {
            explosionVisual.SetActive(showExplosion);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, mineContactRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
