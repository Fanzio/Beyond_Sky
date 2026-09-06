using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Owns the run state. Game over freezes scaled gameplay while unscaled restart input
// remains available. Reloading the scene guarantees that every system starts clean.
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
        isNewRecord = scoreManager.SaveIfBest();
        scoreManager.SaveCoins();

        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        if (WasRestartInputPressed())
        {
            Restart();
        }
    }

    private bool WasRestartInputPressed()
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