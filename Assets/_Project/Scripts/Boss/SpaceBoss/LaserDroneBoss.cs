using UnityEngine;

public enum LaserDroneAttack
{
    IntermittentTrackingLaser,
    HeavyLaser,
    ZoneDrones
}

public enum LaserDroneBossState
{
    Waiting,
    TrackingLaser,
    HeavyAlign,
    HeavyTelegraph,
    HeavyFiring,
    ZoneTelegraph,
    Recover
}

// Space boss with an intermittent tracking beam, a locked heavy beam and two zone drones.
public class LaserDroneBoss : BossBase
{
    [Header("Visual warnings")]
    [SerializeField] private Color trackingWarningColor = new Color(1f, 0.45f, 0.15f);
    [SerializeField] private Color trackingActiveColor = Color.red;
    [SerializeField] private Color heavyWarningColor = new Color(1f, 0.1f, 0.1f);
    [SerializeField] private Color zoneWarningColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private float flashSpeed = 12f;

    [Header("Attack bag")]
    [SerializeField]
    private LaserDroneAttack[] attackBag = new LaserDroneAttack[]
    {
        LaserDroneAttack.HeavyLaser,
        LaserDroneAttack.HeavyLaser,
        LaserDroneAttack.ZoneDrones
    };

    [Header("Band limits")]
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    [Header("Intermittent tracking laser")]
    [SerializeField] private LaserBeamHazard trackingBeam;
    [SerializeField] private float trackingTotalDuration = 12f;
    [SerializeField] private float trackingLaserOnDuration = 2f;
    [SerializeField] private float trackingLaserOffDuration = 4f;
    [SerializeField] private float trackingLaserWarningDuration = 1f;
    [SerializeField] private float trackingSpeed = 4f;

    [Header("Heavy laser")]
    [SerializeField] private LaserBeamHazard heavyBeam;
    [SerializeField] private float heavyAlignDuration = 1f;
    [SerializeField] private float heavyAlignSpeed = 6f;
    [SerializeField] private float heavyTelegraphDuration = 1f;
    [SerializeField] private float heavyFireDuration = 1.4f;

    [Header("Zone drones")]
    [SerializeField] private LaserZoneDrone upperZoneDrone;
    [SerializeField] private LaserZoneDrone lowerZoneDrone;
    [SerializeField] private float zoneTelegraphDuration = 0.8f;
    [SerializeField] private float zoneDroneActiveDuration = 5f;
    [SerializeField] private float zoneDroneSpawnX = 28f;
    [SerializeField] private float zoneDroneHoldX = 10f;
    [SerializeField] private float upperZoneY = 26f;
    [SerializeField] private float lowerZoneY = 14f;

    [Header("Recover")]
    [SerializeField] private float recoverDuration = 0.75f;

    private LaserDroneBossState attackState = LaserDroneBossState.Waiting;
    private LaserDroneAttack[] runtimeAttackBag;
    private int attackBagIndex;
    private float stateTimer;
    private float trackingCycleTimer;
    private bool trackingLaserActive;

    protected override void OnSetupComplete()
    {
        SetTrackingBeamActive(false);
        SetHeavyBeamActive(false);
        DeactivateZoneDrones();

        if (trackingBeam == null || heavyBeam == null)
        {
            Debug.LogWarning("LaserDroneBoss: uno o piu' laser non sono assegnati.", this);
        }

        if (upperZoneDrone == null || lowerZoneDrone == null)
        {
            Debug.LogWarning("LaserDroneBoss: i due droni di zona non sono assegnati.", this);
        }
    }

    protected override void OnBossActivated()
    {
        attackState = LaserDroneBossState.Waiting;
        stateTimer = 0f;
        trackingCycleTimer = 0f;
        trackingLaserActive = false;
        SetTrackingBeamActive(false);
        SetHeavyBeamActive(false);
        DeactivateZoneDrones();
        BuildRuntimeAttackBag();
    }

    protected override void OnFightStarted()
    {
        BeginNextAttack();
    }

