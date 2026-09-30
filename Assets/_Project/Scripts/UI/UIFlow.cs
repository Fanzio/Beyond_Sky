// Gestisce menu, pausa, tutorial, risultati, vittoria e passaggi tra le scene.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BeyondSky.Audio;
using BeyondSky.Shop;
using BeyondSky.Systems;
using BeyondSky.World;

namespace BeyondSky.UI
{
    public class UIFlow : MonoBehaviour
    {
        [SerializeField] private bool mainMenu;
        [SerializeField] private GameManager game;
        [SerializeField] private ScoreManager score;
        [SerializeField] private WorldScroller world;
        [SerializeField] private GameObject hud;
        [SerializeField] private UIFade pausePanel;
        [SerializeField] private UIFade resultsPanel;
        [SerializeField] private UIFade settingsPanel;
        [SerializeField] private UIFade confirmPanel;
        [SerializeField] private UIFade curtain;
        [SerializeField] private ShopPanel shopPanel;
        [SerializeField] private SpaceReachedPanel spaceReachedPanel;
        [SerializeField] private UIFade tutorialPanel;
        [SerializeField] private TMP_Text resultDistance;
        [SerializeField] private TMP_Text resultScore;
        [SerializeField] private TMP_Text resultBest;
        [SerializeField] private TMP_Text resultCoins;
        [SerializeField] private GameObject newRecord;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button resultsMenuButton;
        [SerializeField] private TMP_Text menuBest;
        [SerializeField] private TMP_Text menuCoins;

        [Header("Confirmation texts - assign in Game and MainMenu")]
        [SerializeField] private TMP_Text confirmationTitle;
        [SerializeField] private TMP_Text confirmationMessage;
        [SerializeField] private TMP_Text confirmationButtonText;
        private bool paused;
        private bool resultsShown;
        private float resumeAt = -1f;
        private float loadAt;
        private string nextScene = "";
        private bool quit;
        private bool applicationFocused = true;
        private bool applicationPaused;

        private void Start()
        {
            Time.timeScale = 1f;
            RefreshMenu();
            settingsPanel.HideImmediately();
            confirmPanel.HideImmediately();
            if (shopPanel != null)
            {
                shopPanel.HideImmediately();
            }
            if (!mainMenu)
            {
                pausePanel.HideImmediately();
                resultsPanel.HideImmediately();
                if (spaceReachedPanel != null)
                {
                    spaceReachedPanel.HideImmediately();
                }
            }
            CanvasGroup group = curtain.GetComponent<CanvasGroup>();
            curtain.gameObject.SetActive(true);
            group.alpha = 1f;
            group.blocksRaycasts = true;
            curtain.Hide();
            if (!mainMenu && tutorialPanel != null)
            {
                tutorialPanel.HideImmediately();
                if (PlayerPrefs.GetInt("TutorialSeen", 0) == 0)
                {
                    Time.timeScale = 0f;
                    tutorialPanel.Show();
                }
            }
        }

        private void Update()
        {
            if (!mainMenu)
            {
                if (game.IsGameOver && !resultsShown)
                {
                    ShowResults();
                }
                if (resultsShown)
                {
                    playAgainButton.interactable = game.CanRestart;
                    resultsMenuButton.interactable = game.CanRestart;
                }
            }
            if (nextScene != "")
            {
                if (Time.unscaledTime >= loadAt)
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(nextScene);
                }
                return;
            }
            if (resumeAt >= 0f && Time.unscaledTime >= resumeAt)
            {
                resumeAt = -1f;
                paused = false;
                if (!game.IsGameOver && IsSpaceReachedOpen() == false)
                {
                    Time.timeScale = 1f;
                }
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Back();
            }
        }

        public void Pause()
        {
            if (mainMenu || game.IsGameOver || nextScene != "" || IsSpaceReachedOpen() || IsTutorialOpen())
            {
                return;
            }
            resumeAt = -1f;
            paused = true;
            Time.timeScale = 0f;
            pausePanel.Show();
        }

        public void Resume()
        {
            if (!paused || game.IsGameOver || resumeAt >= 0f)
            {
                return;
            }
            pausePanel.Hide();
            resumeAt = Time.unscaledTime + pausePanel.Duration;
        }

        public void ShowSpaceReached()
        {
            if (mainMenu || game.IsGameOver || nextScene != "" || spaceReachedPanel == null)
            {
                return;
            }

            if (spaceReachedPanel.Show())
            {
                Time.timeScale = 0f;
                AudioSignals.Play(GameSound.SpaceReached);
            }
        }

        public void ContinueSpaceReached()
        {
            if (mainMenu || paused || game.IsGameOver || nextScene != "" ||
                resumeAt >= 0f || IsSpaceReachedOpen() == false)
            {
                return;
            }

            spaceReachedPanel.Hide();
            resumeAt = Time.unscaledTime + spaceReachedPanel.Duration;
        }

        private bool IsSpaceReachedOpen()
        {
            return spaceReachedPanel != null && spaceReachedPanel.IsOpen;
        }

        public void OpenShop()
        {
            if (mainMenu && shopPanel != null && nextScene == "")
            {
                shopPanel.Open();
            }
        }

        public void CloseShop()
        {
            if (shopPanel != null)
            {
                shopPanel.Close();
                RefreshMenu();
            }
        }

        public void OpenSettings()
        {
            settingsPanel.Show();
        }

