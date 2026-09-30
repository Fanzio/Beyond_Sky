// Calcola punti e monete della run e ne salva i risultati una sola volta.
using UnityEngine;
using BeyondSky.Audio;
using BeyondSky.World;

namespace BeyondSky.Systems
{
    public class ScoreManager : MonoBehaviour
    {
        private const string BestScoreKey = "BestScore";
        private const string CoinsKey = "Coins";

        [Header("References")]
        [SerializeField] private WorldScroller worldScroller;
        [SerializeField] private FloorManager floorManager;

        [Header("Scoring")]
        [SerializeField] private float pointsPerUnit = 1f;
        [SerializeField] private int pointsPerCoin = 5;

        private float score;
        private float lastDistance;
        private int runCoins;
        private bool resultsSaved;

        public int Score
        {
            get { return Mathf.FloorToInt(score); }
        }

        public int BestScore
        {
            get { return PlayerPrefs.GetInt(BestScoreKey, 0); }
        }

        public int RunCoins
        {
            get { return runCoins; }
        }

        public int TotalCoins
        {
            get { return PlayerPrefs.GetInt(CoinsKey, 0); }
        }

        public void AddCoin(int amount)
        {
            int earnedCoins = amount * floorManager.CoinMultiplier;
            runCoins = runCoins + earnedCoins;
            score = score + earnedCoins * pointsPerCoin;
            AudioSignals.Play(GameSound.Coin);
        }

        public bool SaveRunResults()
        {
            if (resultsSaved)
            {
                return false;
            }

            UpdateScore();
            resultsSaved = true;
            bool newRecord = Score > BestScore;
            if (newRecord)
            {
                PlayerPrefs.SetInt(BestScoreKey, Score);
            }
            PlayerPrefs.SetInt(CoinsKey, TotalCoins + runCoins);
            PlayerPrefs.Save();
            return newRecord;
        }

        private void Update()
        {
            UpdateScore();
        }

        private void UpdateScore()
        {
            float currentDistance = worldScroller.DistanceTravelled;
            float distanceTravelledThisFrame = currentDistance - lastDistance;
            lastDistance = currentDistance;

            score = score + distanceTravelledThisFrame * pointsPerUnit;
        }
    }
}
