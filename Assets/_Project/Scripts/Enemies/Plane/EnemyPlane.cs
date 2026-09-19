using UnityEngine;

public enum EnemyState
{
    Enter,
    Track,
    Telegraph,
    Leave,
    SustainedFire
}
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
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform secondMuzzle;
    [SerializeField] private float bulletSpeed = 22f;

    private EnemyState state;
    private float stateTimer;
    private float stayTimer;
    private float cooldownTimer;

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
        AudioSignals.Play(GameSound.EnemyDestroyed);
        DeactivateEnemy();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

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
        if (state == EnemyState.SustainedFire)
        {
            UpdateSustainedFire(deltaTime);
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
        TrackPointToTargetY(muzzle, trackSpeed, minY, maxY, deltaTime);

        if (stayTimer >= stayDuration)
        {
            ChangeState(EnemyState.Leave);
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

        TrackPointToTargetY(muzzle, trackSpeed, minY, maxY, Time.deltaTime);

        bodyRenderer.color = FlashBetween(normalColor, telegraphColor, flashSpeed);

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
        ChangeState(EnemyState.SustainedFire);
    }

    private void UpdateSustainedFire(float deltaTime)
    {
        stayTimer = stayTimer + deltaTime;
        if (stayTimer >= stayDuration)
        {
            ChangeState(EnemyState.Leave);
            return;
        }

        TrackPointToTargetY(muzzle, trackSpeed, minY, maxY, deltaTime);
        TickCooldown(deltaTime);
        if (cooldownTimer > 0f)
        {
            return;
        }

        FireBullet();
        cooldownTimer = Mathf.Max(0.03f, shotCooldown);
    }

    private void UpdateLeave(float deltaTime)
    {
        MoveBy(leaveSpeed * deltaTime, leaveSpeed * 0.4f * deltaTime);

        if (stateTimer < leaveDuration)
        {
            return;
        }
        DeactivateEnemy();
    }

    private bool IsAlignedWithTarget()
    {
        float targetY = Mathf.Clamp(Target.position.y, minY, maxY);
        float gap = Mathf.Abs(GetPointPosition(muzzle).y - targetY);
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
        bool fired = FireFromMuzzle(muzzle);

        if (secondMuzzle != null && secondMuzzle != muzzle)
        {
            if (FireFromMuzzle(secondMuzzle))
            {
                fired = true;
            }
        }

        if (fired)
        {
            AudioSignals.Play(GameSound.EnemyShot);
        }
    }

    private bool FireFromMuzzle(Transform firePoint)
    {
        if (NormalBulletPool == null)
        {
            return false;
        }

        Bullet bullet = NormalBulletPool.GetBullet();

        if (bullet == null)
        {
            return false;
        }
        bullet.Launch(GetPointPosition(firePoint), Vector2.left, bulletSpeed);
        return true;
    }

    private void ChangeState(EnemyState nextState)
    {
        if (nextState == EnemyState.Telegraph)
        {
            AudioSignals.Play(GameSound.EnemyWarning);
        }

        state = nextState;
        stateTimer = 0f;
    }

}
