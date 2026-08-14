using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EscapeTheLava
{
    /// <summary>
    /// Single source of "the player just tapped here", in screen space.
    ///
    /// This project is configured for the Input System package only (<c>activeInputHandler: 1</c>),
    /// so the legacy <c>UnityEngine.Input</c> class is unavailable. <see cref="Pointer"/> covers
    /// mouse, pen and touch with one code path, which is exactly the "clicks/taps" the brief asks for.
    /// </summary>
    public class PointerInput : MonoBehaviour
    {
        /// <summary>Screen-space position of a press that was not consumed by the UI.</summary>
        public event Action<Vector2> Pressed;

        [Tooltip("Ignore presses that land on a UI element, so the restart button cannot also hit a tile.")]
        public bool blockWhenOverUi = true;

        void Update()
        {
            if (!TryReadPress(out Vector2 screenPosition)) return;
            if (blockWhenOverUi && IsOverUi()) return;
            Pressed?.Invoke(screenPosition);
        }

        static bool TryReadPress(out Vector2 screenPosition)
        {
            screenPosition = default;

            // Pointer.current is whichever pointer device was used last (mouse, pen or touchscreen).
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                screenPosition = pointer.position.ReadValue();
                return true;
            }

            // Explicit fallbacks for setups where Pointer.current has not been resolved yet.
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = touch.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }

        static bool IsOverUi()
        {
            EventSystem events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }
    }
}
