using UnityEngine;
using TMPro;

public class SpaceReachedPanel : MonoBehaviour
{
    [SerializeField] private UIFade panel;
    [SerializeField] private WorldScroller world;
    [SerializeField] private ScoreManager score;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text coinsText;

    private bool hasBeenShown;

    public bool IsOpen
    {
        get { return panel.IsVisible; }
    }

    public float Duration
    {
        get { return panel.Duration; }
    }

    public bool Show()
    {
        if (hasBeenShown)
        {
            return false;
        }

        hasBeenShown = true;
        distanceText.text = "DISTANCE: " + world.DistanceTravelled.ToString("F0") + " m";
        coinsText.text = "COINS: " + score.RunCoins;
        panel.Show();
        return true;
    }

    public void Hide()
    {
        panel.Hide();
    }

    public void HideImmediately()
    {
        panel.HideImmediately();
    }
}
