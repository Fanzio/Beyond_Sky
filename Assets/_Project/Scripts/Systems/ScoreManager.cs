using UnityEngine;

// Score is pure distance. The floor multiplier is deliberately NOT applied here.
//
// It multiplies the coins you pick up instead, so climbing a floor changes what your
// choices are worth rather than inflating a number that ticks up on its own. A coin in
// the city is worth 1, the same coin in space is worth 5.
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

    private void Update()
    {
        float distance = worldScroller.DistanceTravelled;
        float travelled = distance - lastDistance;
        lastDistance = distance;

        score = score + travelled * pointsPerUnit;
    }
    public void AddCoin(int amount)
    {
        runCoins = runCoins + amount * floorManager.CoinMultiplier;
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
    public void ResetScore()
    {
        score = 0f;
        runCoins = 0;
        lastDistance = worldScroller.DistanceTravelled;
    }
}