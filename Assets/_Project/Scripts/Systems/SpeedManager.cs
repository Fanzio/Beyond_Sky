using UnityEngine;

// The speed of the whole game: how fast the world scrolls past.
//
// Speed rises slowly and never resets. In this genre speed IS the difficulty ramp, so it
// belongs to time, not to skill. Rewarding a risky move with more speed would be handing
// the player a punishment and calling it a prize, and they would simply stop taking the
// risk once they worked that out.
//
// The rise is asymptotic: quick at first so the opening minute has shape, then flattening
// out. A run should end because of a mistake, not because the game became unplayable.
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
    public void ResetSpeed()
    {
        currentSpeed = startSpeed;
    }
    private void ApplyRamp(float deltaTime)
    {
        float headroom = maxSpeed - currentSpeed;

        if (headroom <= 0f)
        {
            return;
        }
        currentSpeed = currentSpeed + headroom * rampRate * deltaTime;
    }
}