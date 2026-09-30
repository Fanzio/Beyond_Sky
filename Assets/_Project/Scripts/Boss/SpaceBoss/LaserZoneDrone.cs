// Gestisce un drone di zona e il suo attacco laser.
using UnityEngine;

namespace BeyondSky.Boss
{
    public enum LaserZoneDroneState
    {
        Entering,
        Charging,
        Active,
        Leaving
    }
    public class LaserZoneDrone : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color chargeColor = Color.red;
        [SerializeField] private float flashSpeed = 12f;

        [Header("Beam")]
        [SerializeField] private LaserBeamHazard beam;
        [SerializeField] private float chargeDuration = 0.75f;

        [Header("Movement")]
        [SerializeField] private float enterSpeed = 18f;
        [SerializeField] private float leaveSpeed = 22f;
        [SerializeField] private float leaveX = 30f;

        private LaserZoneDroneState state;
        private Vector3 holdPosition;
        private float activeDuration;
        private float stateTimer;

        public void Activate(Vector3 startPosition, Vector3 targetHoldPosition, float duration)
        {
            gameObject.SetActive(true);
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            holdPosition = targetHoldPosition;
            activeDuration = Mathf.Max(0f, duration);
            stateTimer = 0f;
            state = LaserZoneDroneState.Entering;

            SetBodyColor(normalColor);
            SetBeamActive(false);
        }

        public void Deactivate()
        {
            SetBeamActive(false);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            SetBeamActive(false);
            SetBodyColor(normalColor);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            stateTimer = stateTimer + deltaTime;

            if (state == LaserZoneDroneState.Entering)
            {
                UpdateEntering(deltaTime);
            }
            else if (state == LaserZoneDroneState.Charging)
            {
                UpdateCharging();
            }
            else if (state == LaserZoneDroneState.Active)
            {
                UpdateActive();
            }
            else if (state == LaserZoneDroneState.Leaving)
            {
                UpdateLeaving(deltaTime);
            }
        }

        private void UpdateEntering(float deltaTime)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                holdPosition,
                enterSpeed * deltaTime);

            if (Vector3.Distance(transform.position, holdPosition) > 0.01f)
            {
                return;
            }

            transform.position = holdPosition;
            SetBeamWarningVisible(true);
            ChangeState(LaserZoneDroneState.Charging);
        }

        private void UpdateCharging()
        {
            float blend = Mathf.PingPong(Time.time * flashSpeed, 1f);
            SetBodyColor(Color.Lerp(normalColor, chargeColor, blend));

            if (stateTimer < chargeDuration)
            {
                return;
            }

            SetBodyColor(chargeColor);
            SetBeamActive(true);
            ChangeState(LaserZoneDroneState.Active);
        }

        private void UpdateActive()
        {
            if (beam != null)
            {
                beam.TickDamage();
            }

            if (stateTimer < activeDuration)
            {
                return;
            }

            SetBeamActive(false);
            SetBodyColor(normalColor);
            ChangeState(LaserZoneDroneState.Leaving);
        }

        private void UpdateLeaving(float deltaTime)
        {
            Vector3 position = transform.position;
            position.x = position.x + leaveSpeed * deltaTime;
            transform.position = position;

            if (transform.position.x < leaveX)
            {
                return;
            }

            Deactivate();
        }

        private void SetBeamActive(bool isActive)
        {
            if (beam != null)
            {
                beam.SetBeamActive(isActive);
            }
        }

        private void SetBeamWarningVisible(bool isVisible)
        {
            if (beam != null)
            {
                beam.SetWarningVisible(isVisible);
            }
        }

        private void SetBodyColor(Color color)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = color;
            }
        }

        private void ChangeState(LaserZoneDroneState nextState)
        {
            state = nextState;
            stateTimer = 0f;
        }
    }
}
