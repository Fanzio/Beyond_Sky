// Mostra un breve lampo in risposta a un passaggio ravvicinato.
using UnityEngine;
using UnityEngine.UI;
using BeyondSky.Systems;

namespace BeyondSky.Juice
{
    public class CloseCallFlash : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image border;
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private float maximumAlpha = 0.16f;
        private float remaining;

        private void Awake()
        {
            border.raycastTarget = false;
            SetAlpha(0f);
        }

        public void Play()
        {
            if (isActiveAndEnabled)
            {
                remaining = duration;
            }
        }

        private void Update()
        {
            if (Time.timeScale == 0f || gameManager.IsGameOver)
            {
                remaining = 0f;
            }
            if (remaining <= 0f)
            {
                SetAlpha(0f);
                return;
            }
            remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
            float progress = 1f - remaining / Mathf.Max(0.01f, duration);
            SetAlpha(Mathf.Sin(progress * Mathf.PI) * maximumAlpha);
        }

        private void SetAlpha(float alpha)
        {
            Color color = Color.white;
            color.a = alpha;
            border.color = color;
            border.enabled = alpha > 0f;
        }

        private void OnDisable()
        {
            remaining = 0f;
            if (border != null)
            {
                SetAlpha(0f);
            }
        }
    }
}
