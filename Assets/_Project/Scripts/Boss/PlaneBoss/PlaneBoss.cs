using UnityEngine;

public enum BossAttack
{
    TrackingBurst,
    HeavyShot,
    SummonPlanes
}

public enum BossState
{
    Waiting,
    BurstAlign,
    BurstTelegraph,
    BurstFire,
    HeavyAlign,
    HeavyTelegraph,
    SummonTelegraph,
    Recover
}

// City boss: a larger EnemyPlane with three attacks.
// The first attack is guaranteed at the beginning. Later attacks come from a shuffled
// bag containing three bursts, one heavy shot and one summon by default.
public class PlaneBoss : BossBase
{
    [Header("Visual warnings")]
    [SerializeField] private Color burstTelegraphColor = Color.red;
    [SerializeField] private Color heavyTelegraphColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color summonTelegraphColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] private float flashSpeed = 10f;

    [Header("Attack order")]
    [SerializeField]
    private BossAttack[] attackBag = new BossAttack[]
    {
        BossAttack.TrackingBurst,
        BossAttack.TrackingBurst,
        BossAttack.TrackingBurst,
        BossAttack.HeavyShot,
        BossAttack.SummonPlanes
    };

    [Header("Band limits")]
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    [Header("Tracking burst")]
    [SerializeField] private float burstAlignDuration = 1f;
    [SerializeField] private float burstAlignSpeed = 6f;
    [SerializeField] private float burstTelegraphDuration = 0.6f;
    [SerializeField] private float burstDuration = 3f;
    [SerializeField] private float burstTrackSpeed = 5.5f;
    [SerializeField] private float burstShotInterval = 0.4f;
    [SerializeField] private float normalBulletSpeed = 22f;

    [Header("Heavy shot")]
    [SerializeField] private BulletPool heavyBulletPool;
    [SerializeField] private float heavyAlignDuration = 1.2f;
    [SerializeField] private float heavyAlignSpeed = 6f;
    [SerializeField] private float heavyTelegraphDuration = 1.1f;
    [SerializeField] private float heavyBulletSpeed = 34f;

    [Header("Summon EnemyPlane")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EnemyBase enemyPlanePrefab;
    [SerializeField] private int summonCount = 4;
    [SerializeField] private float summonTelegraphDuration = 0.8f;
    [SerializeField] private float summonInterval = 0.35f;
    [SerializeField] private float summonSpawnX = 26f;
    [SerializeField] private float summonMinY = 13f;
    [SerializeField] private float summonMaxY = 27f;

    [Header("Recover")]
    [SerializeField] private float recoverDuration = 0.7f;

    [Header("Optional muzzle")]
    [SerializeField] private Transform muzzle;

    private BossState attackState = BossState.Waiting;
    private BossAttack[] runtimeAttackBag;
    private int attackBagIndex;
    private float stateTimer;
    private float burstShotTimer;
    private float summonTimer;
    private int summonIndex;
    private EnemyBase activeSummonedPlane;
    private bool summonSequenceActive;
    private bool openingBurstUsed;

    protected override void OnSetupComplete()
    {
        if (heavyBulletPool == null)
        {
            Debug.LogWarning("CityBoss: heavyBulletPool non assegnato, "
                + "il colpo pesante restera' muto.", this);
        }

        if (enemySpawner == null || enemyPlanePrefab == null)
        {
            Debug.LogWarning("CityBoss: enemySpawner o enemyPlanePrefab non assegnati, "
                + "il summon non funzionera'.", this);
        }
    }

    protected override void OnBossActivated()
    {
        attackState = BossState.Waiting;
        stateTimer = 0f;
        burstShotTimer = 0f;
        summonTimer = 0f;
        summonIndex = 0;
        activeSummonedPlane = null;
        summonSequenceActive = false;
        openingBurstUsed = false;
        BuildRuntimeAttackBag();
    }

    protected override void OnFightStarted()
    {
        BeginNextAttack();
    }

    protected override void TickFight(float deltaTime)
    {
        UpdateSummonSequence(deltaTime);
        stateTimer = stateTimer + deltaTime;

        if (attackState == BossState.BurstAlign)
        {
            UpdateBurstAlign(deltaTime);
            return;
        }

        if (attackState == BossState.BurstTelegraph)
        {
            UpdateBurstTelegraph();
            return;
        }

        if (attackState == BossState.BurstFire)
        {
            UpdateBurstFire(deltaTime);
            return;
        }

        if (attackState == BossState.HeavyAlign)
        {
            UpdateHeavyAlign(deltaTime);
            return;
        }

        if (attackState == BossState.HeavyTelegraph)
        {
            UpdateHeavyTelegraph();
            return;
        }

        if (attackState == BossState.SummonTelegraph)
        {
            UpdateSummonTelegraph();
            return;
        }

        if (attackState == BossState.Recover)
        {
            UpdateRecover();
        }
    }

    protected override void OnDefeatStarted()
    {
        summonSequenceActive = false;
        activeSummonedPlane = null;
    }

    protected override Color GetFightColor()
    {
        if (attackState == BossState.BurstTelegraph)
        {
            return FlashBetween(NormalColor, burstTelegraphColor);
        }

        if (attackState == BossState.HeavyTelegraph)
        {
            return FlashBetween(NormalColor, heavyTelegraphColor);
        }

        if (attackState == BossState.SummonTelegraph)
        {
            return FlashBetween(NormalColor, summonTelegraphColor);
        }

        return NormalColor;
    }

    private void UpdateBurstAlign(float deltaTime)
    {
        TrackPlayer(burstAlignSpeed, deltaTime);

        if (stateTimer < burstAlignDuration)
        {
            return;
        }

        ChangeCityState(BossState.BurstTelegraph);
    }

    private void UpdateBurstTelegraph()
    {
        if (stateTimer < burstTelegraphDuration)
        {
            return;
        }

        FireNormalBullet();
        burstShotTimer = 0f;
        ChangeCityState(BossState.BurstFire);
    }

    private void UpdateBurstFire(float deltaTime)
    {
        TrackPlayer(burstTrackSpeed, deltaTime);
        burstShotTimer = burstShotTimer + deltaTime;

        float safeInterval = Mathf.Max(0.05f, burstShotInterval);

        if (burstShotTimer >= safeInterval)
        {
            burstShotTimer = burstShotTimer - safeInterval;
            FireNormalBullet();
        }

        if (stateTimer < burstDuration)
        {
            return;
        }

        ChangeCityState(BossState.Recover);
    }

    private void UpdateHeavyAlign(float deltaTime)
    {
        TrackPlayer(heavyAlignSpeed, deltaTime);

        if (stateTimer < heavyAlignDuration)
        {
            return;
        }

        ChangeCityState(BossState.HeavyTelegraph);
    }

    private void UpdateHeavyTelegraph()
    {
        if (stateTimer < heavyTelegraphDuration)
        {
            return;
        }

        FireHeavyBullet();
        ChangeCityState(BossState.Recover);
    }

    private void UpdateSummonTelegraph()
    {
        if (stateTimer < summonTelegraphDuration)
        {
            return;
        }

        StartSummonSequence();
        ChangeCityState(BossState.Recover);
    }

    private void UpdateRecover()
    {
        if (stateTimer < recoverDuration)
        {
            return;
        }

        BeginNextAttack();
    }

    private void BeginNextAttack()
    {
        BossAttack nextAttack;

        if (openingBurstUsed == false)
        {
            openingBurstUsed = true;
            nextAttack = BossAttack.TrackingBurst;
        }
        else
        {
            nextAttack = TakeNextBagAttack();
        }

        if (nextAttack == BossAttack.SummonPlanes && summonSequenceActive)
        {
            nextAttack = BossAttack.TrackingBurst;
        }

        if (nextAttack == BossAttack.TrackingBurst)
        {
            ChangeCityState(BossState.BurstAlign);
            return;
        }

        if (nextAttack == BossAttack.HeavyShot)
        {
            ChangeCityState(BossState.HeavyAlign);
            return;
        }

        ChangeCityState(BossState.SummonTelegraph);
    }

    private BossAttack TakeNextBagAttack()
    {
        if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
        {
            return BossAttack.TrackingBurst;
        }

        if (attackBagIndex >= runtimeAttackBag.Length)
        {
            ShuffleRuntimeAttackBag();
        }

        BossAttack result = runtimeAttackBag[attackBagIndex];
        attackBagIndex = attackBagIndex + 1;
        return result;
    }

    private void BuildRuntimeAttackBag()
    {
        if (attackBag == null || attackBag.Length == 0)
        {
            runtimeAttackBag = new BossAttack[]
            {
                BossAttack.TrackingBurst,
                BossAttack.TrackingBurst,
                BossAttack.TrackingBurst,
                BossAttack.HeavyShot,
                BossAttack.SummonPlanes
            };
        }
        else
        {
            runtimeAttackBag = new BossAttack[attackBag.Length];

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
            BossAttack temporary = runtimeAttackBag[i];
            runtimeAttackBag[i] = runtimeAttackBag[swapIndex];
            runtimeAttackBag[swapIndex] = temporary;
        }

        attackBagIndex = 0;
    }

    private void TrackPlayer(float speed, float deltaTime)
    {
        if (Target == null)
        {
            return;
        }

        float targetY = Mathf.Clamp(Target.position.y, minY, maxY);
        float nextY = Mathf.MoveTowards(transform.position.y, targetY, speed * deltaTime);
        SetY(nextY);
    }

    private void FireNormalBullet()
    {
        FireFromPool(NormalBulletPool, normalBulletSpeed);
    }

    private void FireHeavyBullet()
    {
        FireFromPool(heavyBulletPool, heavyBulletSpeed);
    }

    private void FireFromPool(BulletPool pool, float speed)
    {
        if (pool == null)
        {
            return;
        }

        Bullet bullet = pool.GetBullet();

        if (bullet == null)
        {
            return;
        }

        bullet.Launch(GetMuzzlePosition(), Vector2.left, speed);
    }

    private Vector3 GetMuzzlePosition()
    {
        if (muzzle != null)
        {
            return muzzle.position;
        }

        return transform.position;
    }

    private void StartSummonSequence()
    {
        if (summonCount <= 0)
        {
            return;
        }

        summonIndex = 0;
        summonTimer = 0f;
        activeSummonedPlane = null;
        summonSequenceActive = true;
        TrySpawnNextPlane();
    }

    private void UpdateSummonSequence(float deltaTime)
    {
        if (summonSequenceActive == false)
        {
            return;
        }

        if (activeSummonedPlane != null)
        {
            if (activeSummonedPlane.gameObject.activeSelf)
            {
                return;
            }

            activeSummonedPlane = null;
            summonTimer = 0f;
        }

        if (summonIndex >= summonCount)
        {
            summonSequenceActive = false;
            return;
        }

        summonTimer = summonTimer + deltaTime;

        if (summonTimer < summonInterval)
        {
            return;
        }

        summonTimer = 0f;
        TrySpawnNextPlane();
    }

    private void TrySpawnNextPlane()
    {
        if (summonIndex >= summonCount)
        {
            return;
        }

        float progress = 0.5f;

        if (summonCount > 0)
        {
            progress = (summonIndex + 0.5f) / summonCount;
        }

        float spawnY = Mathf.Lerp(summonMinY, summonMaxY, progress);
        Vector3 spawnPosition = new Vector3(summonSpawnX, spawnY, 0f);

        if (enemySpawner == null || enemyPlanePrefab == null)
        {
            summonSequenceActive = false;
            return;
        }

        EnemyBase spawnedPlane = enemySpawner.SpawnForBoss(enemyPlanePrefab, spawnPosition);

        if (spawnedPlane == null)
        {
            return;
        }

        activeSummonedPlane = spawnedPlane;
        summonIndex = summonIndex + 1;
    }

    private Color FlashBetween(Color from, Color to)
    {
        float blend = Mathf.PingPong(Time.time * flashSpeed, 1f);
        return Color.Lerp(from, to, blend);
    }

    private void ChangeCityState(BossState nextState)
    {
        attackState = nextState;
        stateTimer = 0f;
    }
}