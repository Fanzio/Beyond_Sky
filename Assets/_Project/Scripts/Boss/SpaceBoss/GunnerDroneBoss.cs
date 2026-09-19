using System.Collections.Generic;
using UnityEngine;

public enum GunnerDroneAttack
{
    DualTrackingBurst,
    DualHeavyShot,
    RandomReinforcements
}

public enum GunnerDroneState
{
    Waiting,
    BurstAlign,
    BurstTelegraph,
    BurstFire,
    HeavyAlign,
    HeavyTelegraph,
    ReinforcementTelegraph,
    Recover
}

// Dual-gun space boss based on PlaneBoss, with random pooled reinforcements.
public class GunnerDroneBoss : BossBase
{
    [Header("Visual warnings")]
    [SerializeField] private Color burstWarningColor = Color.red;
    [SerializeField] private Color heavyWarningColor = new Color(0.4f, 1f, 0.2f);
    [SerializeField] private Color reinforcementWarningColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private float flashSpeed = 10f;

    [Header("Weapon points")]
    [SerializeField] private Transform upperMuzzle;
    [SerializeField] private Transform lowerMuzzle;

    [Header("Attack order")]
    [SerializeField]
    private GunnerDroneAttack[] attackBag = new GunnerDroneAttack[]
    {
        GunnerDroneAttack.DualTrackingBurst,
        GunnerDroneAttack.DualTrackingBurst,
        GunnerDroneAttack.DualTrackingBurst,
        GunnerDroneAttack.DualHeavyShot,
        GunnerDroneAttack.RandomReinforcements
    };

    [Header("Band limits")]
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    [Header("Dual tracking burst")]
    [SerializeField] private BulletPool gunnerBulletPool;
    [SerializeField] private float burstAlignDuration = 1f;
    [SerializeField] private float burstAlignSpeed = 6f;
    [SerializeField] private float burstTelegraphDuration = 0.6f;
    [SerializeField] private float burstDuration = 3f;
    [SerializeField] private float burstTrackSpeed = 5.5f;
    [SerializeField] private float burstShotInterval = 0.4f;
    [SerializeField] private float normalBulletSpeed = 22f;

    [Header("Dual heavy shot")]
    [SerializeField] private BulletPool heavyBulletPool;
    [SerializeField] private float heavyAlignDuration = 1.2f;
    [SerializeField] private float heavyAlignSpeed = 6f;
    [SerializeField] private float heavyTelegraphDuration = 1.1f;
    [SerializeField] private float heavyBulletSpeed = 34f;

