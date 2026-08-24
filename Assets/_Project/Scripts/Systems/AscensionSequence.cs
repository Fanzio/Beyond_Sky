using UnityEngine;

// The seconds between beating a boss and arriving on the next floor.
//
// It is the only moment in the game where the player has nothing to do, which makes it
// the only moment that can be purely spectacular. The vertical parallax that runs flat
// out here is the same one that stays switched off during normal play: showing "you are
// climbing" continuously would be a lie, since normally you are holding a floor. Here it
// is true, so it is worth turning all the way up.
//
// The plane barely moves and holds its nose up instead. It is boxed inside the band, so
// its real travel is far too small to sell a climb of hundreds of metres: the sense of
// rising comes from the background falling away. The pose is staged rather than derived,
// which is what a cutscene is.
//
// The background swap is timed to the middle of the climb, where the fast scroll hides it.
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