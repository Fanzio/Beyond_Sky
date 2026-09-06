using UnityEngine;

[System.Serializable]
public class Floor
{
    [SerializeField] private string floorName = "Floor";
    [SerializeField] private int coinMultiplier = 1;
    [SerializeField] private Color backgroundColor = new Color(0.18f, 0.36f, 0.54f);

    public string FloorName
    {
        get { return floorName; }
    }

    public int CoinMultiplier
    {
        get { return coinMultiplier; }
    }

    public Color BackgroundColor
    {
        get { return backgroundColor; }
    }

    public Floor(string name, int multiplier, Color color)
    {
        floorName = name;
        coinMultiplier = multiplier;
        backgroundColor = color;
    }
}

// Advances floors only after a boss defeat and restarts the floor-distance counter so
// every floor requires the same configured travel before its next boss.
public class FloorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private Camera targetCamera;

    [Header("Boss")]
    [SerializeField] private float distanceToBoss = 500f;

    [Header("Display")]
    [SerializeField] private float metresPerUnit = 0.8f;
    [SerializeField] private float colourBlendSpeed = 0.7f;

    [Header("Floors - lowest first")]
    [SerializeField]
    private Floor[] floors = new Floor[]
    {
        new Floor("Citta", 1, new Color(0.18f, 0.36f, 0.54f)),
        new Floor("Cielo", 3, new Color(0.25f, 0.55f, 0.85f)),
        new Floor("Spazio", 5, new Color(0.04f, 0.03f, 0.12f))
    };

    public event System.Action<int> FloorAdvanced;

    private int currentFloorIndex;
    private float floorStartDistance;

    public int CurrentFloorIndex
    {
        get { return currentFloorIndex; }
    }

    public string CurrentFloorName
    {
        get { return floors[currentFloorIndex].FloorName; }
    }

    public int CoinMultiplier
    {
        get { return floors[currentFloorIndex].CoinMultiplier; }
    }

    public float DistanceInFloor
    {
        get { return worldScroller.DistanceTravelled - floorStartDistance; }
    }

    public float DistanceToBoss
    {
        get { return Mathf.Max(0f, distanceToBoss - DistanceInFloor); }
    }

    public bool IsBossDue
    {
        get { return DistanceInFloor >= distanceToBoss; }
    }

    public bool IsTopFloor
    {
        get { return currentFloorIndex >= floors.Length - 1; }
    }

    public float DisplayedAltitude
    {
        get { return worldScroller.DistanceTravelled * metresPerUnit; }
    }

    public void NotifyBossDefeated()
    {
        ResetFloorDistance();

        if (IsTopFloor)
        {
            return;
        }

        currentFloorIndex = currentFloorIndex + 1;

        if (FloorAdvanced != null)
        {
            FloorAdvanced.Invoke(currentFloorIndex);
        }
    }

    public void ResetFloorDistance()
    {
        floorStartDistance = worldScroller.DistanceTravelled;
    }

    private void Start()
    {
        currentFloorIndex = 0;
        floorStartDistance = worldScroller.DistanceTravelled;
        targetCamera.backgroundColor = floors[0].BackgroundColor;
    }

    private void Update()
    {
        BlendBackground(Time.deltaTime);
    }

    private void BlendBackground(float deltaTime)
    {
        Color target = floors[currentFloorIndex].BackgroundColor;

        targetCamera.backgroundColor = Color.Lerp(
            targetCamera.backgroundColor, target, colourBlendSpeed * deltaTime);
    }
}