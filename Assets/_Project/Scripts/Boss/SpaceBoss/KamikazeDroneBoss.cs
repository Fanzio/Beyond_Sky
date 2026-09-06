using UnityEngine;

public enum KamikazeDroneAttack
{
    AerialMines,
    Reinforcements
}

public enum KamikazeDroneState
{
    Waiting,
    MineTelegraph,
    MineDeploying,
    ReinforcementTelegraph,
    Recover
}

// Space boss with exactly two abilities: scrolling mines and sequential reinforcements.
public class KamikazeDroneBoss : BossBase
{
    [Header("Visual warnings")]
    [SerializeField] private Color mineWarningColor = Color.red;
    [SerializeField] private Color reinforcementWarningColor = new Color(0.35f, 1f, 0.45f);
    [SerializeField] private float flashSpeed = 12f;

    [Header("Attack order")]
    [SerializeField]
    private KamikazeDroneAttack[] attackBag = new KamikazeDroneAttack[]
    {
        KamikazeDroneAttack.AerialMines,
        KamikazeDroneAttack.AerialMines,
        KamikazeDroneAttack.Reinforcements
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

    private KamikazeDroneState attackState = KamikazeDroneState.Waiting;
    private KamikazeDroneAttack[] runtimeAttackBag;
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
            Debug.LogWarning("KamikazeDroneBoss: Mine Pool non assegnata.", this);
        }

        if (enemySpawner == null || reinforcementPrefab == null)
        {
            Debug.LogWarning("KamikazeDroneBoss: riferimenti dei rinforzi non assegnati.", this);
        }
    }

    protected override void OnBossActivated()
    {
        attackState = KamikazeDroneState.Waiting;
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

        if (attackState == KamikazeDroneState.MineTelegraph)
        {
            UpdateMineTelegraph();
        }
        else if (attackState == KamikazeDroneState.MineDeploying)
        {
            UpdateMineDeploying(deltaTime);
        }
        else if (attackState == KamikazeDroneState.ReinforcementTelegraph)
        {
            UpdateReinforcementTelegraph();
        }
        else if (attackState == KamikazeDroneState.Recover)
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
        if (attackState == KamikazeDroneState.MineTelegraph
            || attackState == KamikazeDroneState.MineDeploying)
        {
            return FlashBetween(NormalColor, mineWarningColor, flashSpeed);
        }

        if (attackState == KamikazeDroneState.ReinforcementTelegraph)
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
        KamikazeDroneAttack nextAttack;

        if (openingMineUsed == false)
        {
            openingMineUsed = true;
            nextAttack = KamikazeDroneAttack.AerialMines;
        }
        else
        {
            nextAttack = TakeNextBagAttack();
        }

        if (nextAttack == KamikazeDroneAttack.Reinforcements
            && reinforcementSequence.IsActive)
        {
            nextAttack = KamikazeDroneAttack.AerialMines;
        }

        if (nextAttack == KamikazeDroneAttack.AerialMines)
        {
            BeginMineAttack();
            return;
        }

        ChangeAttackState(KamikazeDroneState.ReinforcementTelegraph);
    }

    private void BeginMineAttack()
    {
        minesDeployed = 0;
        mineDeployTimer = 0f;
        hasPreviousMineTarget = false;
        ChangeAttackState(KamikazeDroneState.MineTelegraph);
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
            ChangeAttackState(KamikazeDroneState.Recover);
            return;
        }

        ChangeAttackState(KamikazeDroneState.MineDeploying);
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
            ChangeAttackState(KamikazeDroneState.Recover);
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
                mine.Deploy(GetMineDropPosition(), destination, mineTravelDuration);
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
        ChangeAttackState(KamikazeDroneState.Recover);
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

    private KamikazeDroneAttack TakeNextBagAttack()
    {
        if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
        {
            return KamikazeDroneAttack.AerialMines;
        }

        if (attackBagIndex >= runtimeAttackBag.Length)
        {
            ShuffleRuntimeAttackBag();
        }

        KamikazeDroneAttack result = runtimeAttackBag[attackBagIndex];
        attackBagIndex = attackBagIndex + 1;
        return result;
    }

    private void BuildRuntimeAttackBag()
    {
        if (attackBag == null || attackBag.Length == 0)
        {
            runtimeAttackBag = new KamikazeDroneAttack[]
            {
                KamikazeDroneAttack.AerialMines,
                KamikazeDroneAttack.AerialMines,
                KamikazeDroneAttack.Reinforcements
            };
        }
        else
        {
            runtimeAttackBag = new KamikazeDroneAttack[attackBag.Length];

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
            KamikazeDroneAttack temporary = runtimeAttackBag[i];
            runtimeAttackBag[i] = runtimeAttackBag[swapIndex];
            runtimeAttackBag[swapIndex] = temporary;
        }

        attackBagIndex = 0;
    }

    private Vector3 GetMineDropPosition()
    {
        if (mineDropPoint != null)
        {
            return mineDropPoint.position;
        }

        return transform.position;
    }

    private void ChangeAttackState(KamikazeDroneState nextState)
    {
        attackState = nextState;
        stateTimer = 0f;
    }
}
