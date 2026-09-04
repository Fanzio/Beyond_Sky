using UnityEngine;

// Moves an obstacle up or down inside its chunk. Goes alongside Obstacle, and can be
// combined with MovingObstacle for something that climbs while drifting sideways.
//
// Positive speed rises, negative sinks. The speed is capped because the flight band is
// only 16 units tall: something crossing it quickly changes the shape of its chunk faster
// than the player can read it, and late in a run there is no time left to react.
//
// Position is restored by the chunk on recycle, same as the sideways drift.
public class VerticalObstacle : MonoBehaviour
{
    [Header("Vertical drift - positive rises")]
    [Range(-6f, 6f)]
    [SerializeField] private float verticalSpeed = 2f;

    private Vector3 startLocalPosition;
    private bool hasCaptured;

    public void CaptureStart()
    {
        if (hasCaptured)
        {
            return;
        }
        startLocalPosition = transform.localPosition;
        hasCaptured = true;
    }
    public void ResetPosition()
    {
        CaptureStart();
        transform.localPosition = startLocalPosition;
    }
    private void Update()
    {
        Vector3 position = transform.localPosition;
        position.y = position.y + verticalSpeed * Time.deltaTime;
        transform.localPosition = position;
    }
}
