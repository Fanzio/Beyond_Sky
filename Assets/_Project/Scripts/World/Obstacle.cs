using UnityEngine;

// Hides destroyed obstacles so their chunk can restore and reuse them later.
public class Obstacle : MonoBehaviour
{
    private bool isDestroyed;

    public void DestroyObstacle()
    {
        if (isDestroyed == false)
        {
            AudioSignals.Play(GameSound.ObstacleDestroyed);
        }
        isDestroyed = true;
        gameObject.SetActive(false);
    }

    public void ResetObstacle()
    {
        isDestroyed = false;
        gameObject.SetActive(true);
    }
}
