using UnityEngine;

public enum ArmedTransportWeapon
{
    MachineGun,
    Sniper,
    Bazooka
}

public enum ArmedTransportState
{
    Entering,
    Telegraphing,
    Firing,
    PostAttack,
    Leaving
}

// Used by the three city cars and the three sky balloons.
// Each prefab selects one weapon, performs one attack and then leaves the scene.
public class ArmedTransportEnemy : EnemyBase
{
    [Header("Enemy type")]
    [SerializeField] private ArmedTransportWeapon weapon = ArmedTransportWeapon.MachineGun;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer gunnerRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color machineGunWarningColor = Color.red;
    [SerializeField] private Color sniperWarningColor = Color.yellow;
    [SerializeField] private Color bazookaWarningColor = new Color(1f, 0.45f, 0f);
    [SerializeField] private float flashSpeed = 12f;

    [Header("Weapon")]
    [SerializeField] private Transform muzzle;

    [Header("Movement")]
    [SerializeField] private float enterSpeed = 18f;
    [SerializeField] private float holdX = 12f;
    [SerializeField] private float leaveSpeed = 16f;
    [SerializeField] private float despawnX = -22f;
    [SerializeField] private bool useFixedY;
    [SerializeField] private float fixedY = 10.5f;

    [Header("Random aerial entry")]
    [SerializeField] private float randomEntryMinY = 12f;
    [SerializeField] private float randomEntryMaxY = 28f;
    [SerializeField] private float minimumYDistanceFromPlayer = 4f;

    [Header("Timing")]
    [SerializeField] private float machineGunTelegraphDuration = 0.6f;
    [SerializeField] private float sniperTelegraphDuration = 0.7f;
    [SerializeField] private float bazookaTelegraphDuration = 0.85f;
    [SerializeField] private float postAttackDuration = 0.4f;

    [Header("Machine gun")]
    [SerializeField] private int machineGunBulletCount = 8;
    [SerializeField] private float machineGunShotInterval = 0.12f;
    [SerializeField] private float machineGunBulletSpeed = 24f;

    [Header("Sniper")]
    [SerializeField] private int sniperShotCount = 3;
    [SerializeField] private float sniperShotInterval = 0.25f;
    [SerializeField] private float sniperBulletSpeed = 45f;

    [Header("Bazooka")]
    [SerializeField] private int bazookaMissileCount = 2;
    [SerializeField] private float bazookaShotInterval = 0.45f;
    [SerializeField] private float bazookaMissileSpeed = 18f;

    private ArmedTransportState state;
    private float stateTimer;
    private float shotTimer;
    private int shotsFired;
    private Vector2 lockedDirection;
    private Vector3 lockedTargetPosition;

    public override void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        if (useFixedY)
        {
            SetY(fixedY);
        }
        else
        {
            SetY(PickRandomYAwayFromTarget(
                randomEntryMinY,
                randomEntryMaxY,
                minimumYDistanceFromPlayer));
        }

