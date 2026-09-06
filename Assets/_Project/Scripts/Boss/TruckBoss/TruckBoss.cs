using UnityEngine;

public enum TruckBossAttack
{
    MachineGun,
    Sniper,
    Bazooka,
    SummonPlanes
}

public enum TruckBossState
{
    Waiting,
    MachineGunTelegraph,
    MachineGunBurst,
    SniperTelegraph,
    SniperGap,
    BazookaTelegraph,
    BazookaGap,
    SummonTelegraph,
    Recover
}

// A ground boss with three independent gunners and pooled aerial reinforcements.
public class TruckBoss : BossBase
{
    [Header("Gunner visuals")]
    [SerializeField] private SpriteRenderer machineGunnerRenderer;
    [SerializeField] private SpriteRenderer sniperRenderer;
    [SerializeField] private SpriteRenderer bazookaRenderer;
    [SerializeField] private Color normalGunnerColor = Color.white;
    [SerializeField] private Color machineGunWarningColor = Color.red;
    [SerializeField] private Color sniperWarningColor = Color.yellow;
    [SerializeField] private Color bazookaWarningColor = new Color(1f, 0.45f, 0f);
    [SerializeField] private Color summonWarningColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] private float flashSpeed = 12f;

    [Header("Muzzles")]
    [SerializeField] private Transform machineGunMuzzle;
    [SerializeField] private Transform sniperMuzzle;
    [SerializeField] private Transform bazookaMuzzle;

    [Header("Attack order")]
    [SerializeField]
    private TruckBossAttack[] attackBag = new TruckBossAttack[]
    {
        TruckBossAttack.MachineGun,
        TruckBossAttack.MachineGun,
        TruckBossAttack.Sniper,
        TruckBossAttack.Bazooka,
        TruckBossAttack.SummonPlanes
    };

    [Header("Aim limits")]
    [SerializeField] private float minAimY = 12f;
    [SerializeField] private float maxAimY = 28f;

    [Header("Machine gun")]
    [SerializeField] private float machineGunTelegraphDuration = 0.65f;
    [SerializeField] private int machineGunBulletCount = 8;
    [SerializeField] private float machineGunShotInterval = 0.12f;
    [SerializeField] private float machineGunBulletSpeed = 24f;
    [SerializeField] private float machineGunAimSpeed = 6f;

    [Header("Sniper")]
    [SerializeField] private int sniperShotCount = 3;
    [SerializeField] private float sniperTelegraphDuration = 0.45f;
    [SerializeField] private float sniperShotInterval = 0.25f;
    [SerializeField] private float sniperBulletSpeed = 45f;

    [Header("Bazooka")]
    [SerializeField] private ExplosiveMissilePool missilePool;
    [SerializeField] private int bazookaMissileCount = 2;
    [SerializeField] private float bazookaTelegraphDuration = 0.8f;
    [SerializeField] private float bazookaShotInterval = 0.45f;
    [SerializeField] private float bazookaMissileSpeed = 18f;

    [Header("Summon standard EnemyPlane")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EnemyBase enemyPlanePrefab;
    [SerializeField] private int summonCount = 4;
    [SerializeField] private float summonTelegraphDuration = 0.7f;
    [SerializeField] private float summonInterval = 0.35f;
    [SerializeField] private float summonSpawnX = 26f;
    [SerializeField] private float summonMinY = 13f;
    [SerializeField] private float summonMaxY = 27f;

    [Header("Recover")]
    [SerializeField] private float recoverDuration = 0.7f;

    private TruckBossState attackState = TruckBossState.Waiting;
    private TruckBossAttack[] runtimeAttackBag;
    private int attackBagIndex;
    private float stateTimer;
    private bool openingAttackUsed;

    private float machineAimY;
    private float machineGunShotTimer;
    private int machineGunShotsFired;

    private Vector2 lockedSniperDirection;
    private int sniperShotsFired;

    private Vector3 lockedBazookaTarget;
    private int bazookaMissilesFired;

    private BossReinforcementSequence reinforcementSequence =
        new BossReinforcementSequence();

    protected override void OnSetupComplete()
    {
        if (NormalBulletPool == null)
        {
            Debug.LogWarning("TruckBoss: pool dei proiettili normali non assegnato.", this);
        }

        if (missilePool == null)
        {
            Debug.LogWarning("TruckBoss: Missile Pool non assegnato.", this);
        }

        if (enemySpawner == null || enemyPlanePrefab == null)
        {
            Debug.LogWarning("TruckBoss: riferimenti del summon non assegnati.", this);
        }
    }

    protected override void OnBossActivated()
    {
        attackState = TruckBossState.Waiting;
        stateTimer = 0f;
        openingAttackUsed = false;

        machineGunShotTimer = 0f;
        machineGunShotsFired = 0;
        sniperShotsFired = 0;
        bazookaMissilesFired = 0;

        reinforcementSequence.Stop();

        SetAllGunnerColors(normalGunnerColor);
        BuildRuntimeAttackBag();
    }

    protected override void OnFightStarted()
    {
        BeginNextAttack();
    }

    protected override void TickFight(float deltaTime)
    {
        reinforcementSequence.Tick(deltaTime);
        stateTimer = stateTimer + deltaTime;

        if (attackState == TruckBossState.MachineGunTelegraph)
        {
            UpdateMachineGunTelegraph(deltaTime);
        }
        else if (attackState == TruckBossState.MachineGunBurst)
        {
            UpdateMachineGunBurst(deltaTime);
        }
        else if (attackState == TruckBossState.SniperTelegraph)
        {
            UpdateSniperTelegraph();
        }
        else if (attackState == TruckBossState.SniperGap)
        {
            UpdateSniperGap();
        }
        else if (attackState == TruckBossState.BazookaTelegraph)
        {
            UpdateBazookaTelegraph();
        }
        else if (attackState == TruckBossState.BazookaGap)
        {
            UpdateBazookaGap();
        }
        else if (attackState == TruckBossState.SummonTelegraph)
        {
            UpdateSummonTelegraph();
        }
        else if (attackState == TruckBossState.Recover)
        {
            UpdateRecover();
        }

        UpdateGunnerColors();
    }

    protected override void OnDefeatStarted()
    {
        reinforcementSequence.Stop();
        SetAllGunnerColors(normalGunnerColor);
    }

    protected override Color GetFightColor()
    {
        return NormalColor;
    }

    private void BeginNextAttack()
    {
        TruckBossAttack nextAttack;

        if (openingAttackUsed == false)
        {
            openingAttackUsed = true;
            nextAttack = TruckBossAttack.MachineGun;
        }
        else
        {
            nextAttack = TakeNextBagAttack();
        }

        if (nextAttack == TruckBossAttack.SummonPlanes
            && reinforcementSequence.IsActive)
        {
            nextAttack = TruckBossAttack.MachineGun;
        }

        if (nextAttack == TruckBossAttack.MachineGun)
        {
            BeginMachineGunAttack();
            return;
        }

        if (nextAttack == TruckBossAttack.Sniper)
        {
            BeginSniperAttack();
            return;
        }

        if (nextAttack == TruckBossAttack.Bazooka)
        {
            BeginBazookaAttack();
            return;
        }

        ChangeAttackState(TruckBossState.SummonTelegraph);
    }

    private void BeginMachineGunAttack()
    {
        machineGunShotsFired = 0;
        machineGunShotTimer = 0f;
        machineAimY = transform.position.y;

        if (Target != null)
        {
            machineAimY = Mathf.Clamp(Target.position.y, minAimY, maxAimY);
        }

        ChangeAttackState(TruckBossState.MachineGunTelegraph);
    }

    private void UpdateMachineGunTelegraph(float deltaTime)
    {
        TrackMachineGunAim(deltaTime);

        if (stateTimer < machineGunTelegraphDuration)
        {
            return;
        }

        ChangeAttackState(TruckBossState.MachineGunBurst);
        FireMachineGunBullet();
    }

    private void UpdateMachineGunBurst(float deltaTime)
    {
        TrackMachineGunAim(deltaTime);

        if (machineGunShotsFired >= machineGunBulletCount)
        {
            ChangeAttackState(TruckBossState.Recover);
            return;
        }

        machineGunShotTimer = machineGunShotTimer + deltaTime;
        float safeInterval = Mathf.Max(0.03f, machineGunShotInterval);

        if (machineGunShotTimer < safeInterval)
        {
            return;
        }

        machineGunShotTimer = machineGunShotTimer - safeInterval;
        FireMachineGunBullet();
    }

    private void TrackMachineGunAim(float deltaTime)
    {
        if (Target == null)
        {
            return;
        }

        float targetY = Mathf.Clamp(Target.position.y, minAimY, maxAimY);
        machineAimY = Mathf.MoveTowards(
            machineAimY,
            targetY,
            machineGunAimSpeed * deltaTime);
    }

    private void FireMachineGunBullet()
    {
        if (machineGunShotsFired >= machineGunBulletCount)
        {
            return;
        }

        Vector3 muzzlePosition = GetPointPosition(machineGunMuzzle);
        Vector2 aimPoint = new Vector2(GetTargetX(), machineAimY);
        Vector2 direction = aimPoint - (Vector2)muzzlePosition;

        FireNormalBullet(muzzlePosition, direction, machineGunBulletSpeed);
        machineGunShotsFired = machineGunShotsFired + 1;
    }

    private void BeginSniperAttack()
    {
        sniperShotsFired = 0;
        LockSniperDirection();
        ChangeAttackState(TruckBossState.SniperTelegraph);
    }

    private void UpdateSniperTelegraph()
    {
        if (stateTimer < sniperTelegraphDuration)
        {
            return;
        }

        Vector3 muzzlePosition = GetPointPosition(sniperMuzzle);
        FireNormalBullet(muzzlePosition, lockedSniperDirection, sniperBulletSpeed);
        sniperShotsFired = sniperShotsFired + 1;

        if (sniperShotsFired >= sniperShotCount)
        {
            ChangeAttackState(TruckBossState.Recover);
            return;
        }

        ChangeAttackState(TruckBossState.SniperGap);
    }

    private void UpdateSniperGap()
    {
        if (stateTimer < sniperShotInterval)
        {
            return;
        }

        LockSniperDirection();
        ChangeAttackState(TruckBossState.SniperTelegraph);
    }

    private void LockSniperDirection()
    {
        Vector3 muzzlePosition = GetPointPosition(sniperMuzzle);
        Vector3 targetPosition = GetTargetPosition(
            muzzlePosition,
            minAimY,
            maxAimY);
        lockedSniperDirection = targetPosition - muzzlePosition;

        if (lockedSniperDirection.sqrMagnitude < 0.0001f)
        {
            lockedSniperDirection = Vector2.left;
        }

        lockedSniperDirection.Normalize();
    }

    private void BeginBazookaAttack()
    {
        bazookaMissilesFired = 0;
        LockBazookaTarget();
        ChangeAttackState(TruckBossState.BazookaTelegraph);
    }

    private void UpdateBazookaTelegraph()
    {
        if (stateTimer < bazookaTelegraphDuration)
        {
            return;
        }

        FireBazookaMissile();
        bazookaMissilesFired = bazookaMissilesFired + 1;

        if (bazookaMissilesFired >= bazookaMissileCount)
        {
            ChangeAttackState(TruckBossState.Recover);
            return;
        }

        ChangeAttackState(TruckBossState.BazookaGap);
    }

    private void UpdateBazookaGap()
    {
        if (stateTimer < bazookaShotInterval)
        {
            return;
        }

        LockBazookaTarget();
        ChangeAttackState(TruckBossState.BazookaTelegraph);
    }

    private void LockBazookaTarget()
    {
        Vector3 muzzlePosition = GetPointPosition(bazookaMuzzle);
        lockedBazookaTarget = GetTargetPosition(
            muzzlePosition,
            minAimY,
            maxAimY);
    }

    private void FireBazookaMissile()
    {
        if (missilePool == null)
        {
            return;
        }

        ExplosiveMissile missile = missilePool.GetMissile();

        if (missile == null)
        {
            return;
        }

        missile.Launch(
            GetPointPosition(bazookaMuzzle),
            lockedBazookaTarget,
            bazookaMissileSpeed);
    }

    private void UpdateSummonTelegraph()
    {
        if (stateTimer < summonTelegraphDuration)
        {
            return;
        }

        StartSummonSequence();
        ChangeAttackState(TruckBossState.Recover);
    }

    private void StartSummonSequence()
    {
        reinforcementSequence.BeginSingle(
            enemySpawner,
            enemyPlanePrefab,
            summonCount,
            summonInterval,
            summonSpawnX,
            summonMinY,
            summonMaxY);
    }

    private void UpdateRecover()
    {
        if (stateTimer < recoverDuration)
        {
            return;
        }

        BeginNextAttack();
    }

    private TruckBossAttack TakeNextBagAttack()
    {
        if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
        {
            return TruckBossAttack.MachineGun;
        }

        if (attackBagIndex >= runtimeAttackBag.Length)
        {
            ShuffleRuntimeAttackBag();
        }

        TruckBossAttack result = runtimeAttackBag[attackBagIndex];
        attackBagIndex = attackBagIndex + 1;
        return result;
    }

    private void BuildRuntimeAttackBag()
    {
        if (attackBag == null || attackBag.Length == 0)
        {
            runtimeAttackBag = new TruckBossAttack[]
            {
                TruckBossAttack.MachineGun,
                TruckBossAttack.MachineGun,
                TruckBossAttack.Sniper,
                TruckBossAttack.Bazooka,
                TruckBossAttack.SummonPlanes
            };
        }
        else
        {
            runtimeAttackBag = new TruckBossAttack[attackBag.Length];

            for (int i = 0; i < attackBag.Length; i++)
            {
                runtimeAttackBag[i] = attackBag[i];
            }
        }

        ShuffleRuntimeAttackBag();
    }

    private void ShuffleRuntimeAttackBag()
    {
        for (int i = runtimeAttackBag.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            TruckBossAttack temporary = runtimeAttackBag[i];
            runtimeAttackBag[i] = runtimeAttackBag[swapIndex];
            runtimeAttackBag[swapIndex] = temporary;
        }

        attackBagIndex = 0;
    }

    private void FireNormalBullet(Vector3 position, Vector2 direction, float speed)
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

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.left;
        }

        bullet.Launch(position, direction.normalized, speed);
    }

    private float GetTargetX()
    {
        if (Target != null)
        {
            return Target.position.x;
        }

        return transform.position.x - 20f;
    }

    private void UpdateGunnerColors()
    {
        SetAllGunnerColors(normalGunnerColor);

        if (attackState == TruckBossState.MachineGunTelegraph)
        {
            SetRendererColor(
                machineGunnerRenderer,
                FlashBetween(
                    normalGunnerColor,
                    machineGunWarningColor,
                    flashSpeed));
            return;
        }

        if (attackState == TruckBossState.SniperTelegraph)
        {
            SetRendererColor(
                sniperRenderer,
                FlashBetween(
                    normalGunnerColor,
                    sniperWarningColor,
                    flashSpeed));
            return;
        }

        if (attackState == TruckBossState.BazookaTelegraph)
        {
            SetRendererColor(
                bazookaRenderer,
                FlashBetween(
                    normalGunnerColor,
                    bazookaWarningColor,
                    flashSpeed));
            return;
        }

        if (attackState == TruckBossState.SummonTelegraph)
        {
            Color summonColor = FlashBetween(
                normalGunnerColor,
                summonWarningColor,
                flashSpeed);
            SetAllGunnerColors(summonColor);
        }
    }

    private void SetAllGunnerColors(Color color)
    {
        SetRendererColor(machineGunnerRenderer, color);
        SetRendererColor(sniperRenderer, color);
        SetRendererColor(bazookaRenderer, color);
    }

    private void SetRendererColor(SpriteRenderer targetRenderer, Color color)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.color = color;
    }

    private void ChangeAttackState(TruckBossState nextState)
    {
        attackState = nextState;
        stateTimer = 0f;
    }
}
