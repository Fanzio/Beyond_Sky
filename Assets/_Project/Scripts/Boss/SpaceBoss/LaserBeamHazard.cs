// Visualizza un raggio laser e controlla se colpisce il player.
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.Player;

namespace BeyondSky.Boss
{
    public class LaserBeamHazard : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer beamRenderer;
        [SerializeField] private Color warningColor = new Color(1f, 0.55f, 0.1f, 0.45f);
        [SerializeField] private float warningFlashSpeed = 10f;

        [Header("Damage area")]
        [SerializeField] private LayerMask playerLayers;
        [SerializeField] private Vector2 damageSize = new Vector2(28f, 0.8f);
        [SerializeField] private Vector2 localCenterOffset = Vector2.zero;

        private bool beamActive;
        private bool warningVisible;
        private bool missingRendererWarningShown;
        private bool activeColorCached;
        private Color activeColor;

        public void SetBeamActive(bool isActive)
        {
            if (isActive && beamRenderer == null)
            {
                beamActive = false;
                warningVisible = false;
                ShowMissingRendererWarning();
                return;
            }

            CacheActiveColor();
            AudioSignals.SetLaser(this, isActive);
            beamActive = isActive;
            warningVisible = false;

            if (beamRenderer != null)
            {
                beamRenderer.enabled = isActive;

                if (activeColorCached)
                {
                    beamRenderer.color = activeColor;
                }
            }
        }

        public void SetWarningVisible(bool isVisible)
        {
            if (isVisible && beamRenderer == null)
            {
                beamActive = false;
                warningVisible = false;
                ShowMissingRendererWarning();
                return;
            }

            CacheActiveColor();
            bool warningWasVisible = warningVisible;
            AudioSignals.SetLaser(this, false);
            if (isVisible && warningWasVisible == false)
            {
                AudioSignals.Play(GameSound.EnemyWarning);
            }
            beamActive = false;
            warningVisible = isVisible;

            if (beamRenderer == null)
            {
                return;
            }

            beamRenderer.enabled = isVisible;

            if (isVisible)
            {
                if (warningWasVisible == false)
                {
                    beamRenderer.color = warningColor;
                }
            }
            else if (activeColorCached)
            {
                beamRenderer.color = activeColor;
            }
        }

        private void Update()
        {
            if (warningVisible == false || beamRenderer == null)
            {
                return;
            }

            float pulse = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);
            Color pulsingColor = warningColor;
            pulsingColor.a = warningColor.a * Mathf.Lerp(0.3f, 1f, pulse);
            beamRenderer.color = pulsingColor;
        }

        public void TickDamage()
        {
            if (beamActive == false)
            {
                return;
            }

            Vector3 worldCenter = transform.TransformPoint(localCenterOffset);
            Collider2D hit = Physics2D.OverlapBox(
                worldCenter,
                damageSize,
                0f,
                playerLayers);

            if (hit == null)
            {
                return;
            }

            PlaneCollision plane = hit.GetComponentInParent<PlaneCollision>();

            if (plane != null)
            {
                plane.TakeHit();
            }
        }

        private void OnDisable()
        {
            AudioSignals.SetLaser(this, false);
            beamActive = false;
            warningVisible = false;

            if (beamRenderer != null)
            {
                beamRenderer.enabled = false;
            }
        }

        private void CacheActiveColor()
        {
            if (activeColorCached || beamRenderer == null)
            {
                return;
            }

            activeColor = beamRenderer.color;
            activeColorCached = true;
        }

        private void ShowMissingRendererWarning()
        {
            if (missingRendererWarningShown)
            {
                return;
            }

            Debug.LogWarning("LaserBeamHazard: Beam Renderer non assegnato. Il laser resta disattivato.", this);
            missingRendererWarningShown = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 worldCenter = transform.TransformPoint(localCenterOffset);
            Gizmos.DrawWireCube(worldCenter, damageSize);
        }
    }
}
