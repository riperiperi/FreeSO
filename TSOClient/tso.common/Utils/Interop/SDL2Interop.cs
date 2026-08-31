using System.Runtime.InteropServices;

namespace FSO.Common.Utils.Interop
{
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
        public delegate int d_sdl_getwindowdisplayindex(IntPtr window);
        public static d_sdl_getwindowdisplayindex GetSize = FuncLoader.LoadFunction<d_sdl_getwindowdisplayindex>(NativeLibrary, "SDL_GetWindowDisplayIndex");

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sdl_getdisplaydpi(IntPtr window, out float ddpi, out float hdpi, out float vdpi);
        public static d_sdl_getdisplaydpi GetDisplayDpi = FuncLoader.LoadFunction<d_sdl_getdisplaydpi>(NativeLibrary, "SDL_GetDisplayDPI");
    }
}
