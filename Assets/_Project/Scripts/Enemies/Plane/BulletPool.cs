using System.Collections.Generic;
using UnityEngine;

// Bullets are created once at Awake and only ever activated or deactivated after that.
// Allocating during play is what causes the stutter you cannot tune away later.
public class BulletPool : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int poolSize = 16;

    private List<Bullet> bullets = new List<Bullet>();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Bullet bullet = Instantiate(bulletPrefab, transform);
            bullet.gameObject.SetActive(false);
            bullets.Add(bullet);
        }
    }
    public Bullet GetBullet()
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i].gameObject.activeSelf)
            {
                continue;
            }
            return bullets[i];
        }
        return null;
    }
}
