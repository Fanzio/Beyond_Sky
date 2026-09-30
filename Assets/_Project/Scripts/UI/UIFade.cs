// Mostra e nasconde un pannello con una dissolvenza e ne regola i clic.
using UnityEngine;

namespace BeyondSky.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFade : MonoBehaviour
    {
        [SerializeField] private float duration = 0.25f;
        private CanvasGroup group;
        private bool visible;
        public bool IsVisible
        {
            get
            {
                return visible;
            }
        }

        public float Duration
        {
            get
            {
                return duration;
            }
        }

        public void Show()
        {
            group = GetComponent<CanvasGroup>();
            if (!gameObject.activeSelf)
            {
                group.alpha = 0f;
            }
            visible = true;
            gameObject.SetActive(true);
            group.blocksRaycasts = true;
            group.interactable = false;
        }

        public void Hide()
        {
            visible = false;
            group = GetComponent<CanvasGroup>();
            group.interactable = false;
        }

        public void HideImmediately()
        {
            visible = false;
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }
            float target = 0f;
            if (visible)
            {
                target = 1f;
            }
            group.alpha = Mathf.MoveTowards(
                group.alpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));
            if (visible && group.alpha >= 1f)
            {
                group.interactable = true;
            }
            if (!visible && group.alpha <= 0f)
            {
                HideImmediately();
            }
        }
    }
}
