// Gestisce gli attacchi, i segnali e il movimento del boss elicottero.
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.Enemies;
using BeyondSky.Player;

namespace BeyondSky.Boss
{
    public enum HelicopterBossAttack
    {
        FanVolley,
        AerialMines,
        StrafeCharge
    }

    public enum HelicopterBossState
    {
        Waiting,
        FanTelegraph,
        FanVolleyGap,
        MineTelegraph,
        MineDeploying,
        StrafeTracking,
        StrafeLocked,
        StrafeDashing,
        StrafeReturning,
        Recover
    }

    public class HelicopterBoss : BossBase
    {
        [Header("Visual warnings")]
        [SerializeField] private Color fanWarningColor = Color.yellow;
        [SerializeField] private Color mineWarningColor = new Color(1f, 0.25f, 0.85f);
        [SerializeField] private Color strafeWarningColor = Color.red;
        [SerializeField] private float flashSpeed = 12f;

        [Header("References")]
        [SerializeField] private Transform gunMuzzle;
        [SerializeField] private Transform mineDropPoint;
        [SerializeField] private AerialMinePool minePool;
        [SerializeField] private LineRenderer strafeWarningLine;

        [Header("Attack order")]
        [SerializeField]
        private HelicopterBossAttack[] attackBag = new HelicopterBossAttack[]
        {
            HelicopterBossAttack.FanVolley,
            HelicopterBossAttack.FanVolley,
            HelicopterBossAttack.AerialMines,
            HelicopterBossAttack.AerialMines,
            HelicopterBossAttack.StrafeCharge
        };

        [Header("Aim limits")]
        [SerializeField] private float minAimY = 12f;
        [SerializeField] private float maxAimY = 28f;

        [Header("Fan volley")]
        [SerializeField] private float fanTelegraphDuration = 0.7f;
        [SerializeField] private int fanVolleyCount = 2;
        [SerializeField] private int fanBulletsPerVolley = 5;
        [SerializeField] private float fanAngleSpacing = 12f;
        [SerializeField] private float fanBulletSpeed = 22f;
        [SerializeField] private float fanVolleyGap = 0.55f;

        [Header("Aerial mines")]
        [SerializeField] private float mineTelegraphDuration = 0.7f;
        [SerializeField] private int mineCount = 2;
        [SerializeField] private float mineDeployInterval = 0.15f;
        [SerializeField] private float mineTravelDuration = 0.55f;
        [Min(0.05f)]
        [SerializeField] private float mineArmedDuration = 0.65f;
        [SerializeField] private float mineFieldX = 0f;
        [SerializeField] private float mineMinY = 12f;
        [SerializeField] private float mineMaxY = 28f;

        [Header("Strafe charge")]
        [SerializeField] private LayerMask playerLayers;
        [SerializeField] private float strafeTrackingDuration = 0.7f;
        [SerializeField] private float strafeLockedWarningDuration = 0.55f;
        [SerializeField] private float strafeAlignmentSpeed = 10f;
        [SerializeField] private float strafeSpeed = 32f;
        [SerializeField] private float strafeExitX = -18f;
        [SerializeField] private float strafeReturnX = 30f;
        [SerializeField] private float strafeReturnSpeed = 18f;
        [SerializeField] private Vector2 strafeHitboxSize = new Vector2(5.5f, 1.5f);
        [SerializeField] private Vector2 strafeHitboxOffset = Vector2.zero;
        [SerializeField] private float warningLineLeftX = -16f;
        [SerializeField] private float warningLineRightX = 16f;

        [Header("Recover")]
        [SerializeField] private float recoverDuration = 0.75f;

        private HelicopterBossState attackState = HelicopterBossState.Waiting;
        private HelicopterBossAttack[] runtimeAttackBag;
        private int attackBagIndex;
        private bool openingAttackUsed;
        private float stateTimer;
        private Vector3 homePosition;

        private int fanVolleysFired;
        private int minesDeployed;
        private float mineDeployTimer;
        private float lockedStrafeY;

