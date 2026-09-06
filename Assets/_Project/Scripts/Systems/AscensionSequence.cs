using UnityEngine;

// Stages the climb after a boss while input is locked.
// The background changes near the middle, when the faster parallax hides the transition.
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
    [SerializeField] private float ascensionVerticalFactor = 0.9f;
    [SerializeField] private float climbHeight = 5f;
    [SerializeField] private float noseUpAngle = 28f;

    private bool isPlaying;
    private float timer;
    private float startY;
    private bool hasSwapped;

    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    public void Begin()
    {
        bool wasTopFloor = floorManager.IsTopFloor;
        floorManager.NotifyBossDefeated();

        if (wasTopFloor)
        {
            return;
        }

        isPlaying = true;
        timer = 0f;
        hasSwapped = false;
        startY = planeController.transform.position.y;

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
        float progress = Mathf.Clamp01(timer / duration);
        float shape = Mathf.Sin(progress * 180f * Mathf.Deg2Rad);

        backgroundSwitcher.SetExtraVerticalFactor(shape * ascensionVerticalFactor);
        planeController.SetTiltOverride(shape * noseUpAngle);
        ApplyClimb(shape);
        SwapWhenDue(progress);

        if (progress < 1f)
        {
            return;
        }

        Finish();
    }

    private void ApplyClimb(float shape)
    {
        float targetY = startY + shape * climbHeight;
        planeController.MoveVerticalBy(targetY - planeController.transform.position.y);
    }

    private void SwapWhenDue(float progress)
    {
        if (hasSwapped)
        {
            return;
        }
        if (progress < swapAtProgress)
        {
            return;
        }

        hasSwapped = true;
        backgroundSwitcher.ShowFloor(floorManager.CurrentFloorIndex);
    }

    private void Finish()
    {
        isPlaying = false;
        backgroundSwitcher.SetExtraVerticalFactor(0f);
        planeController.ClearTiltOverride();
        planeInput.SetLocked(false);
        gameManager.SetAscending(false);

        floorManager.ResetFloorDistance();
        worldScroller.ClearActiveChunks();
    }
}
