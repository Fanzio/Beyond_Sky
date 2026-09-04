using UnityEngine;

// An obstacle that drifts inside its own chunk. Goes on the same object as Obstacle.
//
// Negative speed moves it LEFT, towards the player, which adds to the world scroll and
// cuts the time available to react. That is why the field is clamped by a Range instead
// of being checked at runtime: the inspector simply will not let an unfair value be
// typed. A limit that cannot be broken beats a limit that has to be policed.
//
// The arithmetic behind the cap: an obstacle has roughly 25 units to cover before
// reaching the plane. Static, that is about 1.25 seconds. At -6 it drops to around 0.95.
// Seeing, deciding and moving takes most of that, and a single hit ends the run.
//
// Positive speed moves it away and BUYS reaction time, so it needs no limit.
//
// The starting position is captured by the Chunk, not in this Awake. Awake order between
// separate GameObjects is not guaranteed, so a chunk could reach its OnEnable while this
// value was still an uninitialised zero and slam the obstacle to the chunk origin. The
// chunk calls CaptureStart from its own Awake instead, and order on a single GameObject
// always holds.
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