        protected override void OnSetupComplete()
        {
            if (NormalBulletPool == null)
            {
                Debug.LogWarning("HelicopterBoss: pool dei proiettili normali non assegnato.", this);
            }

            if (minePool == null)
            {
                Debug.LogWarning("HelicopterBoss: Mine Pool non assegnata.", this);
            }

            if (strafeWarningLine != null)
            {
                strafeWarningLine.useWorldSpace = true;
                strafeWarningLine.positionCount = 2;
                strafeWarningLine.enabled = false;
            }
        }

        protected override void OnBossActivated()
        {
            attackState = HelicopterBossState.Waiting;
            stateTimer = 0f;
            openingAttackUsed = false;
            fanVolleysFired = 0;
            minesDeployed = 0;
            mineDeployTimer = 0f;

            SetWarningLineVisible(false);
            BuildRuntimeAttackBag();

            if (minePool != null)
            {
                minePool.DespawnAll();
            }
        }

        protected override void OnFightStarted()
        {
            homePosition = transform.position;
            BeginNextAttack();
        }

        protected override void TickFight(float deltaTime)
        {
            stateTimer = stateTimer + deltaTime;

            if (attackState == HelicopterBossState.FanTelegraph)
            {
                UpdateFanTelegraph();
            }
            else if (attackState == HelicopterBossState.FanVolleyGap)
            {
                UpdateFanVolleyGap();
            }
            else if (attackState == HelicopterBossState.MineTelegraph)
            {
                UpdateMineTelegraph();
            }
            else if (attackState == HelicopterBossState.MineDeploying)
            {
                UpdateMineDeploying(deltaTime);
            }
            else if (attackState == HelicopterBossState.StrafeTracking)
            {
                UpdateStrafeTracking(deltaTime);
            }
            else if (attackState == HelicopterBossState.StrafeLocked)
            {
                UpdateStrafeLocked();
            }
            else if (attackState == HelicopterBossState.StrafeDashing)
            {
                UpdateStrafeDash(deltaTime);
            }
            else if (attackState == HelicopterBossState.StrafeReturning)
            {
                UpdateStrafeReturn(deltaTime);
            }
            else if (attackState == HelicopterBossState.Recover)
            {
                UpdateRecover();
            }
        }

        protected override void OnDefeatStarted()
        {
            SetWarningLineVisible(false);

            if (minePool != null)
            {
                minePool.DespawnAll();
            }
        }

        protected override Color GetFightColor()
        {
            if (attackState == HelicopterBossState.FanTelegraph
                || attackState == HelicopterBossState.FanVolleyGap)
            {
                return FlashBetween(NormalColor, fanWarningColor, flashSpeed);
            }

            if (attackState == HelicopterBossState.MineTelegraph
                || attackState == HelicopterBossState.MineDeploying)
            {
                return FlashBetween(NormalColor, mineWarningColor, flashSpeed);
            }

            if (attackState == HelicopterBossState.StrafeTracking
                || attackState == HelicopterBossState.StrafeLocked)
            {
                return FlashBetween(NormalColor, strafeWarningColor, flashSpeed);
            }

            if (attackState == HelicopterBossState.StrafeDashing)
            {
                return strafeWarningColor;
            }

            return NormalColor;
        }

        private void BeginNextAttack()
        {
            HelicopterBossAttack nextAttack;

            if (openingAttackUsed == false)
            {
                openingAttackUsed = true;
                nextAttack = HelicopterBossAttack.FanVolley;
            }
            else
            {
                nextAttack = TakeNextBagAttack();
            }

            if (nextAttack == HelicopterBossAttack.FanVolley)
            {
                BeginFanAttack();
                return;
            }

            if (nextAttack == HelicopterBossAttack.AerialMines)
            {
                BeginMineAttack();
                return;
            }

            BeginStrafeAttack();
        }

        private void BeginFanAttack()
        {
            fanVolleysFired = 0;
            ChangeAttackState(HelicopterBossState.FanTelegraph);
        }

        private void UpdateFanTelegraph()
        {
            if (stateTimer < fanTelegraphDuration)
            {
                return;
            }

            FireFanVolley();
            fanVolleysFired = fanVolleysFired + 1;

            if (fanVolleysFired >= fanVolleyCount)
            {
                ChangeAttackState(HelicopterBossState.Recover);
                return;
            }

            ChangeAttackState(HelicopterBossState.FanVolleyGap);
        }

