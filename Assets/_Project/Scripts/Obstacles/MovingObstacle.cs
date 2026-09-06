using UnityEngine;

// Moves an obstacle horizontally inside its chunk. The chunk captures and restores the
// starting position because objects are recycled, while the Range protects reaction time.
public class MovingObstacle : MonoBehaviour
{
    [Header("Drift - negative comes at the player")]
    [Range(-6f, 12f)]
    [SerializeField] private float horizontalSpeed = -5f;

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
        position.x = position.x + horizontalSpeed * Time.deltaTime;
        transform.localPosition = position;
    }
}
