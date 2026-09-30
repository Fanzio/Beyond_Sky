// Gestisce cariche, mine e movimento del boss astronave.
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.Enemies;

namespace BeyondSky.Boss
{
    public enum SpaceShipAttack
    {
        AerialMines,
        Reinforcements
    }

    public enum SpaceShipState
    {
        Waiting,
        MineTelegraph,
        MineDeploying,
        ReinforcementTelegraph,
        Recover
    }

    public class SpaceShipBoss : BossBase
    {
        [Header("Visual warnings")]
        [SerializeField] private Color mineWarningColor = Color.red;
        [SerializeField] private Color reinforcementWarningColor = new Color(0.35f, 1f, 0.45f);
        [SerializeField] private float flashSpeed = 12f;

        [Header("Attack order")]
        [SerializeField]
        private SpaceShipAttack[] attackBag = new SpaceShipAttack[]
        {
            SpaceShipAttack.AerialMines,
            SpaceShipAttack.AerialMines,
            SpaceShipAttack.Reinforcements
        };

        [Header("Continuous vertical movement")]
        [SerializeField] private float movementMinY = 12f;
        [SerializeField] private float movementMaxY = 28f;
        [SerializeField] private float verticalMovementSpeed = 5f;

        [Header("Aerial mines")]
        [SerializeField] private AerialMinePool minePool;
        [SerializeField] private Transform mineDropPoint;
        [SerializeField] private float mineTelegraphDuration = 0.7f;
        [SerializeField] private int mineCount = 2;
        [SerializeField] private float mineDeployInterval = 0.15f;
        [SerializeField] private float mineTravelDuration = 0.55f;
        [SerializeField] private float mineFieldX = 0f;
        [SerializeField] private float mineMinY = 12f;
        [SerializeField] private float mineMaxY = 28f;
        [SerializeField] private float minimumMineYSeparation = 4f;

        [Header("Reinforcements")]
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private EnemyBase reinforcementPrefab;
        [SerializeField] private int reinforcementCount = 4;
        [SerializeField] private float reinforcementTelegraphDuration = 0.8f;
        [SerializeField] private float reinforcementInterval = 0.35f;
        [SerializeField] private float reinforcementSpawnX = 26f;
        [SerializeField] private float reinforcementMinY = 13f;
        [SerializeField] private float reinforcementMaxY = 27f;

        [Header("Recover")]
        [SerializeField] private float recoverDuration = 0.7f;

        private SpaceShipState attackState = SpaceShipState.Waiting;
        private SpaceShipAttack[] runtimeAttackBag;
        private int attackBagIndex;
        private bool openingMineUsed;
        private float stateTimer;

        private int minesDeployed;
        private float mineDeployTimer;
        private float verticalDirection = 1f;
        private float previousMineTargetY;
        private bool hasPreviousMineTarget;

        private BossReinforcementSequence reinforcementSequence =
            new BossReinforcementSequence();

        protected override void OnSetupComplete()
        {
            if (minePool == null)
            {
                Debug.LogWarning("SpaceShipBoss: Mine Pool non assegnata.", this);
            }

            if (enemySpawner == null || reinforcementPrefab == null)
            {
                Debug.LogWarning("SpaceShipBoss: riferimenti dei rinforzi non assegnati.", this);
            }
        }

        protected override void OnBossActivated()
        {
            attackState = SpaceShipState.Waiting;
            stateTimer = 0f;
            openingMineUsed = false;
            minesDeployed = 0;
            mineDeployTimer = 0f;
            hasPreviousMineTarget = false;
            reinforcementSequence.Stop();

            BuildRuntimeAttackBag();

            if (minePool != null)
            {
                minePool.DespawnAll();
            }
        }

        protected override void OnFightStarted()
        {
            if (Random.value < 0.5f)
            {
                verticalDirection = -1f;
            }
            else
            {
                verticalDirection = 1f;
            }

            BeginNextAttack();
        }

        protected override void TickFight(float deltaTime)
        {
            UpdateVerticalMovement(deltaTime);
            reinforcementSequence.Tick(deltaTime);
            stateTimer = stateTimer + deltaTime;

            if (attackState == SpaceShipState.MineTelegraph)
            {
                UpdateMineTelegraph();
            }
            else if (attackState == SpaceShipState.MineDeploying)
            {
                UpdateMineDeploying(deltaTime);
            }
            else if (attackState == SpaceShipState.ReinforcementTelegraph)
            {
                UpdateReinforcementTelegraph();
            }
            else if (attackState == SpaceShipState.Recover)
            {
                UpdateRecover();
            }
        }

        protected override void OnDefeatStarted()
        {
            reinforcementSequence.Stop();

            if (minePool != null)
            {
                minePool.DespawnAll();
            }
        }

        protected override Color GetFightColor()
        {
            if (attackState == SpaceShipState.MineTelegraph
                || attackState == SpaceShipState.MineDeploying)
            {
                return FlashBetween(NormalColor, mineWarningColor, flashSpeed);
            }

            if (attackState == SpaceShipState.ReinforcementTelegraph)
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
            SpaceShipAttack nextAttack;

            if (openingMineUsed == false)
            {
                openingMineUsed = true;
                nextAttack = SpaceShipAttack.AerialMines;
            }
            else
            {
                nextAttack = TakeNextBagAttack();
            }

            if (nextAttack == SpaceShipAttack.Reinforcements
                && reinforcementSequence.IsActive)
            {
                nextAttack = SpaceShipAttack.AerialMines;
            }

            if (nextAttack == SpaceShipAttack.AerialMines)
            {
                BeginMineAttack();
                return;
            }

            ChangeAttackState(SpaceShipState.ReinforcementTelegraph);
        }

