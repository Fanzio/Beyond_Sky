// Muove un missile e ne gestisce esplosione e danni.
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.Player;

namespace BeyondSky.Boss
{
    public enum ExplosiveMissileState
    {
        Flying,
        Exploding
    }
    public class ExplosiveMissile : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer missileRenderer;
        [SerializeField] private GameObject explosionVisual;
        [SerializeField] private float visualAngleOffset;

        [Header("Flight collision")]
        [SerializeField] private LayerMask playerLayers;
        [SerializeField] private float flightRadius = 0.3f;
        [SerializeField] private float maximumFlightTime = 6f;

        [Header("Explosion")]
        [SerializeField] private float explosionRadius = 2.2f;
        [SerializeField] private float explosionDuration = 1f;

        private ExplosiveMissileState state;
        private Vector2 velocity;
        private Vector2 destination;
        private float stateTimer;

        public void Launch(Vector3 startPosition, Vector3 targetPosition, float speed)
        {
            transform.position = startPosition;

            Vector2 direction = new Vector2(
                targetPosition.x - startPosition.x,
                targetPosition.y - startPosition.y);

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.left;
            }

            direction.Normalize();
            velocity = direction * speed;
            destination = targetPosition;
            state = ExplosiveMissileState.Flying;
            stateTimer = 0f;

            PointAlong(direction);
            SetVisuals(true, false);
            gameObject.SetActive(true);
            AudioSignals.Play(GameSound.EnemyShot);
        }

        public void Despawn()
        {
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            SetVisuals(true, false);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            stateTimer = stateTimer + deltaTime;

            if (state == ExplosiveMissileState.Flying)
            {
                UpdateFlight(deltaTime);
                return;
            }

            UpdateExplosion();
        }

        private void UpdateFlight(float deltaTime)
        {
            if (stateTimer >= maximumFlightTime)
            {
                BeginExplosion(transform.position);
                return;
            }

            Vector2 step = velocity * deltaTime;

            if (step.sqrMagnitude < 0.000001f)
            {
                return;
            }

            RaycastHit2D hit = Physics2D.CircleCast(
                transform.position,
                flightRadius,
                step.normalized,
                step.magnitude,
                playerLayers);

            if (hit.collider != null)
            {
                BeginExplosion(hit.point);
                return;
            }

            Vector2 currentPosition = transform.position;
            float distanceBeforeMove = Vector2.Distance(currentPosition, destination);

            if (step.magnitude >= distanceBeforeMove)
            {
                BeginExplosion(destination);
                return;
            }

            transform.position = transform.position + (Vector3)step;
        }

        private void BeginExplosion(Vector2 explosionPosition)
        {
            transform.position = new Vector3(explosionPosition.x, explosionPosition.y, 0f);
            state = ExplosiveMissileState.Exploding;
            AudioSignals.Play(GameSound.Explosion);
            stateTimer = 0f;
            SetVisuals(false, true);
            DealExplosionDamage();
        }

        private void UpdateExplosion()
        {
            DealExplosionDamage();

            if (stateTimer < explosionDuration)
            {
                return;
            }

            Despawn();
        }

        private void DealExplosionDamage()
        {
            Collider2D hit = Physics2D.OverlapCircle(
                transform.position,
                explosionRadius,
                playerLayers);

            if (hit == null)
            {
                return;
            }

            PlaneCollision plane = hit.GetComponentInParent<PlaneCollision>();

            if (plane == null)
            {
                return;
            }

            plane.TakeHit();
        }

        private void PointAlong(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + visualAngleOffset);
        }

        private void SetVisuals(bool showMissile, bool showExplosion)
        {
            if (missileRenderer != null)
            {
                missileRenderer.enabled = showMissile;
            }

            if (explosionVisual != null && explosionVisual != gameObject)
            {
                explosionVisual.SetActive(showExplosion);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, flightRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
