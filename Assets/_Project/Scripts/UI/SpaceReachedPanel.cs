// Mostra una sola volta il traguardo dello Spazio e le statistiche della run.
using UnityEngine;
using TMPro;
using BeyondSky.Systems;
using BeyondSky.World;

namespace BeyondSky.UI
{
    public class SpaceReachedPanel : MonoBehaviour
    {
        [SerializeField] private UIFade panel;
        [SerializeField] private WorldScroller world;
        [SerializeField] private ScoreManager score;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text scoreText;

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
            scoreText.SetText("SCORE: {0}", score.Score);
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
}
