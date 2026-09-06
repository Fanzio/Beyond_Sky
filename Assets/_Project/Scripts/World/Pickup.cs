using UnityEngine;

public enum PickupType
{
    Coin,
    Shield,
    Ammo
}

// Hides collected pickups so their chunk can restore and reuse them later.
public class Pickup : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private PickupType type;
    [SerializeField] private int amount = 1;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D pickupCollider;

    private bool isCollected;

    public PickupType Type
    {
        get { return type; }
    }

    public int Amount
    {
        get { return amount; }
    }

    public bool IsCollected
    {
        get { return isCollected; }
    }

    public void Collect()
    {
        isCollected = true;
        spriteRenderer.enabled = false;
        pickupCollider.enabled = false;
    }

    public void ResetPickup()
    {
        isCollected = false;
        spriteRenderer.enabled = true;
        pickupCollider.enabled = true;
    }
}
