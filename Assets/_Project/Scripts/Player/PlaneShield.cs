using UnityEngine;

// Absorbs one hit and grants brief invulnerability so the same overlapping obstacle
// cannot immediately hit the plane again.
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
        UpdateVisual();
    }

    public bool ConsumeShield()
    {
        if (hasShield == false)
        {
            return false;
        }

        hasShield = false;
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