    [Header("Random reinforcements - first 3 entries are used")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EnemyBase[] reinforcementPrefabs;
    [SerializeField] private int reinforcementCount = 4;
    [SerializeField] private float reinforcementTelegraphDuration = 0.8f;
    [SerializeField] private float reinforcementInterval = 0.35f;
    [SerializeField] private float reinforcementSpawnX = 26f;
    [SerializeField] private float reinforcementMinY = 13f;
    [SerializeField] private float reinforcementMaxY = 27f;

    [Header("Recover")]
    [SerializeField] private float recoverDuration = 0.7f;

    private GunnerDroneState attackState = GunnerDroneState.Waiting;
    private GunnerDroneAttack[] runtimeAttackBag;
    private int attackBagIndex;
    private bool openingBurstUsed;
    private float stateTimer;
    private float burstShotTimer;

    private EnemyBase[] validReinforcements = new EnemyBase[0];
    private BossReinforcementSequence reinforcementSequence =
        new BossReinforcementSequence();

    protected override void OnSetupComplete()
    {
        if (upperMuzzle == null || lowerMuzzle == null)
        {
            Debug.LogWarning(
                "GunnerDroneBoss: Upper Muzzle o Lower Muzzle non assegnato.",
                this);
        }

        if (gunnerBulletPool == null)
        {
            Debug.LogWarning("GunnerDroneBoss: Gunner Bullet Pool non assegnata, verra' usata la pool normale del BossManager.", this);
        }

        if (heavyBulletPool == null)
        {
            Debug.LogWarning("GunnerDroneBoss: Heavy Bullet Pool non assegnata.", this);
        }

        BuildValidReinforcements();

        if (enemySpawner == null || validReinforcements.Length == 0)
        {
            Debug.LogWarning("GunnerDroneBoss: rinforzi non configurati.", this);
        }

        if (reinforcementPrefabs != null && reinforcementPrefabs.Length > 3)
        {
            Debug.LogWarning("GunnerDroneBoss: saranno usati solo i primi 3 prefab di rinforzo.", this);
        }
    }

    protected override void OnBossActivated()
    {
        attackState = GunnerDroneState.Waiting;
        stateTimer = 0f;
        burstShotTimer = 0f;
        openingBurstUsed = false;
        reinforcementSequence.Stop();
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

        if (attackState == GunnerDroneState.BurstAlign)
        {
            UpdateBurstAlign(deltaTime);
        }
        else if (attackState == GunnerDroneState.BurstTelegraph)
        {
            UpdateBurstTelegraph();
        }
        else if (attackState == GunnerDroneState.BurstFire)
        {
            UpdateBurstFire(deltaTime);
        }
        else if (attackState == GunnerDroneState.HeavyAlign)
        {
            UpdateHeavyAlign(deltaTime);
        }
        else if (attackState == GunnerDroneState.HeavyTelegraph)
        {
            UpdateHeavyTelegraph();
        }
        else if (attackState == GunnerDroneState.ReinforcementTelegraph)
        {
            UpdateReinforcementTelegraph();
        }
        else if (attackState == GunnerDroneState.Recover)
        {
            UpdateRecover();
        }
    }

    protected override void OnDefeatStarted()
    {
        reinforcementSequence.Stop();
    }

    protected override Color GetFightColor()
    {
        if (attackState == GunnerDroneState.BurstTelegraph)
        {
            return FlashBetween(NormalColor, burstWarningColor, flashSpeed);
        }

        if (attackState == GunnerDroneState.HeavyTelegraph)
        {
            return FlashBetween(NormalColor, heavyWarningColor, flashSpeed);
        }

        if (attackState == GunnerDroneState.ReinforcementTelegraph)
        {
            return FlashBetween(
                NormalColor,
                reinforcementWarningColor,
                flashSpeed);
        }

        return NormalColor;
    }

    private void BeginNextAttack()
    {
        GunnerDroneAttack nextAttack;

        if (openingBurstUsed == false)
        {
            openingBurstUsed = true;
            nextAttack = GunnerDroneAttack.DualTrackingBurst;
        }
        else
        {
            nextAttack = TakeNextBagAttack();
        }

        if (nextAttack == GunnerDroneAttack.RandomReinforcements
            && reinforcementSequence.IsActive)
        {
            nextAttack = GunnerDroneAttack.DualTrackingBurst;
        }

        if (nextAttack == GunnerDroneAttack.DualTrackingBurst)
        {
            ChangeAttackState(GunnerDroneState.BurstAlign);
            return;
        }

        if (nextAttack == GunnerDroneAttack.DualHeavyShot)
        {
            ChangeAttackState(GunnerDroneState.HeavyAlign);
            return;
        }

        ChangeAttackState(GunnerDroneState.ReinforcementTelegraph);
    }

    private void UpdateBurstAlign(float deltaTime)
    {
        TrackPointPairToTargetY(
            upperMuzzle,
            lowerMuzzle,
            burstAlignSpeed,
            minY,
            maxY,
            deltaTime);

        if (stateTimer < burstAlignDuration)
        {
            return;
        }

        ChangeAttackState(GunnerDroneState.BurstTelegraph);
    }

    private void UpdateBurstTelegraph()
    {
        if (stateTimer < burstTelegraphDuration)
        {
            return;
        }

        FireDualBullets(GetNormalFirePool(), normalBulletSpeed);
        burstShotTimer = 0f;
        ChangeAttackState(GunnerDroneState.BurstFire);
    }

    private void UpdateBurstFire(float deltaTime)
    {
        TrackPointPairToTargetY(
            upperMuzzle,
            lowerMuzzle,
            burstTrackSpeed,
            minY,
            maxY,
            deltaTime);
        burstShotTimer = burstShotTimer + deltaTime;
        float safeInterval = Mathf.Max(0.05f, burstShotInterval);

        if (burstShotTimer >= safeInterval)
        {
            burstShotTimer = burstShotTimer - safeInterval;
            FireDualBullets(GetNormalFirePool(), normalBulletSpeed);
        }

        if (stateTimer < burstDuration)
        {
            return;
        }

        ChangeAttackState(GunnerDroneState.Recover);
    }

    private void UpdateHeavyAlign(float deltaTime)
    {
        TrackPointPairToTargetY(
            upperMuzzle,
            lowerMuzzle,
            heavyAlignSpeed,
            minY,
            maxY,
            deltaTime);

        if (stateTimer < heavyAlignDuration)
        {
            return;
        }

        ChangeAttackState(GunnerDroneState.HeavyTelegraph);
    }

    private void UpdateHeavyTelegraph()
    {
        if (stateTimer < heavyTelegraphDuration)
        {
            return;
        }

        FireDualBullets(heavyBulletPool, heavyBulletSpeed);
        ChangeAttackState(GunnerDroneState.Recover);
    }

    private void FireDualBullets(BulletPool pool, float speed)
    {
        if (pool == null)
        {
            return;
        }

        FireOneBullet(pool, GetPointPosition(upperMuzzle), speed);
        FireOneBullet(pool, GetPointPosition(lowerMuzzle), speed);
    }

    private BulletPool GetNormalFirePool()
    {
        if (gunnerBulletPool != null)
        {
            return gunnerBulletPool;
        }

        return NormalBulletPool;
    }

    private void FireOneBullet(BulletPool pool, Vector3 position, float speed)
    {
        Bullet bullet = pool.GetBullet();

        if (bullet == null)
        {
            return;
        }

        bullet.Launch(position, Vector2.left, speed);

        AudioSignals.Play(GameSound.EnemyShot);
    }

    private void UpdateReinforcementTelegraph()
    {
        if (stateTimer < reinforcementTelegraphDuration)
        {
            return;
        }

        StartReinforcementSequence();
        ChangeAttackState(GunnerDroneState.Recover);
    }

    private void StartReinforcementSequence()
    {
        reinforcementSequence.BeginRandom(
            enemySpawner,
            validReinforcements,
            reinforcementCount,
            reinforcementInterval,
            reinforcementSpawnX,
            reinforcementMinY,
            reinforcementMaxY);
    }

    private void BuildValidReinforcements()
    {
        List<EnemyBase> valid = new List<EnemyBase>();

        if (reinforcementPrefabs != null)
        {
            int limit = Mathf.Min(3, reinforcementPrefabs.Length);

            for (int i = 0; i < limit; i++)
            {
                EnemyBase prefab = reinforcementPrefabs[i];

                if (prefab == null || valid.Contains(prefab))
                {
                    continue;
                }

                valid.Add(prefab);
            }
        }

        validReinforcements = valid.ToArray();
    }

    private void UpdateRecover()
    {
        if (stateTimer < recoverDuration)
        {
            return;
        }

        BeginNextAttack();
    }

    private GunnerDroneAttack TakeNextBagAttack()
    {
        if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
        {
            return GunnerDroneAttack.DualTrackingBurst;
        }

        if (attackBagIndex >= runtimeAttackBag.Length)
        {
            ShuffleRuntimeAttackBag();
        }

        GunnerDroneAttack result = runtimeAttackBag[attackBagIndex];
        attackBagIndex = attackBagIndex + 1;
        return result;
    }

    private void BuildRuntimeAttackBag()
    {
        if (attackBag == null || attackBag.Length == 0)
        {
            runtimeAttackBag = new GunnerDroneAttack[]
            {
                GunnerDroneAttack.DualTrackingBurst,
                GunnerDroneAttack.DualTrackingBurst,
                GunnerDroneAttack.DualTrackingBurst,
                GunnerDroneAttack.DualHeavyShot,
                GunnerDroneAttack.RandomReinforcements
            };
        }
        else
        {
            runtimeAttackBag = new GunnerDroneAttack[attackBag.Length];

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
            GunnerDroneAttack temporary = runtimeAttackBag[i];
            runtimeAttackBag[i] = runtimeAttackBag[swapIndex];
            runtimeAttackBag[swapIndex] = temporary;
        }

        attackBagIndex = 0;
    }

    private void ChangeAttackState(GunnerDroneState nextState)
    {
        if (nextState == GunnerDroneState.BurstTelegraph ||
            nextState == GunnerDroneState.HeavyTelegraph ||
            nextState == GunnerDroneState.ReinforcementTelegraph)
        {
            AudioSignals.Play(GameSound.EnemyWarning);
        }

        attackState = nextState;
        stateTimer = 0f;
    }
}
