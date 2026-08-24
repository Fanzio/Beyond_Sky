using UnityEngine;

// A bullet moves in a straight line and dies on contact or after its lifetime.
//
// The hit check is a swept cast along the bullet's own path, not an overlap. The bullet
// is the fast moving thing here, so it has to be the one asking whether it crossed
// something, or it would pass straight through its target between two frames.
//
// The same script serves both sides. What a bullet can hit is decided by targetLayers
// on its prefab, so the enemy prefab points at Player and the player prefab points at
// Obstacle. Nothing here needs to know who fired.
public class Bullet : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float radius = 0.25f;

    [Header("Life")]
    [SerializeField] private float lifetime = 5f;

    private Vector2 velocity;
    private float timer;

    public void Launch(Vector3 startPosition, Vector2 direction, float speed)
    {
        transform.position = startPosition;
        velocity = direction.normalized * speed;
        timer = 0f;
        gameObject.SetActive(true);
    }
    public void Despawn()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        timer = timer + deltaTime;

        if (timer >= lifetime)
        {
            Despawn();
            return;
        }

        Vector2 step = velocity * deltaTime;

        if (step.magnitude < 0.0001f)
        {
            return;
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, radius, step.normalized, step.magnitude, targetLayers);

        if (hit.collider != null)
        {
            HitTarget(hit.collider);
            return;
        }

        transform.position = transform.position + (Vector3)step;
    }
    private void HitTarget(Collider2D other)
    {
        PlaneCollision plane = other.GetComponentInParent<PlaneCollision>();

        if (plane != null)
        {
            plane.TakeHit();
            Despawn();
            return;
        }

        BossPlane boss = other.GetComponentInParent<BossPlane>();

        if (boss != null)
        {
            boss.TakeBulletHit();
            Despawn();
            return;
        }

        EnemyPlane enemy = other.GetComponentInParent<EnemyPlane>();

        if (enemy != null)
        {
            enemy.TakeBulletHit();
            Despawn();
            return;
        }

        Obstacle obstacle = other.GetComponentInParent<Obstacle>();

        if (obstacle != null)
        {
            obstacle.DestroyObstacle();
        }
        Despawn();
    }
}