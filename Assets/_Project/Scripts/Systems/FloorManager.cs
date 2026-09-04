using UnityEngine;

[System.Serializable]
public class Floor
{
    public string floorName = "Floor";
    public int coinMultiplier = 1;
    public Color backgroundColor = new Color(0.18f, 0.36f, 0.54f);
}

// Floors are earned, not reached. Distance no longer promotes the player: it only says
// how close the next boss is, and beating that boss is what moves you up.
//
// This is the difference between a multiplier that arrives because you waited and one
// that arrives because you won. The second is worth far more, and it costs nothing extra
// to build once the boss exists.
//
// The distance counter restarts on every floor, so the boss always comes after the same
// stretch of play no matter how far into the run you are.
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
        new Floor { floorName = "Citta", coinMultiplier = 1,
            backgroundColor = new Color(0.18f, 0.36f, 0.54f) },
        new Floor { floorName = "Cielo", coinMultiplier = 3,
            backgroundColor = new Color(0.25f, 0.55f, 0.85f) },
        new Floor { floorName = "Spazio", coinMultiplier = 5,
            backgroundColor = new Color(0.04f, 0.03f, 0.12f) }
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
        get { return floors[currentFloorIndex].floorName; }
    }
    public int CoinMultiplier
    {
        get { return floors[currentFloorIndex].coinMultiplier; }
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

    private void Start()
    {
        currentFloorIndex = 0;
        floorStartDistance = worldScroller.DistanceTravelled;
        targetCamera.backgroundColor = floors[0].backgroundColor;
    }
    private void Update()
    {
        BlendBackground(Time.deltaTime);
    }

    // Called when a boss goes down. On the top floor there is nowhere left to climb, so
    // the run simply keeps going and the next boss is queued instead.
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

    // Called again when the ascension ends. The floor is promoted at the START of the
    // climb, but the world keeps scrolling throughout it, so those seconds would count
    // against the new floor and bring its boss forward by a good chunk of the distance.
    public void ResetFloorDistance()
    {
        floorStartDistance = worldScroller.DistanceTravelled;
    }
    private void BlendBackground(float deltaTime)
    {
        Color target = floors[currentFloorIndex].backgroundColor;

        targetCamera.backgroundColor = Color.Lerp(
            targetCamera.backgroundColor, target, colourBlendSpeed * deltaTime);
    }
}   