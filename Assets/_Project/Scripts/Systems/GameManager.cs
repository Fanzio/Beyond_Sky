// Gestisce gli stati della partita e salva i risultati alla morte del player.
using UnityEngine;

namespace BeyondSky.Systems
{
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScoreManager scoreManager;

        [Header("Restart")]
        [SerializeField] private float restartDelay = 0.6f;

        private bool isAscending;
        private bool isGameOver;
        private bool isNewRecord;
        private float gameOverTime;

        public bool IsGameOver
        {
            get { return isGameOver; }
        }

        public bool IsAscending
        {
            get { return isAscending; }
        }

        public bool IsNewRecord
        {
            get { return isNewRecord; }
        }

        public bool CanRestart
        {
            get { return isGameOver && Time.unscaledTime - gameOverTime >= restartDelay; }
        }

        public void SetAscending(bool ascending)
        {
            isAscending = ascending;
        }

        public void GameOver()
        {
            if (isGameOver)
            {
                return;
            }

            isGameOver = true;
            gameOverTime = Time.unscaledTime;
            isNewRecord = scoreManager.SaveRunResults();

            Time.timeScale = 0f;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
        }

    }
}