    protected override void TickFight(float deltaTime)
    {
        stateTimer = stateTimer + deltaTime;

        if (attackState == LaserDroneBossState.TrackingLaser)
        {
            UpdateTrackingLaser(deltaTime);
        }
        else if (attackState == LaserDroneBossState.HeavyAlign)
        {
            UpdateHeavyAlign(deltaTime);
        }
        else if (attackState == LaserDroneBossState.HeavyTelegraph)
        {
            UpdateHeavyTelegraph();
        }
        else if (attackState == LaserDroneBossState.HeavyFiring)
        {
            UpdateHeavyFiring();
        }
        else if (attackState == LaserDroneBossState.ZoneTelegraph)
        {
            UpdateZoneTelegraph();
        }
        else if (attackState == LaserDroneBossState.Recover)
        {
            UpdateRecover();
        }
    }

    protected override void OnDefeatStarted()
    {
        trackingLaserActive = false;
        SetTrackingBeamActive(false);
        SetHeavyBeamActive(false);
        DeactivateZoneDrones();
    }

    protected override Color GetFightColor()
    {
        if (attackState == LaserDroneBossState.TrackingLaser)
        {
            if (trackingLaserActive)
            {
                return trackingActiveColor;
            }

            return FlashBetween(NormalColor, trackingWarningColor, flashSpeed);
        }

        if (attackState == LaserDroneBossState.HeavyTelegraph)
        {
            return FlashBetween(NormalColor, heavyWarningColor, flashSpeed);
        }

        if (attackState == LaserDroneBossState.HeavyFiring)
        {
            return heavyWarningColor;
        }

        if (attackState == LaserDroneBossState.ZoneTelegraph)
        {
            return FlashBetween(NormalColor, zoneWarningColor, flashSpeed);
        }

        return NormalColor;
    }

    private void BeginNextAttack()
    {
        LaserDroneAttack nextAttack = TakeNextBagAttack();

        if (nextAttack == LaserDroneAttack.IntermittentTrackingLaser)
        {
            BeginTrackingLaser();
            return;
        }

        if (nextAttack == LaserDroneAttack.HeavyLaser)
        {
            ChangeAttackState(LaserDroneBossState.HeavyAlign);
            return;
        }

        ChangeAttackState(LaserDroneBossState.ZoneTelegraph);
    }

    private void BeginTrackingLaser()
    {
        trackingCycleTimer = 0f;
        trackingLaserActive = false;
        SetTrackingBeamActive(false);
        SetTrackingWarningVisible(false);
        ChangeAttackState(LaserDroneBossState.TrackingLaser);
    }

    private void UpdateTrackingLaser(float deltaTime)
    {
        TrackTargetY(trackingSpeed, minY, maxY, deltaTime);
        trackingCycleTimer = trackingCycleTimer + deltaTime;

        if (trackingLaserActive)
        {
            if (trackingBeam != null)
            {
                trackingBeam.TickDamage();
            }

            if (trackingCycleTimer >= trackingLaserOnDuration)
            {
                trackingCycleTimer = 0f;
                trackingLaserActive = false;
                SetTrackingBeamActive(false);
            }
        }
        else
        {
            float safeOffDuration = Mathf.Max(0.01f, trackingLaserOffDuration);
            float safeWarningDuration = Mathf.Clamp(
                trackingLaserWarningDuration,
                0f,
                safeOffDuration);

            float warningStartTime = safeOffDuration - safeWarningDuration;

            if (trackingCycleTimer >= warningStartTime)
            {
                SetTrackingWarningVisible(true);
            }
            else
            {
                SetTrackingWarningVisible(false);
            }

            if (trackingCycleTimer >= safeOffDuration)
            {
                trackingCycleTimer = 0f;
                trackingLaserActive = true;
                SetTrackingBeamActive(true);
            }
        }

        if (stateTimer < trackingTotalDuration)
        {
            return;
        }

        trackingLaserActive = false;
        SetTrackingWarningVisible(false);
        SetTrackingBeamActive(false);
        ChangeAttackState(LaserDroneBossState.Recover);
    }

    private void UpdateHeavyAlign(float deltaTime)
    {
        TrackTargetY(heavyAlignSpeed, minY, maxY, deltaTime);

        if (stateTimer < heavyAlignDuration)
        {
            return;
        }

        SetHeavyWarningVisible(true);
        ChangeAttackState(LaserDroneBossState.HeavyTelegraph);
    }

