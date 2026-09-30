// Gestisce gli attacchi dei tre tiratori del boss camion o mongolfiera.
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.Enemies;

namespace BeyondSky.Boss
{
    public enum ArmedTransportBossAttack
    {
        MachineGun,
        Sniper,
        Bazooka,
        SummonPlanes
    }

    public enum ArmedTransportBossState
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
    public class ArmedTransportBoss : BossBase
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
        private ArmedTransportBossAttack[] attackBag = new ArmedTransportBossAttack[]
        {
            ArmedTransportBossAttack.MachineGun,
            ArmedTransportBossAttack.MachineGun,
            ArmedTransportBossAttack.Sniper,
            ArmedTransportBossAttack.Bazooka,
            ArmedTransportBossAttack.SummonPlanes
        };

        [Header("Machine gun")]
        [SerializeField] private float machineGunTelegraphDuration = 0.65f;
        [SerializeField] private int machineGunBulletCount = 8;
        [SerializeField] private float machineGunShotInterval = 0.12f;
        [SerializeField] private float machineGunBulletSpeed = 24f;

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

        private ArmedTransportBossState attackState = ArmedTransportBossState.Waiting;
        private ArmedTransportBossAttack[] runtimeAttackBag;
        private int attackBagIndex;
        private float stateTimer;
        private bool openingAttackUsed;

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
                Debug.LogWarning("ArmedTransportBoss: pool dei proiettili normali non assegnato.", this);
            }

            if (missilePool == null)
            {
                Debug.LogWarning("ArmedTransportBoss: Missile Pool non assegnato.", this);
            }

            if (enemySpawner == null || enemyPlanePrefab == null)
            {
                Debug.LogWarning("ArmedTransportBoss: riferimenti del summon non assegnati.", this);
            }
        }

        protected override void OnBossActivated()
        {
            attackState = ArmedTransportBossState.Waiting;
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

            if (attackState == ArmedTransportBossState.MachineGunTelegraph)
            {
                UpdateMachineGunTelegraph();
            }
            else if (attackState == ArmedTransportBossState.MachineGunBurst)
            {
                UpdateMachineGunBurst(deltaTime);
            }
            else if (attackState == ArmedTransportBossState.SniperTelegraph)
            {
                UpdateSniperTelegraph();
            }
            else if (attackState == ArmedTransportBossState.SniperGap)
            {
                UpdateSniperGap();
            }
            else if (attackState == ArmedTransportBossState.BazookaTelegraph)
            {
                UpdateBazookaTelegraph();
            }
            else if (attackState == ArmedTransportBossState.BazookaGap)
            {
                UpdateBazookaGap();
            }
            else if (attackState == ArmedTransportBossState.SummonTelegraph)
            {
                UpdateSummonTelegraph();
            }
            else if (attackState == ArmedTransportBossState.Recover)
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

        private void BeginNextAttack()
        {
            ArmedTransportBossAttack nextAttack;

            if (openingAttackUsed == false)
            {
                openingAttackUsed = true;
                nextAttack = ArmedTransportBossAttack.MachineGun;
            }
            else
            {
                nextAttack = TakeNextBagAttack();
            }

            if (nextAttack == ArmedTransportBossAttack.SummonPlanes
                && reinforcementSequence.IsActive)
            {
                nextAttack = ArmedTransportBossAttack.MachineGun;
            }

            if (nextAttack == ArmedTransportBossAttack.MachineGun)
            {
                BeginMachineGunAttack();
                return;
            }

            if (nextAttack == ArmedTransportBossAttack.Sniper)
            {
                BeginSniperAttack();
                return;
            }

            if (nextAttack == ArmedTransportBossAttack.Bazooka)
            {
                BeginBazookaAttack();
                return;
            }

            ChangeAttackState(ArmedTransportBossState.SummonTelegraph);
        }

        private void BeginMachineGunAttack()
        {
            machineGunShotsFired = 0;
            machineGunShotTimer = 0f;

            ChangeAttackState(ArmedTransportBossState.MachineGunTelegraph);
        }

        private void UpdateMachineGunTelegraph()
        {
            if (stateTimer < machineGunTelegraphDuration)
            {
                return;
            }

            ChangeAttackState(ArmedTransportBossState.MachineGunBurst);
            FireMachineGunBullet();
        }

        private void UpdateMachineGunBurst(float deltaTime)
        {
            if (machineGunShotsFired >= machineGunBulletCount)
            {
                ChangeAttackState(ArmedTransportBossState.Recover);
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

        private Vector3 GetPlayerAimPosition(Vector3 muzzlePosition)
        {
            if (Target != null)
            {
                return new Vector3(Target.position.x, Target.position.y, 0f);
            }

            return muzzlePosition + Vector3.left * 20f;
        }

        private void FireMachineGunBullet()
        {
            if (machineGunShotsFired >= machineGunBulletCount)
            {
                return;
            }

            Vector3 muzzlePosition = GetPointPosition(machineGunMuzzle);
            Vector3 targetPosition = GetPlayerAimPosition(muzzlePosition);
            Vector2 direction = targetPosition - muzzlePosition;

            FireNormalBullet(muzzlePosition, direction, machineGunBulletSpeed);
            machineGunShotsFired = machineGunShotsFired + 1;
        }

        private void BeginSniperAttack()
        {
            sniperShotsFired = 0;
            LockSniperDirection();
            ChangeAttackState(ArmedTransportBossState.SniperTelegraph);
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
                ChangeAttackState(ArmedTransportBossState.Recover);
                return;
            }

            ChangeAttackState(ArmedTransportBossState.SniperGap);
        }

        private void UpdateSniperGap()
        {
            if (stateTimer < sniperShotInterval)
            {
                return;
            }

            LockSniperDirection();
            ChangeAttackState(ArmedTransportBossState.SniperTelegraph);
        }

        private void LockSniperDirection()
        {
            Vector3 muzzlePosition = GetPointPosition(sniperMuzzle);
            Vector3 targetPosition = GetPlayerAimPosition(muzzlePosition);
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
            ChangeAttackState(ArmedTransportBossState.BazookaTelegraph);
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
                ChangeAttackState(ArmedTransportBossState.Recover);
                return;
            }

            ChangeAttackState(ArmedTransportBossState.BazookaGap);
        }

        private void UpdateBazookaGap()
        {
            if (stateTimer < bazookaShotInterval)
            {
                return;
            }

            LockBazookaTarget();
            ChangeAttackState(ArmedTransportBossState.BazookaTelegraph);
        }

        private void LockBazookaTarget()
        {
            Vector3 muzzlePosition = GetPointPosition(bazookaMuzzle);
            lockedBazookaTarget = GetPlayerAimPosition(muzzlePosition);
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
            ChangeAttackState(ArmedTransportBossState.Recover);
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

        private ArmedTransportBossAttack TakeNextBagAttack()
        {
            if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
            {
                return ArmedTransportBossAttack.MachineGun;
            }

            if (attackBagIndex >= runtimeAttackBag.Length)
            {
                ShuffleRuntimeAttackBag();
            }

            ArmedTransportBossAttack result = runtimeAttackBag[attackBagIndex];
            attackBagIndex = attackBagIndex + 1;
            return result;
        }

        private void BuildRuntimeAttackBag()
        {
            if (attackBag == null || attackBag.Length == 0)
            {
                runtimeAttackBag = new ArmedTransportBossAttack[]
                {
                    ArmedTransportBossAttack.MachineGun,
                    ArmedTransportBossAttack.MachineGun,
                    ArmedTransportBossAttack.Sniper,
                    ArmedTransportBossAttack.Bazooka,
                    ArmedTransportBossAttack.SummonPlanes
                };
            }
            else
            {
                runtimeAttackBag = new ArmedTransportBossAttack[attackBag.Length];

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
                ArmedTransportBossAttack temporary = runtimeAttackBag[i];
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

            AudioSignals.Play(GameSound.EnemyShot);
        }

        private void UpdateGunnerColors()
        {
            SetAllGunnerColors(normalGunnerColor);

            if (attackState == ArmedTransportBossState.MachineGunTelegraph)
            {
                SetRendererColor(
                    machineGunnerRenderer,
                    FlashBetween(
                        normalGunnerColor,
                        machineGunWarningColor,
                        flashSpeed));
                return;
            }

            if (attackState == ArmedTransportBossState.SniperTelegraph)
            {
                SetRendererColor(
                    sniperRenderer,
                    FlashBetween(
                        normalGunnerColor,
                        sniperWarningColor,
                        flashSpeed));
                return;
            }

            if (attackState == ArmedTransportBossState.BazookaTelegraph)
            {
                SetRendererColor(
                    bazookaRenderer,
                    FlashBetween(
                        normalGunnerColor,
                        bazookaWarningColor,
                        flashSpeed));
                return;
            }

            if (attackState == ArmedTransportBossState.SummonTelegraph)
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

        private void ChangeAttackState(ArmedTransportBossState nextState)
        {
            if (nextState == ArmedTransportBossState.MachineGunTelegraph ||
                nextState == ArmedTransportBossState.SniperTelegraph ||
                nextState == ArmedTransportBossState.BazookaTelegraph ||
                nextState == ArmedTransportBossState.SummonTelegraph)
            {
                AudioSignals.Play(GameSound.EnemyWarning);
            }

            attackState = nextState;
            stateTimer = 0f;
        }
    }
}
