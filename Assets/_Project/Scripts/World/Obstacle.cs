// Gestisce danni, distruzione e ripristino di un ostacolo.
using UnityEngine;
using BeyondSky.Audio;

namespace BeyondSky.World
{
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
}
