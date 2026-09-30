// Attiva il fondale corrispondente al piano corrente.
using UnityEngine;

namespace BeyondSky.World
{
    [System.Serializable]
    public class FloorBackground
    {
        [SerializeField] private ParallaxLayer farLayer;
        [SerializeField] private ParallaxLayer nearLayer;

        public ParallaxLayer FarLayer
        {
            get { return farLayer; }
        }

        public ParallaxLayer NearLayer
        {
            get { return nearLayer; }
        }
    }

    public class BackgroundSwitcher : MonoBehaviour
    {
        [Header("Floors - same order as FloorManager")]
        [SerializeField] private FloorBackground[] floors;

        public void ShowFloor(int floorIndex)
        {
            for (int i = 0; i < floors.Length; i++)
            {
                bool visible = i == floorIndex;
                floors[i].FarLayer.SetVisible(visible);
                floors[i].NearLayer.SetVisible(visible);
            }
        }

        public void CaptureVerticalStart(int floorIndex)
        {
            floors[floorIndex].FarLayer.CaptureVerticalStart();
            floors[floorIndex].NearLayer.CaptureVerticalStart();
        }

        public void SetVerticalDisplacement(int floorIndex, float distance)
        {
            floors[floorIndex].FarLayer.SetVerticalDisplacement(distance);
            floors[floorIndex].NearLayer.SetVerticalDisplacement(distance);
        }

        private void Start()
        {
            ShowFloor(0);
        }
    }
}