        private void UpdateFanVolleyGap()
        {
            if (stateTimer < fanVolleyGap)
            {
                return;
            }

            FireFanVolley();
            fanVolleysFired = fanVolleysFired + 1;

            if (fanVolleysFired >= fanVolleyCount)
            {
                ChangeAttackState(HelicopterBossState.Recover);
                return;
            }

            ChangeAttackState(HelicopterBossState.FanVolleyGap);
        }

        private void FireFanVolley()
        {
            if (NormalBulletPool == null)
            {
                return;
            }

            Vector3 muzzlePosition = GetPointPosition(gunMuzzle);
            Vector2 baseDirection = GetTargetPosition(
                muzzlePosition,
                minAimY,
                maxAimY) - muzzlePosition;

            if (baseDirection.sqrMagnitude < 0.0001f)
            {
                baseDirection = Vector2.left;
            }

            baseDirection.Normalize();
            int safeBulletCount = Mathf.Max(1, fanBulletsPerVolley);
            float middleIndex = (safeBulletCount - 1) * 0.5f;

            for (int i = 0; i < safeBulletCount; i++)
            {
                Bullet bullet = NormalBulletPool.GetBullet();

                if (bullet == null)
                {
                    continue;
                }

                float angle = (i - middleIndex) * fanAngleSpacing;
                Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                Vector2 direction = rotation * baseDirection;
                bullet.Launch(muzzlePosition, direction, fanBulletSpeed);
                AudioSignals.Play(GameSound.EnemyShot);
            }
        }

        private void BeginMineAttack()
        {
            minesDeployed = 0;
            mineDeployTimer = 0f;
            ChangeAttackState(HelicopterBossState.MineTelegraph);
        }

        private void UpdateMineTelegraph()
        {
            if (stateTimer < mineTelegraphDuration)
            {
                return;
            }

            DeployNextMine();

            if (minesDeployed >= mineCount)
            {
                ChangeAttackState(HelicopterBossState.Recover);
                return;
            }

            ChangeAttackState(HelicopterBossState.MineDeploying);
        }

        private void UpdateMineDeploying(float deltaTime)
        {
            mineDeployTimer = mineDeployTimer + deltaTime;

            if (mineDeployTimer < mineDeployInterval)
            {
                return;
            }

            mineDeployTimer = 0f;
            DeployNextMine();

            if (minesDeployed >= mineCount)
            {
                ChangeAttackState(HelicopterBossState.Recover);
            }
        }

        private void DeployNextMine()
        {
            int safeMineCount = Mathf.Max(1, mineCount);
            float progress = (minesDeployed + 0.5f) / safeMineCount;
            float targetY = Mathf.Lerp(mineMinY, mineMaxY, progress);
            Vector3 destination = new Vector3(mineFieldX, targetY, 0f);

            if (minePool != null)
            {
                AerialMine mine = minePool.GetMine();

                if (mine != null)
                {
                    mine.Deploy(
                        GetPointPosition(mineDropPoint),
                        destination,
                        mineTravelDuration,
                        mineArmedDuration);
                }
            }

            minesDeployed = minesDeployed + 1;
        }

        private void BeginStrafeAttack()
        {
            lockedStrafeY = transform.position.y;
            SetWarningLineVisible(true);
            UpdateWarningLine(transform.position.y);
            ChangeAttackState(HelicopterBossState.StrafeTracking);
        }

        private void UpdateStrafeTracking(float deltaTime)
        {
            float targetY = transform.position.y;

            if (Target != null)
            {
                targetY = Mathf.Clamp(Target.position.y, minAimY, maxAimY);
            }

            float alignedY = Mathf.MoveTowards(
                transform.position.y,
                targetY,
                strafeAlignmentSpeed * deltaTime);

            SetY(alignedY);
            UpdateWarningLine(alignedY);

            if (stateTimer < strafeTrackingDuration)
            {
                return;
            }

            lockedStrafeY = alignedY;
            SetY(lockedStrafeY);
            UpdateWarningLine(lockedStrafeY);
            ChangeAttackState(HelicopterBossState.StrafeLocked);
        }

