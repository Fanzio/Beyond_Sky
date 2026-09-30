// Esegue un attacco casuale dell’elicottero e poi lo fa uscire di scena.
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.Boss;
using BeyondSky.Player;

namespace BeyondSky.Enemies
{
    public enum StandardHelicopterAttack
    {
        FanVolley,
        AerialMines,
        StrafeCharge
    }

    public enum StandardHelicopterState
    {
        Entering,
        FanTelegraph,
        FanGap,
        MineTelegraph,
        MineGap,
        StrafeTracking,
        StrafeLocked,
        StrafeDashing,
        PostAttack,
        Leaving
    }

    public class SingleAttackHelicopterEnemy : EnemyBase
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color fanWarningColor = Color.yellow;
        [SerializeField] private Color mineWarningColor = new Color(1f, 0.25f, 0.85f);
        [SerializeField] private Color strafeWarningColor = Color.red;
        [SerializeField] private float flashSpeed = 12f;

        [Header("References")]
        [SerializeField] private Transform gunMuzzle;
        [SerializeField] private Transform mineDropPoint;
        [SerializeField] private LineRenderer strafeWarningLine;

        [Header("Movement")]
        [SerializeField] private float enterSpeed = 24f;
        [SerializeField] private float holdX = 13f;
        [SerializeField] private float leaveSpeed = 28f;
        [SerializeField] private float leaveX = 30f;
        [SerializeField] private float postAttackDuration = 0.35f;

        [Header("Random entry")]
        [SerializeField] private float randomEntryMinY = 12f;
        [SerializeField] private float randomEntryMaxY = 28f;
        [SerializeField] private float minimumYDistanceFromPlayer = 4f;

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
        [SerializeField] private Vector2 strafeHitboxSize = new Vector2(5.5f, 1.5f);
        [SerializeField] private Vector2 strafeHitboxOffset = Vector2.zero;
        [SerializeField] private float warningLineLeftX = -16f;
        [SerializeField] private float warningLineRightX = 16f;

        private StandardHelicopterAttack chosenAttack;
        private StandardHelicopterState state;
        private float stateTimer;
        private int fanVolleysFired;
        private int minesDeployed;
        private float mineDeployTimer;
        private float lockedStrafeY;

        public override void Setup(Transform playerTarget, BulletPool pool)
        {
            base.Setup(playerTarget, pool);

            if (strafeWarningLine != null)
            {
                strafeWarningLine.useWorldSpace = true;
                strafeWarningLine.positionCount = 2;
                strafeWarningLine.enabled = false;
            }
        }

        public override void Activate(Vector3 startPosition)
        {
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            SetY(PickRandomYAwayFromTarget(
                randomEntryMinY,
                randomEntryMaxY,
                minimumYDistanceFromPlayer));
            chosenAttack = (StandardHelicopterAttack)Random.Range(0, 3);
            fanVolleysFired = 0;
            minesDeployed = 0;
            mineDeployTimer = 0f;
            SetBodyColor(normalColor);
            SetWarningLineVisible(false);
            ChangeState(StandardHelicopterState.Entering);
            gameObject.SetActive(true);
        }

        public override void TakeBulletHit()
        {
            AudioSignals.Play(GameSound.EnemyDestroyed);
            DeactivateEnemy();
        }

        private void OnDisable()
        {
            SetWarningLineVisible(false);
            SetBodyColor(normalColor);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            stateTimer = stateTimer + deltaTime;

            if (state == StandardHelicopterState.Entering)
            {
                UpdateEntering(deltaTime);
            }
            else if (state == StandardHelicopterState.FanTelegraph)
            {
                UpdateFanTelegraph();
            }
            else if (state == StandardHelicopterState.FanGap)
            {
                UpdateFanGap();
            }
            else if (state == StandardHelicopterState.MineTelegraph)
            {
                UpdateMineTelegraph();
            }
            else if (state == StandardHelicopterState.MineGap)
            {
                UpdateMineGap(deltaTime);
            }
            else if (state == StandardHelicopterState.StrafeTracking)
            {
                UpdateStrafeTracking(deltaTime);
            }
            else if (state == StandardHelicopterState.StrafeLocked)
            {
                UpdateStrafeLocked();
            }
            else if (state == StandardHelicopterState.StrafeDashing)
            {
                UpdateStrafeDashing(deltaTime);
            }
            else if (state == StandardHelicopterState.PostAttack)
            {
                UpdatePostAttack();
            }
            else if (state == StandardHelicopterState.Leaving)
            {
                UpdateLeaving(deltaTime);
            }

            UpdateBodyWarning();
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
            BeginChosenAttack();
        }

        private void BeginChosenAttack()
        {
            if (chosenAttack == StandardHelicopterAttack.FanVolley)
            {
                fanVolleysFired = 0;
                ChangeState(StandardHelicopterState.FanTelegraph);
                return;
            }

            if (chosenAttack == StandardHelicopterAttack.AerialMines)
            {
                minesDeployed = 0;
                mineDeployTimer = 0f;
                ChangeState(StandardHelicopterState.MineTelegraph);
                return;
            }

            lockedStrafeY = transform.position.y;
            SetWarningLineVisible(true);
            UpdateWarningLine(transform.position.y);
            ChangeState(StandardHelicopterState.StrafeTracking);
        }

