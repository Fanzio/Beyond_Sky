using UnityEngine;

[System.Serializable]
public class FloorBackground
{
    public ParallaxLayer farLayer;
    public ParallaxLayer nearLayer;
}

// Holds one pair of parallax layers per floor and shows only the current pair.
//
// Every grid is built once at start and then only hidden or shown. Rebuilding a grid mid
// run would mean instantiating during gameplay, which is exactly what the pooling rule
// exists to prevent. Seventy-odd transforms is nothing; a hitch during the climb is not.
public class BackgroundSwitcher : MonoBehaviour
{
    [Header("Floors - same order as FloorManager")]
    [SerializeField] private FloorBackground[] floors;

    private void Start()
    {
        ShowFloor(0);
    }
    public void ShowFloor(int floorIndex)
    {
        for (int i = 0; i < floors.Length; i++)
        {
            bool visible = i == floorIndex;
            floors[i].farLayer.SetVisible(visible);
            floors[i].nearLayer.SetVisible(visible);
        }
    }
    public void SetExtraVerticalFactor(float extra)
    {
        for (int i = 0; i < floors.Length; i++)
        {
            floors[i].farLayer.SetExtraVerticalFactor(extra);
            floors[i].nearLayer.SetExtraVerticalFactor(extra);
        }
    }
}