        shotsFired = 0;
        shotTimer = 0f;
        SetVisualColor(normalColor);
        ChangeState(ArmedTransportState.Entering);
        gameObject.SetActive(true);
    }

    public override void TakeBulletHit()
    {
        DeactivateEnemy();
    }

    private void OnDisable()
    {
        SetVisualColor(normalColor);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stateTimer = stateTimer + deltaTime;

        if (state == ArmedTransportState.Entering)
        {
            UpdateEntering(deltaTime);
        }
        else if (state == ArmedTransportState.Telegraphing)
        {
            UpdateTelegraphing();
        }
        else if (state == ArmedTransportState.Firing)
        {
            UpdateFiring(deltaTime);
        }
        else if (state == ArmedTransportState.PostAttack)
        {
            UpdatePostAttack();
        }
        else if (state == ArmedTransportState.Leaving)
        {
            UpdateLeaving(deltaTime);
        }
    }

    private void UpdateEntering(float deltaTime)
    {
        MoveBy(-enterSpeed * deltaTime, 0f);

        if (transform.position.x > holdX)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = holdX;
        transform.position = position;
        LockAim();
        ChangeState(ArmedTransportState.Telegraphing);
    }

    private void UpdateTelegraphing()
    {
        SetVisualColor(FlashBetween(
            normalColor,
            GetWarningColor(),
            flashSpeed));

        if (stateTimer < GetTelegraphDuration())
        {
            return;
        }

        SetVisualColor(normalColor);
        shotsFired = 0;
        shotTimer = 0f;
        FireNextShot();

        if (shotsFired >= GetShotCount())
        {
            ChangeState(ArmedTransportState.PostAttack);
            return;
        }

        ChangeState(ArmedTransportState.Firing);
    }

    private void UpdateFiring(float deltaTime)
    {
        shotTimer = shotTimer + deltaTime;
        float safeInterval = Mathf.Max(0.03f, GetShotInterval());

        if (shotTimer < safeInterval)
        {
            return;
        }

        shotTimer = shotTimer - safeInterval;
        FireNextShot();

        if (shotsFired >= GetShotCount())
        {
            ChangeState(ArmedTransportState.PostAttack);
        }
    }

    private void UpdatePostAttack()
    {
        if (stateTimer < postAttackDuration)
        {
            return;
        }

        ChangeState(ArmedTransportState.Leaving);
    }

    private void UpdateLeaving(float deltaTime)
    {
        MoveBy(-leaveSpeed * deltaTime, 0f);

        if (transform.position.x > despawnX)
        {
            return;
        }

        DeactivateEnemy();
    }

    private void FireNextShot()
    {
        if (shotsFired >= GetShotCount())
        {
            return;
        }

        if (weapon == ArmedTransportWeapon.Bazooka)
        {
            FireMissile();
        }
        else
        {
            FireBullet();
        }

        shotsFired = shotsFired + 1;
    }

    private void FireBullet()
    {
        if (NormalBulletPool == null)
        {
            return;
        }

        Bullet bullet = NormalBulletPool.GetBullet();

        if (bullet == null)
        {
            return;
        }

        float speed = machineGunBulletSpeed;

        if (weapon == ArmedTransportWeapon.Sniper)
        {
            speed = sniperBulletSpeed;
        }

        bullet.Launch(GetPointPosition(muzzle), lockedDirection, speed);
    }

    private void FireMissile()
    {
        if (MissilePool == null)
        {
            Debug.LogWarning(
                "ArmedTransportEnemy: Explosive Missile Pool non assegnata nel roster.",
                this);
            return;
        }

        ExplosiveMissile missile = MissilePool.GetMissile();

        if (missile == null)
        {
            return;
        }

        Vector3 muzzlePosition = GetPointPosition(muzzle);
        lockedTargetPosition = GetTargetPosition(muzzlePosition, 10f);

        missile.Launch(
            muzzlePosition,
            lockedTargetPosition,
            bazookaMissileSpeed);
    }

    private void LockAim()
    {
        Vector3 muzzlePosition = GetPointPosition(muzzle);
        lockedTargetPosition = GetTargetPosition(muzzlePosition, 10f);

        lockedDirection = lockedTargetPosition - muzzlePosition;

        if (lockedDirection.sqrMagnitude < 0.0001f)
        {
            lockedDirection = Vector2.left;
        }

        lockedDirection.Normalize();
    }

    private int GetShotCount()
    {
        if (weapon == ArmedTransportWeapon.MachineGun)
        {
            return Mathf.Max(1, machineGunBulletCount);
        }

        if (weapon == ArmedTransportWeapon.Sniper)
        {
            return Mathf.Max(1, sniperShotCount);
        }

        return Mathf.Max(1, bazookaMissileCount);
    }

    private float GetShotInterval()
    {
        if (weapon == ArmedTransportWeapon.MachineGun)
        {
            return machineGunShotInterval;
        }

        if (weapon == ArmedTransportWeapon.Sniper)
        {
            return sniperShotInterval;
        }

        return bazookaShotInterval;
    }

    private float GetTelegraphDuration()
    {
        if (weapon == ArmedTransportWeapon.MachineGun)
        {
            return machineGunTelegraphDuration;
        }

        if (weapon == ArmedTransportWeapon.Sniper)
        {
            return sniperTelegraphDuration;
        }

        return bazookaTelegraphDuration;
    }

    private Color GetWarningColor()
    {
        if (weapon == ArmedTransportWeapon.MachineGun)
        {
            return machineGunWarningColor;
        }

        if (weapon == ArmedTransportWeapon.Sniper)
        {
            return sniperWarningColor;
        }

        return bazookaWarningColor;
    }

    private void SetVisualColor(Color color)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.color = color;
        }

        if (gunnerRenderer != null)
        {
            gunnerRenderer.color = color;
        }
    }

    private void ChangeState(ArmedTransportState nextState)
    {
        state = nextState;
        stateTimer = 0f;
    }

}