        private void UpdateFanTelegraph()
        {
            if (stateTimer < fanTelegraphDuration)
            {
                return;
            }

            FireFanVolley();
            fanVolleysFired = fanVolleysFired + 1;

            if (fanVolleysFired >= Mathf.Max(1, fanVolleyCount))
            {
                ChangeState(StandardHelicopterState.PostAttack);
                return;
            }

            ChangeState(StandardHelicopterState.FanGap);
        }

        private void UpdateFanGap()
        {
            if (stateTimer < fanVolleyGap)
            {
                return;
            }

            FireFanVolley();
            fanVolleysFired = fanVolleysFired + 1;

            if (fanVolleysFired >= Mathf.Max(1, fanVolleyCount))
            {
                ChangeState(StandardHelicopterState.PostAttack);
                return;
            }

            ChangeState(StandardHelicopterState.FanGap);
        }

        private void FireFanVolley()
        {
            if (NormalBulletPool == null)
            {
                return;
            }

            Vector3 muzzlePosition = GetPointPosition(gunMuzzle);
            Vector2 baseDirection = GetTargetPosition(muzzlePosition, 10f) - muzzlePosition;

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

        private void UpdateMineTelegraph()
        {
            if (stateTimer < mineTelegraphDuration)
            {
                return;
            }

            DeployNextMine();

            if (minesDeployed >= Mathf.Max(1, mineCount))
            {
                ChangeState(StandardHelicopterState.PostAttack);
                return;
            }

            mineDeployTimer = 0f;
            ChangeState(StandardHelicopterState.MineGap);
        }

        private void UpdateMineGap(float deltaTime)
        {
            mineDeployTimer = mineDeployTimer + deltaTime;

            if (mineDeployTimer < mineDeployInterval)
            {
                return;
            }

            mineDeployTimer = 0f;
            DeployNextMine();

            if (minesDeployed >= Mathf.Max(1, mineCount))
            {
                ChangeState(StandardHelicopterState.PostAttack);
            }
        }

        private void DeployNextMine()
        {
            int safeMineCount = Mathf.Max(1, mineCount);
            float progress = (minesDeployed + 0.5f) / safeMineCount;
            float targetY = Mathf.Lerp(mineMinY, mineMaxY, progress);
            Vector3 destination = new Vector3(mineFieldX, targetY, 0f);

            if (MinePool != null)
            {
                AerialMine mine = MinePool.GetMine();

                if (mine != null)
                {
                    mine.Deploy(
                        GetPointPosition(mineDropPoint),
                        destination,
                        mineTravelDuration,
                        mineArmedDuration);
                }
            }
            else
            {
                Debug.LogWarning(
                    "SingleAttackHelicopterEnemy: Aerial Mine Pool non assegnata nel roster.",
                    this);
            }

            minesDeployed = minesDeployed + 1;
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
            ChangeState(StandardHelicopterState.StrafeLocked);
        }

        private void UpdateStrafeLocked()
        {
            SetY(lockedStrafeY);

            if (stateTimer < strafeLockedWarningDuration)
            {
                return;
            }

            SetWarningLineVisible(false);
            ChangeState(StandardHelicopterState.StrafeDashing);
        }

        private void UpdateStrafeDashing(float deltaTime)
        {
            Vector2 step = Vector2.left * strafeSpeed * deltaTime;
            CheckStrafeHit(step);
            MoveBy(step.x, step.y);

            if (transform.position.x > strafeExitX)
            {
                return;
            }

            DeactivateEnemy();
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

        private void UpdatePostAttack()
        {
            if (stateTimer < postAttackDuration)
            {
                return;
            }

            ChangeState(StandardHelicopterState.Leaving);
        }

        private void UpdateLeaving(float deltaTime)
        {
            MoveBy(leaveSpeed * deltaTime, leaveSpeed * 0.15f * deltaTime);

            if (transform.position.x < leaveX)
            {
                return;
            }

            DeactivateEnemy();
        }

        private void UpdateBodyWarning()
        {
            if (state == StandardHelicopterState.FanTelegraph
                || state == StandardHelicopterState.FanGap)
            {
                SetFlashingColor(fanWarningColor);
                return;
            }

            if (state == StandardHelicopterState.MineTelegraph
                || state == StandardHelicopterState.MineGap)
            {
                SetFlashingColor(mineWarningColor);
                return;
            }

            if (state == StandardHelicopterState.StrafeTracking
                || state == StandardHelicopterState.StrafeLocked)
            {
                SetFlashingColor(strafeWarningColor);
                return;
            }

            if (state == StandardHelicopterState.StrafeDashing)
            {
                SetBodyColor(strafeWarningColor);
                return;
            }

            SetBodyColor(normalColor);
        }

        private void SetFlashingColor(Color warningColor)
        {
            SetBodyColor(FlashBetween(normalColor, warningColor, flashSpeed));
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

        private void SetWarningLineVisible(bool isVisible)
        {
            if (strafeWarningLine != null)
            {
                strafeWarningLine.enabled = isVisible;
            }
        }

        private void ChangeState(StandardHelicopterState nextState)
        {
            if (nextState == StandardHelicopterState.FanTelegraph ||
                nextState == StandardHelicopterState.MineTelegraph ||
                nextState == StandardHelicopterState.StrafeLocked)
            {
                AudioSignals.Play(GameSound.EnemyWarning);
            }

            state = nextState;
            stateTimer = 0f;
        }

        private void SetBodyColor(Color color)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = color;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector2 center = (Vector2)transform.position + strafeHitboxOffset;
            Gizmos.DrawWireCube(center, strafeHitboxSize);
        }
    }
}
