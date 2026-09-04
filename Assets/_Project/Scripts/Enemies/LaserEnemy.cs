using UnityEngine;

public enum LaserState
{
    Enter,
    Align,
    Charge,
    Fire,
    Leave
}

// An enemy that matches the player's height, holds position, charges, then fires a beam
// across the whole screen for a few seconds.
//
// It stops tracking the moment it starts charging. Since the beam covers its own height
// all the way to the left edge, a drone that kept aligning would be unavoidable: the
// charge IS the warning, and the player has that window to leave the lane.
//
// The beam is a separate child object with its own collider, sitting on the Obstacle
// layer, so it kills exactly like any other obstacle and needs no special case anywhere.
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

    private Transform target;
    private LaserState state;
    private float stateTimer;
    private Vector3 beamBaseScale;

    public override void Setup(Transform playerTarget, BulletPool pool)
    {
        target = playerTarget;
    }
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
        gameObject.SetActive(false);
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
        float targetY = Mathf.Clamp(target.position.y, minY, maxY);
        SetY(Mathf.MoveTowards(transform.position.y, targetY, alignSpeed * deltaTime));

        if (stateTimer < alignDuration)
        {
            return;
        }
        ChangeState(LaserState.Charge);
    }
    private void UpdateCharge()
    {
        float blend = Mathf.PingPong(Time.time * flashSpeed, 1f);
        bodyRenderer.color = Color.Lerp(normalColor, chargeColor, blend);

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
        gameObject.SetActive(false);
    }

    // The beam snaps out over a fraction of a second rather than appearing at full size.
    // It reads as a beam firing instead of a wall blinking into existence, and the growth
    // is fast enough that it never costs the player a reaction they had earned.
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
    private void MoveBy(float deltaX, float deltaY)
    {
        Vector3 position = transform.position;
        position.x = position.x + deltaX;
        position.y = position.y + deltaY;
        transform.position = position;
    }
    private void SetY(float y)
    {
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;
    }
}