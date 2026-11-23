using UnityEngine;
#if TM_HAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TripMate
{
    /// <summary>
    /// 同時支援舊版 Input Manager 與新版 Input System，專案用哪一種都能直接跑。
    /// TM_HAS_INPUTSYSTEM 由 TripMate.Runtime.asmdef 在「有安裝 Input System 套件」時自動定義。
    /// </summary>
    public static class InputCompat
    {
        public static Vector2 Move()
        {
#if TM_HAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var v = Vector2.zero;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
            }
            var g = Gamepad.current;
            if (g != null) v += g.leftStick.ReadValue();
            return Vector2.ClampMagnitude(v, 1f);
#else
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
        }

        /// <summary>按住中：shift</summary>
        public static bool Held(string key)
        {
#if TM_HAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var k = Keyboard.current;
            if (k == null) return false;
            if (key == "shift") return k.leftShiftKey.isPressed || k.rightShiftKey.isPressed || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
            return false;
#else
            if (key == "shift") return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            return false;
#endif
        }

        /// <summary>按住滑鼠（左鍵或右鍵）拖曳時的移動量（像素），沒按住時為 0。</summary>
        public static Vector2 MouseDrag()
        {
#if TM_HAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var m = Mouse.current;
            if (m == null || !(m.leftButton.isPressed || m.rightButton.isPressed)) return Vector2.zero;
            return m.delta.ReadValue();
#else
            if (!(Input.GetMouseButton(0) || Input.GetMouseButton(1))) return Vector2.zero;
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f; // 舊版的軸值約是像素 × 0.1
#endif
        }

        /// <summary>右搖桿（手把轉鏡頭）。</summary>
        public static Vector2 LookStick()
        {
#if TM_HAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
#else
            return Vector2.zero;
#endif
        }

        /// <summary>滑鼠滾輪：往前為正</summary>
        public static float Scroll()
        {
#if TM_HAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        /// <summary>按鍵名稱：tab, space, q, e, h, r, 1, 2, 3, 4, click</summary>
        public static bool Down(string key)
        {
#if TM_HAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case "tab": return k.tabKey.wasPressedThisFrame;
                case "space": return k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
                case "q": return k.qKey.wasPressedThisFrame;
                case "e": return k.eKey.wasPressedThisFrame;
                case "h": return k.hKey.wasPressedThisFrame;
                case "1": return k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame;
                case "2": return k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame;
                case "3": return k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame;
                case "4": return k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame;
                case "r": return k.rKey.wasPressedThisFrame;
                case "click": return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            }
            return false;
#else
            switch (key)
            {
                case "tab": return Input.GetKeyDown(KeyCode.Tab);
                case "space": return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
                case "q": return Input.GetKeyDown(KeyCode.Q);
                case "e": return Input.GetKeyDown(KeyCode.E);
                case "h": return Input.GetKeyDown(KeyCode.H);
                case "1": return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                case "2": return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
                case "3": return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
                case "4": return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
                case "r": return Input.GetKeyDown(KeyCode.R);
                case "click": return Input.GetMouseButtonDown(0);
            }
            return false;
#endif
        }
    }
}
