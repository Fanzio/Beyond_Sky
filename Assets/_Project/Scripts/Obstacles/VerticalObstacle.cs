using UnityEngine;

// Moves an obstacle vertically inside its chunk. The chunk restores its starting position
// when recycled, while the Range protects reaction time inside the flight band.
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
