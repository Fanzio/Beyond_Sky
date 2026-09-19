using UnityEngine;

public class PlaneDodge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneInput planeInput;
    [SerializeField] private Transform visual;

    [Header("Roll")]
    [SerializeField] private float rollDuration;
    [SerializeField] private float rollBankAngle;

    [Header("Cooldown")]
    [SerializeField] private float cooldown;

    private bool isRolling;
    private float rollTimer;
    private float rollDirection;
    private float cooldownTimer;
    private Vector3 baseScale;

    public bool IsRolling
    {
        get { return isRolling; }
    }

    public bool IsInvulnerable
    {
        get { return isRolling; }
    }

    public bool IsReady
    {
        get { return cooldownTimer <= 0f; }
    }

    public float CooldownNormalized
    {
        get { return 1f - Mathf.Clamp01(cooldownTimer / cooldown); }
    }
    private void Awake()
    {
        baseScale = visual.localScale;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        TickCooldown(deltaTime);

        if (isRolling)
        {
            AdvanceRoll(deltaTime);
            return;
        }

        TryStartRoll();
    }

    private void TickCooldown(float deltaTime)
    {
        if (cooldownTimer <= 0f)
        {
            return;
        }

        cooldownTimer = cooldownTimer - deltaTime;
    }

    private void TryStartRoll()
    {
        float direction = planeInput.ConsumeDoubleTap();

        if (Mathf.Approximately(direction, 0f))
        {
            return;
        }

        if (IsReady == false)
        {
            return;
        }

        isRolling = true;
        AudioSignals.Play(GameSound.Dodge);
        rollTimer = 0f;
        rollDirection = direction;
    }

    private void AdvanceRoll(float deltaTime)
    {
        rollTimer = rollTimer + deltaTime;

        UpdateRollVisual();

        if (rollTimer < rollDuration)
        {
            return;
        }

        EndRoll();
    }

    private void UpdateRollVisual()
    {
        float progress = rollTimer / rollDuration;

        Vector3 scale = baseScale;
        scale.y = baseScale.y * Mathf.Cos(progress * 360f * Mathf.Deg2Rad);
        visual.localScale = scale;

        float bank = Mathf.Sin(progress * 180f * Mathf.Deg2Rad) * rollBankAngle;
        visual.localRotation = Quaternion.Euler(0f, 0f, rollDirection * bank);
    }

    private void EndRoll()
    {
        isRolling = false;
        visual.localScale = baseScale;
        visual.localRotation = Quaternion.identity;
        cooldownTimer = cooldown;
    }
}