    private void UpdateHeavyTelegraph()
    {
        if (stateTimer < heavyTelegraphDuration)
        {
            return;
        }

        SetHeavyBeamActive(true);
        ChangeAttackState(LaserDroneBossState.HeavyFiring);
    }

    private void UpdateHeavyFiring()
    {
        if (heavyBeam != null)
        {
            heavyBeam.TickDamage();
        }

        if (stateTimer < heavyFireDuration)
        {
            return;
        }

        SetHeavyBeamActive(false);
        ChangeAttackState(LaserDroneBossState.Recover);
    }

    private void UpdateZoneTelegraph()
    {
        if (stateTimer < zoneTelegraphDuration)
        {
            return;
        }

        ActivateZoneDrones();
        ChangeAttackState(LaserDroneBossState.Recover);
    }

    private void ActivateZoneDrones()
    {
        if (upperZoneDrone == null || lowerZoneDrone == null)
        {
            Debug.LogError(
                "LaserDroneBoss: assegna Upper Zone Drone e Lower Zone Drone nell'Inspector.",
                this);
            return;
        }

        if (upperZoneDrone == lowerZoneDrone)
        {
            Debug.LogError(
                "LaserDroneBoss: Upper Zone Drone e Lower Zone Drone devono essere due istanze diverse.",
                this);
            return;
        }

        Vector3 upperStart = new Vector3(zoneDroneSpawnX, upperZoneY, 0f);
        Vector3 upperHold = new Vector3(zoneDroneHoldX, upperZoneY, 0f);
        upperZoneDrone.Activate(
            upperStart,
            upperHold,
            zoneDroneActiveDuration);

        Vector3 lowerStart = new Vector3(zoneDroneSpawnX, lowerZoneY, 0f);
        Vector3 lowerHold = new Vector3(zoneDroneHoldX, lowerZoneY, 0f);
        lowerZoneDrone.Activate(
            lowerStart,
            lowerHold,
            zoneDroneActiveDuration);
    }

    private void DeactivateZoneDrones()
    {
        if (upperZoneDrone != null)
        {
            upperZoneDrone.Deactivate();
        }

        if (lowerZoneDrone != null)
        {
            lowerZoneDrone.Deactivate();
        }
    }

    private void UpdateRecover()
    {
        if (stateTimer < recoverDuration)
        {
            return;
        }

        BeginNextAttack();
    }

    private LaserDroneAttack TakeNextBagAttack()
    {
        if (runtimeAttackBag == null || runtimeAttackBag.Length == 0)
        {
            return LaserDroneAttack.HeavyLaser;
        }

        if (attackBagIndex >= runtimeAttackBag.Length)
        {
            ShuffleRuntimeAttackBag();
        }

        LaserDroneAttack result = runtimeAttackBag[attackBagIndex];
        attackBagIndex = attackBagIndex + 1;
        return result;
    }

    private void BuildRuntimeAttackBag()
    {
        if (attackBag == null || attackBag.Length == 0)
        {
            runtimeAttackBag = new LaserDroneAttack[]
            {
                LaserDroneAttack.HeavyLaser,
                LaserDroneAttack.HeavyLaser,
                LaserDroneAttack.ZoneDrones
            };
        }
        else
        {
            runtimeAttackBag = new LaserDroneAttack[attackBag.Length];

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
            LaserDroneAttack temporary = runtimeAttackBag[i];
            runtimeAttackBag[i] = runtimeAttackBag[swapIndex];
            runtimeAttackBag[swapIndex] = temporary;
        }

        attackBagIndex = 0;
    }

    private void SetTrackingBeamActive(bool isActive)
    {
        if (trackingBeam != null)
        {
            trackingBeam.SetBeamActive(isActive);
        }
    }

    private void SetTrackingWarningVisible(bool isVisible)
    {
        if (trackingBeam != null)
        {
            trackingBeam.SetWarningVisible(isVisible);
        }
    }

    private void SetHeavyBeamActive(bool isActive)
    {
        if (heavyBeam != null)
        {
            heavyBeam.SetBeamActive(isActive);
        }
    }

    private void SetHeavyWarningVisible(bool isVisible)
    {
        if (heavyBeam != null)
        {
            heavyBeam.SetWarningVisible(isVisible);
        }
    }

    private void ChangeAttackState(LaserDroneBossState nextState)
    {
        attackState = nextState;
        stateTimer = 0f;
    }
}
