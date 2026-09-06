using UnityEngine;

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

// Keeps one prebuilt pair of parallax layers per floor and changes visibility without
// rebuilding grids during gameplay.
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

    public void SetExtraVerticalFactor(float extra)
    {
        for (int i = 0; i < floors.Length; i++)
        {
            floors[i].FarLayer.SetExtraVerticalFactor(extra);
            floors[i].NearLayer.SetExtraVerticalFactor(extra);
        }
    }

    private void Start()
    {
        ShowFloor(0);
    }
}
