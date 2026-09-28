namespace EFT.UI
{
    public enum ECursorType { Idle, Invisible }

    public static class CursorSwitcher
    {
        public sealed class CursorData;
        public static ECursorType LastCursor;
        public static ECursorType PreviousType => LastCursor;
        public static bool LastVisible;
        public static UnityEngine.FullScreenMode LastFullscreenMode;

        public static void SetCursor(ECursorType type) => LastCursor = type;
        public static void SetCursor(CursorData data) => throw new Exception("Wrong cursor overload");
        public static void SetCursorLockMode(bool visible, UnityEngine.FullScreenMode mode)
        {
            LastVisible = visible;
            LastFullscreenMode = mode;
            UnityEngine.Cursor.lockState = visible ? UnityEngine.CursorLockMode.None : UnityEngine.CursorLockMode.Locked;
        }
        public static void SetCursorLockMode(bool visible) => throw new Exception("Wrong lock overload");
    }

    public static class UnrelatedCursor
    {
        public static void SetCursor(ECursorType type) => throw new Exception("Wrong cursor type");
        public static void SetCursor(string name) => throw new Exception("Wrong cursor type");
    }

    public class InstanceCursor
    {
        public void SetCursor(ECursorType type) => throw new Exception("Instance cursor method");
        public void SetCursorLockMode(bool visible, UnityEngine.FullScreenMode mode) => throw new Exception("Instance lock method");
    }
}

namespace UnityEngine
{
    public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, Windowed }
}

namespace SPT.Reflection.Utils
{
    public static class PatchConstants
    {
        public static Type[] EftTypes =>
        [
            typeof(EFT.UI.UnrelatedCursor),
            typeof(EFT.UI.InstanceCursor),
            typeof(EFT.UI.CursorSwitcher)
        ];
    }
}
