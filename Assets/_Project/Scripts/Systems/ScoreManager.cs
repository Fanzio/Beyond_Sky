using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private const string BestScoreKey = "BestScore";
    private const string CoinsKey = "Coins";

    [Header("References")]
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private FloorManager floorManager;

    [Header("Scoring")]
    [SerializeField] private float pointsPerUnit = 1f;

    private float score;
    private float lastDistance;
    private int runCoins;

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
        runCoins = runCoins + amount * floorManager.CoinMultiplier;
        AudioSignals.Play(GameSound.Coin);
    }

    public void SaveCoins()
    {
        PlayerPrefs.SetInt(CoinsKey, TotalCoins + runCoins);
        PlayerPrefs.Save();
    }

    public bool SaveIfBest()
    {
        if (Score <= BestScore)
        {
            return false;
        }

        PlayerPrefs.SetInt(BestScoreKey, Score);
        PlayerPrefs.Save();
        return true;
    }

    private void Update()
    {
        float currentDistance = worldScroller.DistanceTravelled;
        float distanceTravelledThisFrame = currentDistance - lastDistance;
        lastDistance = currentDistance;

        score = score + distanceTravelledThisFrame * pointsPerUnit;
    }
}
