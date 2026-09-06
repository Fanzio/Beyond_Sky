using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Separates steering and firing so both actions can be used together during combat.
// The first steering tap is immediate; a second tap in the same zone triggers the dodge.
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
