using UnityEngine;

namespace MafiaUnity
{
    /// <summary>
    /// Platform-neutral virtual input state used by the Android touch UI.
    /// Desktop keyboard/mouse input remains unchanged.
    /// </summary>
    public static class MobileInputState
    {
        public static Vector2 Move;
        public static Vector2 Look;
        public static bool RunHeld;
        public static bool CrouchHeld;
        public static bool UsePressed;

        public static void ResetFrame()
        {
            Look = Vector2.zero;
            UsePressed = false;
        }
    }
}
