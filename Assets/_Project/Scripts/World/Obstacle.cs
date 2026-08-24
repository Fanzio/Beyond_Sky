using UnityEngine;

// Marks something the player can crash into and a bullet can destroy.
//
// Destroying hides the object rather than removing it, because chunks are recycled and
// everything inside has to come back intact next time round. Without the reset the world
// would slowly empty out over a long run, and the cause would be hard to spot.
public class Obstacle : MonoBehaviour
{
    private bool isDestroyed;

    public bool IsDestroyed
    {
        get { return isDestroyed; }
    }

    public void DestroyObstacle()
    {
        isDestroyed = true;
        gameObject.SetActive(false);
    }
    public void ResetObstacle()
    {
        isDestroyed = false;
        gameObject.SetActive(true);
    }
}