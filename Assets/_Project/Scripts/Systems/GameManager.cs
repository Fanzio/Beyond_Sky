using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Run states: playing, ascending, game over.
//
// Game over freezes the game with timeScale. That stops everything in one line, so no
// other script has to know the state exists. Update still runs while frozen - only
// Time.deltaTime and physics are scaled - so input keeps working.
//
// Restarting simply reloads the scene. It looks blunt but it is the right call here:
// every system starts clean with no reset method to write, and nothing can be forgotten.
// The best score survives because it lives in PlayerPrefs.
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
    public void SetAscending(bool ascending)
    {
        isAscending = ascending;
    }
    public bool IsNewRecord
    {
        get { return isNewRecord; }
    }
    public bool CanRestart
    {
        get { return isGameOver && Time.unscaledTime - gameOverTime >= restartDelay; }
    }
    private void Awake()
    {
        Time.timeScale = 1f;
    }
    private void Update()
    {
        if (CanRestart == false)
        {
            return;
        }
        if (WasTapped())
        {
            Restart();
        }
    }
    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        gameOverTime = Time.unscaledTime;
        isNewRecord = scoreManager.SaveIfBest();
        scoreManager.SaveCoins();

        Time.timeScale = 0f;
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private bool WasTapped()
    {
        if (Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
        }
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
        }
        return false;
    }
}