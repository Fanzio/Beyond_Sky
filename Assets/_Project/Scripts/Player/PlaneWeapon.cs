using UnityEngine;

// Firing and ammo. The player starts a run with nothing and has to find bullets.
//
// The whole point of scarcity is the decision it creates: every bullet spent clearing a
// path is a bullet missing when the boss arrives. Make ammo plentiful and that decision
// disappears, and with it the close call loop, because shooting everything is easier
// than threading gaps.
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