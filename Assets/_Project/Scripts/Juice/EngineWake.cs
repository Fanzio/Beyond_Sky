// Visualizza la scia del motore e ne aggiorna l’aspetto durante il volo.
using UnityEngine;
using BeyondSky.Systems;

namespace BeyondSky.Juice
{
    [DefaultExecutionOrder(50)]
    public class EngineWake : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private SpeedManager speedManager;
        [SerializeField] private Transform emitter;
        [SerializeField] private LineRenderer line;
        [SerializeField] private float lifetime = 0.3f;
        [SerializeField] private float sampleInterval = 0.02f;
        [SerializeField] private float driftFactor = 0.4f;
        private Vector3[] points = new Vector3[24];
        private float[] ages = new float[24];
        private int count;
        private float sampleTimer;

        private void Awake()
        {
            line.positionCount = 0;
        }

        private void LateUpdate()
        {
            if (gameManager.IsGameOver)
            {
                Clear();
                return;
            }
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                points[i].x = points[i].x - speedManager.CurrentSpeed * driftFactor * deltaTime;
                ages[i] = ages[i] + deltaTime;
            }
            while (count > 0 && ages[0] >= lifetime)
            {
                RemoveOldest();
            }
            sampleTimer = sampleTimer + deltaTime;
            if (count == 0 || sampleTimer >= sampleInterval)
            {
                sampleTimer = 0f;
                if (count == points.Length)
                {
                    RemoveOldest();
                }
                points[count] = emitter.position;
                ages[count] = 0f;
                count = count + 1;
            }
            line.positionCount = count + 1;
            for (int i = 0; i < count; i++)
            {
                line.SetPosition(i, points[i]);
            }
            line.SetPosition(count, emitter.position);
        }

        private void RemoveOldest()
        {
            for (int i = 1; i < count; i++)
            {
                points[i - 1] = points[i];
                ages[i - 1] = ages[i];
            }
            count = count - 1;
        }

        private void Clear()
        {
            count = 0;
            sampleTimer = 0f;
            line.positionCount = 0;
        }

        private void OnDisable()
        {
            if (line != null)
            {
                Clear();
            }
        }
    }
}