        public void CloseSettings()
        {
            PlayerPrefs.Save();
            settingsPanel.Hide();
        }

        public void AskDelete()
        {
            quit = false;
            SetConfirmation(
                "DELETE SAVED DATA?",
                "This resets your best score, coins and skins.\nYour current run will end.",
                "DELETE");
            confirmPanel.Show();
        }

        private void SetConfirmation(string title, string message, string button)
        {
            confirmationTitle.text = title;
            confirmationMessage.text = message;
            confirmationButtonText.text = button;
        }

        public void CancelConfirmation()
        {
            confirmPanel.Hide();
        }

        public void Confirm()
        {
            if (quit)
            {
                PlayerPrefs.Save();
                Application.Quit();
                confirmPanel.Hide();
                return;
            }
            PlayerPrefs.DeleteKey("BestScore");
            PlayerPrefs.DeleteKey("Coins");
            PlayerPrefs.DeleteKey("OwnedSkins");
            PlayerPrefs.DeleteKey("EquippedSkin");
            PlayerPrefs.Save();
            confirmPanel.Hide();
            RefreshMenu();
            if (!mainMenu)
            {
                Load("MainMenu");
            }
        }

        public void Play()
        {
            Load("Game");
        }

        public void Restart()
        {
            if (!mainMenu && game.IsGameOver && !game.CanRestart)
            {
                return;
            }
            Load("Game");
        }

        public void MainMenu()
        {
            if (!mainMenu && game.IsGameOver && !game.CanRestart)
            {
                return;
            }
            if (!mainMenu && nextScene == "" && IsSpaceReachedOpen())
            {
                score.SaveRunResults();
            }
            Load("MainMenu");
        }

        private void Load(string sceneName)
        {
            if (nextScene != "")
            {
                return;
            }
            PlayerPrefs.Save();
            nextScene = sceneName;
            resumeAt = -1f;
            Time.timeScale = 0f;
            curtain.Show();
            loadAt = Time.unscaledTime + curtain.Duration;
        }

        private void Back()
        {
            if (curtain.gameObject.activeSelf || resumeAt >= 0f || IsSpaceReachedOpen() || IsTutorialOpen())
            {
                return;
            }
            if (confirmPanel.gameObject.activeSelf)
            {
                CancelConfirmation();
                return;
            }
            if (settingsPanel.gameObject.activeSelf)
            {
                CloseSettings();
                return;
            }
            if (shopPanel != null && shopPanel.gameObject.activeSelf)
            {
                CloseShop();
                return;
            }
            if (mainMenu)
            {
                quit = true;
                SetConfirmation("QUIT GAME?", "Do you want to close Beyond Sky?", "QUIT");
                confirmPanel.Show();
            }
            else if (resultsShown)
            {
                MainMenu();
            }
            else if (paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private void ShowResults()
        {
            resultsShown = true;
            resumeAt = -1f;
            pausePanel.HideImmediately();
            settingsPanel.HideImmediately();
            confirmPanel.HideImmediately();
            if (spaceReachedPanel != null)
            {
                spaceReachedPanel.HideImmediately();
            }
            hud.SetActive(false);
            resultDistance.text = "DISTANCE: " + world.DistanceTravelled.ToString("F0") + " m";
            resultScore.text = "SCORE: " + score.Score;
            resultBest.text = "BEST SCORE: " + score.BestScore;
            resultCoins.text = "COINS: +" + score.RunCoins;
            newRecord.SetActive(game.IsNewRecord);
            resultsPanel.Show();
            if (PlayerPrefs.GetInt("VibrationEnabled", 1) == 1 && Application.platform == RuntimePlatform.Android)
            {
                Handheld.Vibrate();
            }
        }

        private void RefreshMenu()
        {
            if (mainMenu)
            {
                menuBest.SetText("BEST SCORE: {0}", PlayerPrefs.GetInt("BestScore", 0));
                menuCoins.SetText("{0}", PlayerPrefs.GetInt("Coins", 0));
            }
        }

        private bool IsTutorialOpen()
        {
            return tutorialPanel != null && tutorialPanel.IsVisible;
        }

        public void CloseTutorial()
        {
            if (!IsTutorialOpen() || resumeAt >= 0f)
            {
                return;
            }
            PlayerPrefs.SetInt("TutorialSeen", 1);
            PlayerPrefs.Save();
            tutorialPanel.Hide();
            resumeAt = Time.unscaledTime + tutorialPanel.Duration;
        }

        private void OnApplicationPause(bool value)
        {
            applicationPaused = value;
            PauseWhenInactive();
        }

        private void OnApplicationFocus(bool focused)
        {
            applicationFocused = focused;
            PauseWhenInactive();
        }

        private void PauseWhenInactive()
        {
            if (applicationPaused || !applicationFocused)
            {
                if (resumeAt >= 0f)
                {
                    resumeAt = -1f;
                }
                Pause();
            }
        }

        private void LateUpdate()
        {
            int timeout = SleepTimeout.SystemSetting;
            if (!mainMenu && !applicationPaused && applicationFocused &&
                !game.IsGameOver && Time.timeScale > 0f && nextScene == "")
            {
                timeout = SleepTimeout.NeverSleep;
            }
            if (Screen.sleepTimeout != timeout)
            {
                Screen.sleepTimeout = timeout;
            }
        }
    }
}
