using UnityEngine;

public enum LaserState
{
    Enter,
    Align,
    Charge,
    Fire,
    Leave
}

// Stops tracking before charging so the warning gives the player a fair escape window.
// Its separate beam child uses the normal obstacle collision path.
public class LaserEnemy : EnemyBase
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color chargeColor = Color.red;
    [SerializeField] private float flashSpeed = 12f;

    [Header("Beam")]
    [SerializeField] private GameObject beamObject;
    [SerializeField] private Transform beamVisual;
    [SerializeField] private float beamGrowTime = 0.12f;

    [Header("Timing")]
    [SerializeField] private float alignDuration = 1.2f;
    [SerializeField] private float chargeDuration = 1f;
    [SerializeField] private float fireDuration = 3f;

    [Header("Movement")]
    [SerializeField] private float enterSpeed = 24f;
    [SerializeField] private float holdX = 14f;
    [SerializeField] private float alignSpeed = 8f;
    [SerializeField] private float leaveSpeed = 30f;
    [SerializeField] private float leaveDuration = 1.4f;

    [Header("Band limits")]
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    private LaserState state;
    private float stateTimer;
    private Vector3 beamBaseScale;

    public override void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        bodyRenderer.color = normalColor;
        SetBeamActive(false);
        ChangeState(LaserState.Enter);
        gameObject.SetActive(true);
    }

    public override void TakeBulletHit()
    {
        SetBeamActive(false);
        DeactivateEnemy();
    }

    private void Awake()
    {
        if (beamVisual != null)
        {
            beamBaseScale = beamVisual.localScale;
        }
    }

    private void OnDisable()
    {
        SetBeamActive(false);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stateTimer = stateTimer + deltaTime;

        if (state == LaserState.Enter)
        {
            UpdateEnter(deltaTime);
            return;
        }
        if (state == LaserState.Align)
        {
            UpdateAlign(deltaTime);
            return;
        }
        if (state == LaserState.Charge)
        {
            UpdateCharge();
            return;
        }
        if (state == LaserState.Fire)
        {
            UpdateFire(deltaTime);
            return;
        }
        UpdateLeave(deltaTime);
    }

    private void UpdateEnter(float deltaTime)
    {
        MoveBy(-enterSpeed * deltaTime, 0f);

        if (transform.position.x > holdX)
        {
            return;
        }
        ChangeState(LaserState.Align);
    }

    private void UpdateAlign(float deltaTime)
    {
        TrackTargetY(alignSpeed, minY, maxY, deltaTime);

        if (stateTimer < alignDuration)
        {
            return;
        }
        ChangeState(LaserState.Charge);
    }

    private void UpdateCharge()
    {
        bodyRenderer.color = FlashBetween(normalColor, chargeColor, flashSpeed);

        if (stateTimer < chargeDuration)
        {
            return;
        }

        bodyRenderer.color = chargeColor;
        SetBeamActive(true);
        ChangeState(LaserState.Fire);
    }

    private void UpdateFire(float deltaTime)
    {
        GrowBeam();

        if (stateTimer < fireDuration)
        {
            return;
        }

        SetBeamActive(false);
        bodyRenderer.color = normalColor;
        ChangeState(LaserState.Leave);
    }

    private void UpdateLeave(float deltaTime)
    {
        MoveBy(leaveSpeed * deltaTime, 0f);

        if (stateTimer < leaveDuration)
        {
            return;
        }
        DeactivateEnemy();
    }

    private void GrowBeam()
    {
        if (beamVisual == null)
        {
            return;
        }

        float progress = Mathf.Clamp01(stateTimer / beamGrowTime);
        Vector3 scale = beamBaseScale;
        scale.y = beamBaseScale.y * progress;
        beamVisual.localScale = scale;
    }

    private void SetBeamActive(bool active)
    {
        if (beamObject == null)
        {
            return;
        }
        beamObject.SetActive(active);
    }

    private void ChangeState(LaserState nextState)
    {
        state = nextState;
        stateTimer = 0f;
    }

}
