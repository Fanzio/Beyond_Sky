using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Three zones. The left part of the screen flies, the right part shoots.
//
//   Left area, upper half  -> climb
//   Left area, lower half  -> descend
//   Right area             -> one tap, one bullet
//
// Splitting the hands means steering and firing never compete. A fire button placed
// inside the steering area would stop the plane every time you shot, which is the last
// thing you want during a boss fight.
//
// A double tap inside a steering zone is the dodge. The FIRST tap still steers normally,
// because waiting to find out whether a tap is a double would add lag to every input.
//
// Screen coordinates have their origin at the bottom left, so a high y is the top.
public class PlaneInput : MonoBehaviour
{
    [Header("Zones")]
    [SerializeField] private float steeringWidthFraction = 0.58f;
    [SerializeField] private float verticalSplitFraction = 0.5f;

    [Header("Double tap")]
    [SerializeField] private float doubleTapWindow = 0.28f;

    private float verticalInput;
    private float lastTapTime = -10f;
    private float lastTapZone;
    private float pendingDoubleTap;
    private bool pendingFire;
    private bool isLocked;

    public float VerticalInput
    {
        get { return verticalInput; }
    }

    public float ConsumeDoubleTap()
    {
        float result = pendingDoubleTap;
        pendingDoubleTap = 0f;
        return result;
    }
    public bool ConsumeFire()
    {
        bool result = pendingFire;
        pendingFire = false;
        return result;
    }
    private void Update()
    {
        if (isLocked)
        {
            verticalInput = 0f;
            pendingFire = false;
            pendingDoubleTap = 0f;
            return;
        }

        verticalInput = ReadHeldSteering();
        ReadPresses();
    }

    // Locking here switches off steering, firing and the dodge in one place, so no other
    // script has to know that a cutscene state exists.
    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }
    private float ReadHeldSteering()
    {
        float value = 0f;

        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed == false)
                {
                    continue;
                }
                value = value + SteeringValue(touch.position.ReadValue());
            }
        }
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                value = value + SteeringValue(Mouse.current.position.ReadValue());
            }
        }
        return Mathf.Clamp(value, -1f, 1f);
    }
    private void ReadPresses()
    {
        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame == false)
                {
                    continue;
                }
                RegisterPress(touch.position.ReadValue());
            }
        }
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                RegisterPress(Mouse.current.position.ReadValue());
            }
        }
    }
    private void RegisterPress(Vector2 position)
    {
        if (IsInSteeringArea(position.x) == false)
        {
            pendingFire = true;
            return;
        }

        float zone = SteeringValue(position);
        bool inTime = Time.unscaledTime - lastTapTime <= doubleTapWindow;
        bool sameZone = Mathf.Approximately(zone, lastTapZone);

        if (inTime && sameZone)
        {
            pendingDoubleTap = zone;
            lastTapTime = -10f;
            return;
        }
        lastTapTime = Time.unscaledTime;
        lastTapZone = zone;
    }
    private bool IsInSteeringArea(float screenX)
    {
        return screenX < Screen.width * steeringWidthFraction;
    }
    private float SteeringValue(Vector2 position)
    {
        if (IsInSteeringArea(position.x) == false)
        {
            return 0f;
        }
        if (position.y > Screen.height * verticalSplitFraction)
        {
            return 1f;
        }
        return -1f;
    }
}