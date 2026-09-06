using UnityEngine;

// Manages limited ammunition and obtains every projectile from the shared bullet pool.
public class PlaneWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneInput planeInput;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform muzzle;

    [Header("Firing")]
    [SerializeField] private float bulletSpeed = 34f;
    [SerializeField] private float fireCooldown = 0.18f;

    [Header("Ammo")]
    [SerializeField] private int startingAmmo = 0;

    private int ammo;
    private float cooldownTimer;

    public int Ammo
    {
        get { return ammo; }
    }

    public void AddAmmo(int amount)
    {
        ammo = ammo + amount;
    }

    private void Awake()
    {
        ammo = startingAmmo;
    }

    private void Update()
    {
        if (gameManager.IsGameOver)
        {
            planeInput.ConsumeFire();
            return;
        }

        TickCooldown(Time.deltaTime);

        if (planeInput.ConsumeFire() == false)
        {
            return;
        }

        TryFire();
    }

    private void TickCooldown(float deltaTime)
    {
        if (cooldownTimer <= 0f)
        {
            return;
        }

        cooldownTimer = cooldownTimer - deltaTime;
    }

    private void TryFire()
    {
        if (ammo <= 0)
        {
            return;
        }

        if (cooldownTimer > 0f)
        {
            return;
        }

        Bullet bullet = bulletPool.GetBullet();

        if (bullet == null)
        {
            return;
        }

        bullet.Launch(muzzle.position, Vector2.right, bulletSpeed);
        ammo = ammo - 1;
        cooldownTimer = fireCooldown;
    }
}
