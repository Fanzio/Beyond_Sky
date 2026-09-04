using UnityEngine;

public enum EnemyState
{
    Enter,
    Track,
    Telegraph,
    Leave
}

// An enemy that flies in, then hunts the player's altitude for a few seconds, firing
// whenever it manages to line up.
//
// Lining up is the TRIGGER, not a timer: keep moving and it never gets a shot away,
// stand still and it punishes you. That turns a passive obstacle into something the
// player is actively evading for as long as it is on screen.
//
// It still stops tracking to flash before every shot. Firing the instant it aligned
// would mean firing exactly where the player is, with no window to react - the flash is
// what makes each shot survivable, and it is the same rule the boss uses.
//
// It holds its X while hunting, so all the pressure is vertical and the player always
// knows where the danger is coming from.
public class EnemyPlane : EnemyBase
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color telegraphColor = Color.red;
    [SerializeField] private float flashSpeed = 14f;

    [Header("Stay")]
    [SerializeField] private float stayDuration = 4f;

    [Header("Aim")]
    [SerializeField] private float alignTolerance = 0.6f;
    [SerializeField] private float telegraphDuration = 0.45f;
    [SerializeField] private float shotCooldown = 1.1f;

    [Header("Movement")]
    [SerializeField] private float enterSpeed = 28f;
    [SerializeField] private float holdX = 11f;
    [SerializeField] private float trackSpeed = 7f;
    [SerializeField] private float leaveSpeed = 36f;
    [SerializeField] private float leaveDuration = 1.4f;

    [Header("Band limits")]
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    [Header("Firing")]
    [SerializeField] private float bulletSpeed = 22f;

    private Transform target;
    private BulletPool bulletPool;
    private EnemyState state;
    private float stateTimer;
    private float stayTimer;
    private float cooldownTimer;

    public override void Setup(Transform playerTarget, BulletPool pool)
    {
        target = playerTarget;
        bulletPool = pool;
    }
    public override void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        bodyRenderer.color = normalColor;
        stayTimer = 0f;
        cooldownTimer = 0f;
        ChangeState(EnemyState.Enter);
        gameObject.SetActive(true);
    }
    public override void TakeBulletHit()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stateTimer = stateTimer + deltaTime;

        if (state == EnemyState.Enter)
        {
            UpdateEnter(deltaTime);
            return;
        }
        if (state == EnemyState.Track)
        {
            UpdateTrack(deltaTime);
            return;
        }
        if (state == EnemyState.Telegraph)
        {
            UpdateTelegraph();
            return;
        }
        UpdateLeave(deltaTime);
    }
    private void UpdateEnter(float deltaTime)
    {
        MoveBy(-enterSpeed * deltaTime, 0f);

        if (transform.position.x > holdX)
        {
            return;
        }
        ChangeState(EnemyState.Track);
    }
    private void UpdateTrack(float deltaTime)
    {
        stayTimer = stayTimer + deltaTime;
        TickCooldown(deltaTime);

        float targetY = Mathf.Clamp(target.position.y, minY, maxY);
        SetY(Mathf.MoveTowards(transform.position.y, targetY, trackSpeed * deltaTime));

        if (stayTimer >= stayDuration)
        {
            ChangeState(EnemyState.Leave);
            return;
        }
        if (cooldownTimer > 0f)
        {
            return;
        }
        if (IsAlignedWithTarget() == false)
        {
            return;
        }
        ChangeState(EnemyState.Telegraph);
    }
    private void UpdateTelegraph()
    {
        stayTimer = stayTimer + Time.deltaTime;

        float blend = Mathf.PingPong(Time.time * flashSpeed, 1f);
        bodyRenderer.color = Color.Lerp(normalColor, telegraphColor, blend);

        if (stateTimer < telegraphDuration)
        {
            return;
        }

        bodyRenderer.color = normalColor;
        FireBullet();
        cooldownTimer = shotCooldown;

        if (stayTimer >= stayDuration)
        {
            ChangeState(EnemyState.Leave);
            return;
        }
        ChangeState(EnemyState.Track);
    }
    private void UpdateLeave(float deltaTime)
    {
        MoveBy(leaveSpeed * deltaTime, leaveSpeed * 0.4f * deltaTime);

        if (stateTimer < leaveDuration)
        {
            return;
        }
        gameObject.SetActive(false);
    }
    private bool IsAlignedWithTarget()
    {
        float gap = Mathf.Abs(transform.position.y - target.position.y);
        return gap <= alignTolerance;
    }
    private void TickCooldown(float deltaTime)
    {
        if (cooldownTimer <= 0f)
        {
            return;
        }
        cooldownTimer = cooldownTimer - deltaTime;
    }
    private void FireBullet()
    {
        if (bulletPool == null)
        {
            return;
        }

        Bullet bullet = bulletPool.GetBullet();

        if (bullet == null)
        {
            return;
        }
        bullet.Launch(transform.position, Vector2.left, bulletSpeed);
    }
    private void ChangeState(EnemyState nextState)
    {
        state = nextState;
        stateTimer = 0f;
    }
    private void MoveBy(float deltaX, float deltaY)
    {
        Vector3 position = transform.position;
        position.x = position.x + deltaX;
        position.y = position.y + deltaY;
        transform.position = position;
    }
    private void SetY(float y)
    {
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;
    }
}