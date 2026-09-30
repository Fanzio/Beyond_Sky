// Aumenta gradualmente la velocità di scorrimento verso il limite massimo.
using UnityEngine;

namespace BeyondSky.Systems
{
    public class SpeedManager : MonoBehaviour
    {
        [Header("Ramp")]
        [SerializeField] private float startSpeed = 16f;
        [SerializeField] private float maxSpeed = 36f;
        [SerializeField] private float rampRate = 0.035f;

        private float currentSpeed;

        public float CurrentSpeed
        {
            get { return currentSpeed; }
        }

        public float NormalizedSpeed
        {
            get { return Mathf.InverseLerp(startSpeed, maxSpeed, currentSpeed); }
        }

        private void Awake()
        {
            currentSpeed = startSpeed;
        }

        private void Update()
        {
            ApplyRamp(Time.deltaTime);
        }

        private void ApplyRamp(float deltaTime)
        {
            float remainingSpeed = maxSpeed - currentSpeed;

            if (remainingSpeed <= 0f)
            {
                return;
            }

            currentSpeed = currentSpeed + remainingSpeed * rampRate * deltaTime;
        }
    }
}
