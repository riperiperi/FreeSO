using System.Runtime.InteropServices;

namespace FSO.Common.Utils.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SDL2DisplayMode
    {
        public uint Format;
        public int Width;
        public int Height;
        public int RefreshRate;
        public nint DriverData;
    }

    public class SDL2Interop
    {
        public static IntPtr NativeLibrary = GetNativeLibrary();

        private static IntPtr GetNativeLibrary()
        {
            if (OperatingSystem.IsWindows())
                return FuncLoader.LoadLibraryExt("SDL2.dll");
            else if (OperatingSystem.IsLinux())
                return FuncLoader.LoadLibraryExt("libSDL2-2.0.so.0");
            else if (OperatingSystem.IsMacOS())
                return FuncLoader.LoadLibraryExt("libSDL2-2.0.0.dylib");
            else
                return FuncLoader.LoadLibraryExt("sdl2");
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sdl_getwindowsize(IntPtr window, out int w, out int h);
        public static d_sdl_getwindowsize GetWindowSize = FuncLoader.LoadFunction<d_sdl_getwindowsize>(NativeLibrary, "SDL_GetWindowSize");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sdl_gl_getdrawablesize(IntPtr window, out int w, out int h);
        public static d_sdl_gl_getdrawablesize GetGlDrawableSize = FuncLoader.LoadFunction<d_sdl_gl_getdrawablesize>(NativeLibrary, "SDL_GL_GetDrawableSize");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sdl_getwindowdisplayindex(IntPtr window);
        public static d_sdl_getwindowdisplayindex GetWindowDisplayIndex = FuncLoader.LoadFunction<d_sdl_getwindowdisplayindex>(NativeLibrary, "SDL_GetWindowDisplayIndex");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sdl_getdisplaydpi(int index, out float ddpi, out float hdpi, out float vdpi);
        public static d_sdl_getdisplaydpi GetDisplayDpi = FuncLoader.LoadFunction<d_sdl_getdisplaydpi>(NativeLibrary, "SDL_GetDisplayDPI");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sdl_getcurrentdisplaymode(int index, out SDL2DisplayMode mode);
        public static d_sdl_getcurrentdisplaymode GetCurrentDisplayMode = FuncLoader.LoadFunction<d_sdl_getcurrentdisplaymode>(NativeLibrary, "SDL_GetCurrentDisplayMode");
    }
}
