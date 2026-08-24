using UnityEngine;

public enum BossState
{
    Idle,
    Enter,
    Track,
    Telegraph,
    Recover,
    Defeated
}

// The boss is a bigger enemy plane with one number standing in for its health: a timer.
//
// The timer drains on its own AND drops by a chunk on every bullet, so surviving it and
// shooting it down are the same mechanic seen from two sides. One value, one bar, and
// the choice between the two strategies stays entirely with the player.
//
// Telegraph and tracking shorten as the timer falls, so the fight builds instead of
// repeating. The telegraph never goes below its floor value: that limit is what keeps
// every shot dodgeable, and it applies to the boss exactly as it does to a normal enemy.
public class BossPlane : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color telegraphColor = Color.red;
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private float flashSpeed = 10f;
    [SerializeField] private float hitFlashTime = 0.12f;

    [Header("Fight length")]
    [SerializeField] private float totalTime = 24f;
    [SerializeField] private float timeLostPerBullet = 3f;

    [Header("Attack cycle - start of fight")]
    [SerializeField] private float trackDurationStart = 1.6f;
    [SerializeField] private float telegraphDurationStart = 1.1f;

    [Header("Attack cycle - end of fight")]
    [SerializeField] private float trackDurationEnd = 0.9f;
    [SerializeField] private float telegraphDurationEnd = 0.75f;

    [Header("Recover")]
    [SerializeField] private float recoverDuration = 0.7f;

    [Header("Movement")]
    [SerializeField] private float enterSpeed = 22f;
    [SerializeField] private float holdX = 12f;
    [SerializeField] private float trackSpeed = 6f;
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    [Header("Firing")]
    [SerializeField] private float bulletSpeed = 22f;

    [Header("Defeat")]
    [SerializeField] private float defeatDuration = 1.2f;
    [SerializeField] private float defeatSpinSpeed = 420f;
    [SerializeField] private float defeatFallSpeed = 9f;

    private Transform target;
    private BulletPool bulletPool;
    private BossState state = BossState.Idle;
    private float stateTimer;
    private float timeRemaining;
    private float hitFlashTimer;
    private bool isFinished;

    public bool IsFinished
    {
        get { return isFinished; }
    }
    public bool IsFighting
    {
        get { return state == BossState.Track || state == BossState.Telegraph || state == BossState.Recover; }
    }
    public float TimeRemaining
    {
        get { return timeRemaining; }
    }
    public float TotalTime
    {
        get { return totalTime; }
    }
    public float NormalizedTime
    {
        get { return Mathf.Clamp01(timeRemaining / totalTime); }
    }

    public void Setup(Transform playerTarget, BulletPool pool)
    {
        target = playerTarget;
        bulletPool = pool;
    }
    public void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        timeRemaining = totalTime;
        hitFlashTimer = 0f;
        isFinished = false;
        bodyRenderer.color = normalColor;
        ChangeState(BossState.Enter);
        gameObject.SetActive(true);
    }
    public void TakeBulletHit()
    {
        if (IsFighting == false)
        {
            return;
        }

        timeRemaining = timeRemaining - timeLostPerBullet;
        hitFlashTimer = hitFlashTime;

        if (timeRemaining > 0f)
        {
            return;
        }
        ChangeState(BossState.Defeated);
    }
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stateTimer = stateTimer + deltaTime;

        TickHitFlash(deltaTime);

        if (state == BossState.Enter)
        {
            UpdateEnter(deltaTime);
            return;
        }
        if (state == BossState.Defeated)
        {
            UpdateDefeated(deltaTime);
            return;
        }
        if (IsFighting == false)
        {
            return;
        }

        DrainTimer(deltaTime);

        if (state == BossState.Track)
        {
            UpdateTrack(deltaTime);
            return;
        }
        if (state == BossState.Telegraph)
        {
            UpdateTelegraph();
            return;
        }
        UpdateRecover();
    }
    private void DrainTimer(float deltaTime)
    {
        timeRemaining = timeRemaining - deltaTime;

        if (timeRemaining > 0f)
        {
            return;
        }
        ChangeState(BossState.Defeated);
    }
    private void UpdateEnter(float deltaTime)
    {
        MoveBy(-enterSpeed * deltaTime, 0f);

        if (transform.position.x > holdX)
        {
            return;
        }
        ChangeState(BossState.Track);
    }
    private void UpdateTrack(float deltaTime)
    {
        float targetY = Mathf.Clamp(target.position.y, minY, maxY);
        SetY(Mathf.MoveTowards(transform.position.y, targetY, trackSpeed * deltaTime));
        ApplyColor();

        if (stateTimer < CurrentTrackDuration())
        {
            return;
        }
        ChangeState(BossState.Telegraph);
    }
    private void UpdateTelegraph()
    {
        ApplyColor();

        if (stateTimer < CurrentTelegraphDuration())
        {
            return;
        }
        FireBullet();
        ChangeState(BossState.Recover);
    }
    private void UpdateRecover()
    {
        ApplyColor();

        if (stateTimer < recoverDuration)
        {
            return;
        }
        ChangeState(BossState.Track);
    }
    private void UpdateDefeated(float deltaTime)
    {
        transform.Rotate(0f, 0f, defeatSpinSpeed * deltaTime);
        MoveBy(-defeatFallSpeed * 0.4f * deltaTime, -defeatFallSpeed * deltaTime);

        if (stateTimer < defeatDuration)
        {
            return;
        }

        isFinished = true;
        gameObject.SetActive(false);
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
    private float Progress()
    {
        return 1f - NormalizedTime;
    }
    private float CurrentTrackDuration()
    {
        return Mathf.Lerp(trackDurationStart, trackDurationEnd, Progress());
    }
    private float CurrentTelegraphDuration()
    {
        return Mathf.Lerp(telegraphDurationStart, telegraphDurationEnd, Progress());
    }
    private void TickHitFlash(float deltaTime)
    {
        if (hitFlashTimer <= 0f)
        {
            return;
        }
        hitFlashTimer = hitFlashTimer - deltaTime;
    }
    private void ApplyColor()
    {
        if (hitFlashTimer > 0f)
        {
            bodyRenderer.color = hitColor;
            return;
        }
        if (state == BossState.Telegraph)
        {
            float blend = Mathf.PingPong(Time.time * flashSpeed, 1f);
            bodyRenderer.color = Color.Lerp(normalColor, telegraphColor, blend);
            return;
        }
        bodyRenderer.color = normalColor;
    }
    private void ChangeState(BossState nextState)
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
