using UnityEngine;

public class PickupCollector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PlaneShield planeShield;
    [SerializeField] private PlaneWeapon planeWeapon;

    [Header("Detection")]
    [SerializeField] private LayerMask pickupLayers;
    [SerializeField] private float pickupRadius = 1.4f;

    private Collider2D[] hits = new Collider2D[8];

    private void Update()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, pickupRadius, hits, pickupLayers);

        for (int i = 0; i < count; i++)
        {
            Pickup pickup = hits[i].GetComponent<Pickup>();

            if (pickup == null)
            {
                continue;
            }

            if (pickup.IsCollected)
            {
                continue;
            }

            Collect(pickup);
        }
    }

    private void Collect(Pickup pickup)
    {
        pickup.Collect();

        if (pickup.Type == PickupType.Coin)
        {
            scoreManager.AddCoin(pickup.Amount);
            return;
        }

        if (pickup.Type == PickupType.Shield)
        {
            planeShield.GrantShield();
            return;
        }

        if (pickup.Type == PickupType.Ammo)
        {
            planeWeapon.AddAmmo(pickup.Amount);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
