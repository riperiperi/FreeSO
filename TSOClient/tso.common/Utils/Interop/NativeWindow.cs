namespace FSO.Common.Utils.Interop
{
    public struct FSODisplayInfo(float dpi, int refreshRate = 0)
    {
        public float Dpi = dpi;
        public int RefreshRate = refreshRate;
    }

    public static class NativeWindow
    {
        public static Action<nint, Action<string>> RegisterDragCallback = (window, func) => { };
        public static Func<nint, FSODisplayInfo> GetDisplayInfo = (window) => new FSODisplayInfo(1f);
    }
}
