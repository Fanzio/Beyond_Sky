using UnityEngine;
public class AscensionSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlaneInput planeInput;
    [SerializeField] private PlaneController planeController;
    [SerializeField] private BackgroundSwitcher backgroundSwitcher;
    [SerializeField] private WorldScroller worldScroller;

    [Header("Timing")]
    [SerializeField] private float duration = 4.5f;
    [SerializeField] private float swapAtProgress = 0.45f;

    [Header("Effect")]
    [Min(0f)]
    [SerializeField] private float backgroundTravelDistance = 20f;
    [SerializeField] private float climbHeight = 5f;
    [SerializeField] private float noseUpAngle = 28f;

    private bool isPlaying;
    private float timer;
    private float startY;
    private bool hasSwapped;
    private int previousFloorIndex;

    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    public void Begin()
    {
        previousFloorIndex = floorManager.CurrentFloorIndex;
        bool wasTopFloor = floorManager.IsTopFloor;
        floorManager.NotifyBossDefeated();

        if (wasTopFloor)
        {
            return;
        }

        isPlaying = true;
        AudioSignals.Play(GameSound.Ascension);
        timer = 0f;
        hasSwapped = false;
        startY = planeController.transform.position.y;
        backgroundSwitcher.CaptureVerticalStart(previousFloorIndex);
        backgroundSwitcher.CaptureVerticalStart(floorManager.CurrentFloorIndex);

        planeInput.SetLocked(true);
        gameManager.SetAscending(true);
    }

    private void Update()
    {
        if (isPlaying == false)
        {
            return;
        }

        timer = timer + Time.deltaTime;
        float progress = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
        float swapProgress = Mathf.Clamp(swapAtProgress, 0.01f, 0.99f);
        float shape = Mathf.Sin(progress * 180f * Mathf.Deg2Rad);

        ApplyBackgroundMovement(progress, swapProgress);
        planeController.SetTiltOverride(shape * noseUpAngle);
        ApplyClimb(shape);
        SwapWhenDue(progress, swapProgress);

        if (progress < 1f)
        {
            return;
        }

        Finish();
    }

    private void ApplyBackgroundMovement(float progress, float swapProgress)
    {
        float movement = (1f - Mathf.Cos(progress * Mathf.PI)) * 0.5f;
        float movementAtSwap = (1f - Mathf.Cos(swapProgress * Mathf.PI)) * 0.5f;
        float fullDistance = backgroundTravelDistance / (1f - movementAtSwap);
        float outgoingDistance = fullDistance * Mathf.Min(movement, movementAtSwap);
        float incomingDistance = fullDistance * Mathf.Max(0f, movement - movementAtSwap);

        backgroundSwitcher.SetVerticalDisplacement(previousFloorIndex, outgoingDistance);
        backgroundSwitcher.SetVerticalDisplacement(floorManager.CurrentFloorIndex, incomingDistance);
    }

    private void ApplyClimb(float shape)
    {
        float targetY = startY + shape * climbHeight;
        planeController.MoveVerticalBy(targetY - planeController.transform.position.y);
    }

    private void SwapWhenDue(float progress, float swapProgress)
    {
        if (hasSwapped)
        {
            return;
        }
        if (progress < swapProgress)
        {
            return;
        }

        hasSwapped = true;
        backgroundSwitcher.ShowFloor(floorManager.CurrentFloorIndex);
    }

    private void Finish()
    {
        isPlaying = false;
        backgroundSwitcher.SetVerticalDisplacement(
            floorManager.CurrentFloorIndex, backgroundTravelDistance);
        planeController.ClearTiltOverride();
        planeInput.SetLocked(false);
        gameManager.SetAscending(false);

        floorManager.ResetFloorDistance();
        worldScroller.ClearActiveChunks();
    }
}
