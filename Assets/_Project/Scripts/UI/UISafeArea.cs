// Adatta l’area della UI alla parte utilizzabile dello schermo.
using UnityEngine;

namespace BeyondSky.UI
{
    public class UISafeArea : MonoBehaviour
    {
        private RectTransform area;
        private Rect previous;
        private int previousWidth;
        private int previousHeight;

        private void OnEnable()
        {
            area = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (previous != Screen.safeArea || previousWidth != Screen.width || previousHeight != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }
            previous = Screen.safeArea;
            previousWidth = Screen.width;
            previousHeight = Screen.height;
            area.anchorMin = new Vector2(previous.xMin / Screen.width, previous.yMin / Screen.height);
            area.anchorMax = new Vector2(previous.xMax / Screen.width, previous.yMax / Screen.height);
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
        }
    }
}
