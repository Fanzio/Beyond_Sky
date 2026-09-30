// Interpreta mouse e touch, distinguendo i comandi di gioco dai tocchi sulla UI.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BeyondSky.Input
{
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
        private List<RaycastResult> uiHits = new List<RaycastResult>(16);
        private List<int> uiTouches = new List<int>(10);
        private bool uiMouse;
        private PointerEventData pointer;
        private EventSystem pointerSystem;
        public float VerticalInput
        {
            get
            {
                return verticalInput;
            }
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
            TrackUIContacts();
            if (isLocked || Time.timeScale == 0f)
            {
                verticalInput = 0f;
                pendingFire = false;
                pendingDoubleTap = 0f;
                lastTapTime = -10f;
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
                    if (uiTouches.Contains(touch.touchId.ReadValue()))
                    {
                        continue;
                    }
                    value = value + SteeringValue(touch.position.ReadValue());
                }
            }
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.isPressed && !uiMouse)
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
                    if (uiTouches.Contains(touch.touchId.ReadValue()))
                    {
                        continue;
                    }
                    RegisterPress(touch.position.ReadValue());
                }
            }
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && !uiMouse)
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

        private void TrackUIContacts()
        {
            RemoveReleasedUIContacts();
            TrackUITouches();
            TrackUIMouse();
        }

        private void RemoveReleasedUIContacts()
        {
            for (int i = uiTouches.Count - 1; i >= 0; i--)
            {
                if (IsTouchHeld(uiTouches[i]) == false)
                {
                    uiTouches.RemoveAt(i);
                }
            }
        }

        private bool IsTouchHeld(int touchId)
        {
            if (Touchscreen.current == null)
            {
                return false;
            }
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed && touch.touchId.ReadValue() == touchId)
                {
                    return true;
                }
            }
            return false;
        }

        private void TrackUITouches()
        {
            if (Touchscreen.current == null)
            {
                return;
            }
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed == false && touch.press.wasPressedThisFrame == false)
                {
                    continue;
                }
                int touchId = touch.touchId.ReadValue();
                if (uiTouches.Contains(touchId))
                {
                    continue;
                }
                if (Time.timeScale == 0f || IsOverUI(touch.position.ReadValue()))
                {
                    uiTouches.Add(touchId);
                }
            }
        }

        private void TrackUIMouse()
        {
            if (Mouse.current == null)
            {
                uiMouse = false;
                return;
            }
            if (Mouse.current.leftButton.isPressed == false &&
                Mouse.current.leftButton.wasPressedThisFrame == false)
            {
                uiMouse = false;
                return;
            }
            if (uiMouse)
            {
                return;
            }
            if (Time.timeScale == 0f || IsOverUI(Mouse.current.position.ReadValue()))
            {
                uiMouse = true;
            }
        }

        private bool IsOverUI(Vector2 position)
        {
            uiHits.Clear();
            EventSystem currentSystem = EventSystem.current;
            if (currentSystem == null)
            {
                pointerSystem = null;
                pointer = null;
                return false;
            }
            if (pointer == null || pointerSystem != currentSystem)
            {
                pointerSystem = currentSystem;
                pointer = new PointerEventData(pointerSystem);
            }
            pointer.Reset();
            pointer.position = position;
            pointerSystem.RaycastAll(pointer, uiHits);
            for (int i = 0; i < uiHits.Count; i++)
            {
                if (uiHits[i].module is GraphicRaycaster)
                {
                    return true;
                }
            }
            return false;
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
}
