using UnityEngine;

public class PlaneShield : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer shieldVisual;

    [Header("Break")]
    [SerializeField] private float breakInvulnerabilityTime = 0.7f;

    private bool hasShield;
    private float invulnerabilityTimer;

    public bool HasShield
    {
        get { return hasShield; }
    }

    public bool IsInvulnerable
    {
        get { return invulnerabilityTimer > 0f; }
    }

    private void Awake()
    {
        UpdateVisual();
    }

    private void Update()
    {
        if (invulnerabilityTimer <= 0f)
        {
            return;
        }

        invulnerabilityTimer = invulnerabilityTimer - Time.deltaTime;
    }

    public void GrantShield()
    {
        hasShield = true;
        AudioSignals.Play(GameSound.ShieldPickup);
        UpdateVisual();
    }

    public bool ConsumeShield()
    {
        if (hasShield == false)
        {
            return false;
        }

        hasShield = false;
        AudioSignals.Play(GameSound.ShieldBreak);
        invulnerabilityTimer = breakInvulnerabilityTime;
        UpdateVisual();
        return true;
    }

    private void UpdateVisual()
    {
        if (shieldVisual == null)
        {
            return;
        }

        shieldVisual.enabled = hasShield;
    }
}