        private void UpdateStrafeLocked()
        {
            SetY(lockedStrafeY);

            if (stateTimer < strafeLockedWarningDuration)
            {
                return;
            }

            SetWarningLineVisible(false);
            ChangeAttackState(HelicopterBossState.StrafeDashing);
        }

        private void UpdateStrafeDash(float deltaTime)
        {
            Vector2 step = Vector2.left * strafeSpeed * deltaTime;
            CheckStrafeHit(step);
            MoveBy(step.x, step.y);

            if (transform.position.x > strafeExitX)
            {
                return;
            }

            transform.position = new Vector3(strafeReturnX, homePosition.y, homePosition.z);
            ChangeAttackState(HelicopterBossState.StrafeReturning);
        }

        private void CheckStrafeHit(Vector2 step)
        {
            if (step.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector2 castOrigin = (Vector2)transform.position + strafeHitboxOffset;
            RaycastHit2D hit = Physics2D.BoxCast(
                castOrigin,
                strafeHitboxSize,
                0f,
                step.normalized,
                step.magnitude,
                playerLayers);

            if (hit.collider == null)
            {
                return;
            }

            PlaneCollision plane = hit.collider.GetComponentInParent<PlaneCollision>();

            if (plane != null)
            {
                plane.TakeHit();
            }
        }

        private void UpdateStrafeReturn(float deltaTime)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                homePosition,
                strafeReturnSpeed * deltaTime);

            if (Vector3.Distance(transform.position, homePosition) > 0.01f)
            {
                return;
            }

            transform.position = homePosition;
            ChangeAttackState(HelicopterBossState.Recover);
        }

        private void UpdateRecover()
        {
            if (stateTimer < recoverDuration)
            {
                return;
            }

            BeginNextAttack();
        }

        private HelicopterBossAttack TakeNextBagAttack()
        {
            if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
            {
                return HelicopterBossAttack.FanVolley;
            }

            if (attackBagIndex >= runtimeAttackBag.Length)
            {
                ShuffleRuntimeAttackBag();
            }

            HelicopterBossAttack result = runtimeAttackBag[attackBagIndex];
            attackBagIndex = attackBagIndex + 1;
            return result;
        }

        private void BuildRuntimeAttackBag()
        {
            if (attackBag == null || attackBag.Length == 0)
            {
                runtimeAttackBag = new HelicopterBossAttack[]
                {
                    HelicopterBossAttack.FanVolley,
                    HelicopterBossAttack.FanVolley,
                    HelicopterBossAttack.AerialMines,
                    HelicopterBossAttack.AerialMines,
                    HelicopterBossAttack.StrafeCharge
                };
            }
            else
            {
                runtimeAttackBag = new HelicopterBossAttack[attackBag.Length];

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
                HelicopterBossAttack temporary = runtimeAttackBag[i];
                runtimeAttackBag[i] = runtimeAttackBag[swapIndex];
                runtimeAttackBag[swapIndex] = temporary;
            }

            attackBagIndex = 0;
        }

        private void SetWarningLineVisible(bool isVisible)
        {
            if (strafeWarningLine == null)
            {
                return;
            }

            strafeWarningLine.enabled = isVisible;
        }

        private void UpdateWarningLine(float y)
        {
            if (strafeWarningLine == null)
            {
                return;
            }

            strafeWarningLine.SetPosition(0, new Vector3(warningLineLeftX, y, 0f));
            strafeWarningLine.SetPosition(1, new Vector3(warningLineRightX, y, 0f));
        }

        private void ChangeAttackState(HelicopterBossState nextState)
        {
            if (nextState == HelicopterBossState.FanTelegraph ||
                nextState == HelicopterBossState.MineTelegraph ||
                nextState == HelicopterBossState.StrafeLocked)
            {
                AudioSignals.Play(GameSound.EnemyWarning);
            }

            attackState = nextState;
            stateTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 center = transform.position + (Vector3)strafeHitboxOffset;
            Gizmos.DrawWireCube(center, strafeHitboxSize);
        }
    }
}
