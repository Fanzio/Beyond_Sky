// Visualizza l’esplosione del player alla fine della partita.
using UnityEngine;

namespace BeyondSky.Juice
{
    public class PlayerDeathBurst : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private float startSize = 0.5f;
        [SerializeField] private float endSize = 2f;
        private Vector3 baseScale;
        private float timer;
        private bool playing;
        private bool applicationPaused;

        private void Awake()
        {
            baseScale = transform.localScale;
            visual.enabled = false;
        }

        public void PlayAt(Vector3 position)
        {
            if (isActiveAndEnabled == false)
            {
                return;
            }
            transform.position = position;
            timer = 0f;
            playing = true;
            visual.color = Color.white;
            visual.enabled = true;
            transform.localScale = baseScale * startSize;
        }

        private void Update()
        {
            if (playing == false || applicationPaused)
            {
                return;
            }
            timer = timer + Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
            float eased = 1f - (1f - progress) * (1f - progress);
            transform.localScale = baseScale * Mathf.Lerp(startSize, endSize, eased);
            Color color = Color.white;
            color.a = 1f - Mathf.InverseLerp(0.55f, 1f, progress);
            visual.color = color;
            if (progress >= 1f)
            {
                playing = false;
                visual.enabled = false;
                transform.localScale = baseScale;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
        }

        private void OnDisable()
        {
            playing = false;
            if (visual != null)
            {
                visual.enabled = false;
            }
        }
    }
}
