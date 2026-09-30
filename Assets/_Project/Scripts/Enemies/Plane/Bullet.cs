// Muove un proiettile e rileva i bersagli lungo il percorso.
using UnityEngine;
using BeyondSky.Boss;
using BeyondSky.Player;
using BeyondSky.World;

namespace BeyondSky.Enemies
{
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

            BossBase boss = other.GetComponentInParent<BossBase>();

            if (boss != null)
            {
                boss.TakeBulletHit();
                Despawn();
                return;
            }

            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();

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
}
