using UnityEngine;

namespace MafiaUnity
{
    /// <summary>
    /// Platform-neutral virtual input state used by the Android touch UI.
    /// Desktop keyboard/mouse input remains unchanged.
    /// One-shot actions are consumed by gameplay code so they cannot be lost
    /// between Update and FixedUpdate.
    /// </summary>
    public static class MobileInputState
    {
        public static Vector2 Move;
        public static Vector2 Look;
        public static bool RunHeld;
        public static bool CrouchHeld;

        private static bool usePressed;

        public static void AddLook(Vector2 delta)
        {
            Look += delta;
        }

        public static Vector2 ConsumeLook()
        {
            var value = Look;
            Look = Vector2.zero;
            return value;
        }

        public static void PressUse()
        {
            usePressed = true;
        }

        public static bool ConsumeUse()
        {
            if (!usePressed)
                return false;

            usePressed = false;
            return true;
        }

        public static void ResetAll()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            RunHeld = false;
            CrouchHeld = false;
            usePressed = false;
        }
    }
}