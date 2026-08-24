using UnityEngine;

// Vertical movement inside a bounded band. There is no flight model: the input sets a
// target vertical speed and the plane reaches it almost immediately, because dodging is
// a half-second decision and the controls have to answer straight away.
//
// The tilt is derived in LateUpdate from how far the plane ACTUALLY moved this frame,
// not from its own velocity field. Anything that moves the plane therefore tilts it,
// including sequences that drive it from the outside. Deriving it from the velocity
// field meant the nose stayed level during the ascension, since that climb is produced
// by another script calling MoveVerticalBy.
//
// A cutscene can still stage the pose explicitly with SetTiltOverride: the plane is
// boxed inside the band, so its real movement is far too small to sell a long climb.
public class PlaneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneInput planeInput;
    [SerializeField] private PlaneDodge planeDodge;
    [SerializeField] private Transform visual;

    [Header("Movement")]
    [SerializeField] private float verticalSpeed;
    [SerializeField] private float acceleration;

    [Header("Band limits")]
    [SerializeField] private float minY;
    [SerializeField] private float maxY;

    [Header("Visual tilt")]
    [SerializeField] private float maxTiltAngle;
    [SerializeField] private float tiltSmoothing;

    private float verticalVelocity;
    private float currentTilt;
    private float lastY;
    private bool hasTiltOverride;
    private float tiltOverride;

    public float VerticalVelocity
    {
        get { return verticalVelocity; }
    }

    public void SetTiltOverride(float angle)
    {
        hasTiltOverride = true;
        tiltOverride = angle;
    }
    public void ClearTiltOverride()
    {
        hasTiltOverride = false;
    }
    public void MoveVerticalBy(float delta)
    {
        Vector3 position = transform.position;
        position.y = position.y + delta;

        if (position.y <= minY)
        {
            position.y = minY;
            verticalVelocity = 0f;
        }
        if (position.y >= maxY)
        {
            position.y = maxY;
            verticalVelocity = 0f;
        }
        transform.position = position;
    }
    private void Start()
    {
        lastY = transform.position.y;
    }
    private void Update()
    {
        if (planeDodge != null)
        {
            if (planeDodge.IsRolling)
            {
                verticalVelocity = 0f;
                return;
            }
        }
        float deltaTime = Time.deltaTime;

        UpdateVerticalVelocity(deltaTime);
        MoveVerticalBy(verticalVelocity * deltaTime);
    }
    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        float movedRate = (transform.position.y - lastY) / deltaTime;
        lastY = transform.position.y;

        UpdateTilt(movedRate, deltaTime);
    }
    private void UpdateVerticalVelocity(float deltaTime)
    {
        float target = planeInput.VerticalInput * verticalSpeed;

        verticalVelocity = Mathf.MoveTowards(
            verticalVelocity, target, acceleration * deltaTime);
    }
    private void UpdateTilt(float movedRate, float deltaTime)
    {
        float targetTilt = tiltOverride;

        if (hasTiltOverride == false)
        {
            float ratio = Mathf.Clamp(movedRate / verticalSpeed, -1f, 1f);
            targetTilt = ratio * maxTiltAngle;
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothing * deltaTime);
        visual.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 left = new Vector3(transform.position.x - 20f, 0f, 0f);
        Vector3 right = new Vector3(transform.position.x + 20f, 0f, 0f);

        Gizmos.DrawLine(left + Vector3.up * minY, right + Vector3.up * minY);
        Gizmos.DrawLine(left + Vector3.up * maxY, right + Vector3.up * maxY);
    }
}