        private void BeginMineAttack()
        {
            minesDeployed = 0;
            mineDeployTimer = 0f;
            hasPreviousMineTarget = false;
            ChangeAttackState(SpaceShipState.MineTelegraph);
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
                ChangeAttackState(SpaceShipState.Recover);
                return;
            }

            ChangeAttackState(SpaceShipState.MineDeploying);
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
                ChangeAttackState(SpaceShipState.Recover);
            }
        }

        private void DeployNextMine()
        {
            float targetY = PickRandomMineY();
            Vector3 destination = new Vector3(mineFieldX, targetY, 0f);

            if (minePool != null)
            {
                AerialMine mine = minePool.GetMine();

                if (mine != null)
                {
                    mine.Deploy(GetPointPosition(mineDropPoint), destination, mineTravelDuration);
                }
            }

            minesDeployed = minesDeployed + 1;
        }

        private float PickRandomMineY()
        {
            float lowerLimit = Mathf.Min(mineMinY, mineMaxY);
            float upperLimit = Mathf.Max(mineMinY, mineMaxY);
            float selectedY = Random.Range(lowerLimit, upperLimit);

            if (hasPreviousMineTarget)
            {
                float availableRange = upperLimit - lowerLimit;
                float safeSeparation = Mathf.Clamp(
                    minimumMineYSeparation,
                    0f,
                    availableRange);

                for (int attempt = 0; attempt < 8; attempt++)
                {
                    if (Mathf.Abs(selectedY - previousMineTargetY) >= safeSeparation)
                    {
                        break;
                    }

                    selectedY = Random.Range(lowerLimit, upperLimit);
                }

                if (Mathf.Abs(selectedY - previousMineTargetY) < safeSeparation)
                {
                    float distanceFromLower = Mathf.Abs(previousMineTargetY - lowerLimit);
                    float distanceFromUpper = Mathf.Abs(upperLimit - previousMineTargetY);

                    if (distanceFromLower > distanceFromUpper)
                    {
                        selectedY = lowerLimit;
                    }
                    else
                    {
                        selectedY = upperLimit;
                    }
                }
            }

            previousMineTargetY = selectedY;
            hasPreviousMineTarget = true;
            return selectedY;
        }

        private void UpdateVerticalMovement(float deltaTime)
        {
            float lowerLimit = Mathf.Min(movementMinY, movementMaxY);
            float upperLimit = Mathf.Max(movementMinY, movementMaxY);
            float safeMovementSpeed = Mathf.Max(0f, verticalMovementSpeed);
            float nextY = transform.position.y
                + verticalDirection * safeMovementSpeed * deltaTime;

            if (nextY >= upperLimit)
            {
                nextY = upperLimit;
                verticalDirection = -1f;
            }
            else if (nextY <= lowerLimit)
            {
                nextY = lowerLimit;
                verticalDirection = 1f;
            }

            SetY(nextY);
        }

        private void UpdateReinforcementTelegraph()
        {
            if (stateTimer < reinforcementTelegraphDuration)
            {
                return;
            }

            StartReinforcementSequence();
            ChangeAttackState(SpaceShipState.Recover);
        }

        private void StartReinforcementSequence()
        {
            reinforcementSequence.BeginSingle(
                enemySpawner,
                reinforcementPrefab,
                reinforcementCount,
                reinforcementInterval,
                reinforcementSpawnX,
                reinforcementMinY,
                reinforcementMaxY);
        }

        private void UpdateRecover()
        {
            if (stateTimer < recoverDuration)
            {
                return;
            }

            BeginNextAttack();
        }

        private SpaceShipAttack TakeNextBagAttack()
        {
            if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
            {
                return SpaceShipAttack.AerialMines;
            }

            if (attackBagIndex >= runtimeAttackBag.Length)
            {
                ShuffleRuntimeAttackBag();
            }

            SpaceShipAttack result = runtimeAttackBag[attackBagIndex];
            attackBagIndex = attackBagIndex + 1;
            return result;
        }

        private void BuildRuntimeAttackBag()
        {
            if (attackBag == null || attackBag.Length == 0)
            {
                runtimeAttackBag = new SpaceShipAttack[]
                {
                    SpaceShipAttack.AerialMines,
                    SpaceShipAttack.AerialMines,
                    SpaceShipAttack.Reinforcements
                };
            }
            else
            {
                runtimeAttackBag = new SpaceShipAttack[attackBag.Length];

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
                SpaceShipAttack temporary = runtimeAttackBag[i];
                runtimeAttackBag[i] = runtimeAttackBag[swapIndex];
                runtimeAttackBag[swapIndex] = temporary;
            }

            attackBagIndex = 0;
        }

        private void ChangeAttackState(SpaceShipState nextState)
        {
            if (nextState == SpaceShipState.MineTelegraph ||
                nextState == SpaceShipState.ReinforcementTelegraph)
            {
                AudioSignals.Play(GameSound.EnemyWarning);
            }

            attackState = nextState;
            stateTimer = 0f;
        }
    }
}
