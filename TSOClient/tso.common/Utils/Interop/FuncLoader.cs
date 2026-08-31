using System.Runtime.InteropServices;

/**
 * From Monogame, used to load SDL2 functions in the same way that it does.
 */
namespace FSO.Common.Utils.Interop
{
    internal class FuncLoader
    {
        private class Windows
        {
            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibraryW(string lpszLib);
        }

        private class Linux
        {
            [DllImport("libdl.so.2")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("libdl.so.2")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }

        private class OSX
        {
            [DllImport("/usr/lib/libSystem.dylib")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("/usr/lib/libSystem.dylib")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }

        private const int RTLD_LAZY = 0x0001;

        public static IntPtr LoadLibraryExt(string libname)
        {
            var ret = IntPtr.Zero;
            var assemblyLocation = Path.GetDirectoryName(System.AppContext.BaseDirectory) ?? "./";

            // Try .NET Framework / mono locations
            if (OperatingSystem.IsMacOS())
            {
                ret = LoadLibrary(Path.Combine(assemblyLocation, libname));

                // Look in Frameworks for .app bundles
                if (ret == IntPtr.Zero)
                    ret = LoadLibrary(Path.Combine(assemblyLocation, "..", "Frameworks", libname));
            }
            else
            {
                if (Environment.Is64BitProcess)
                    ret = LoadLibrary(Path.Combine(assemblyLocation, "x64", libname));
                else
                    ret = LoadLibrary(Path.Combine(assemblyLocation, "x86", libname));
            }

            // Try .NET Core development locations
            if (ret == IntPtr.Zero)
                ret = LoadLibrary(Path.Combine(assemblyLocation, "runtimes", Rid, "native", libname));

            // Try current folder (.NET Core will copy it there after publish)
            if (ret == IntPtr.Zero)
                ret = LoadLibrary(Path.Combine(assemblyLocation, libname));

            // Try alternate way of checking current folder
            // assemblyLocation is null if we are inside macOS app bundle
            if (ret == IntPtr.Zero)
                ret = LoadLibrary(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, libname));

            // Try loading system library
            if (ret == IntPtr.Zero)
                ret = LoadLibrary(libname);

            // Welp, all failed, PANIC!!!
            if (ret == IntPtr.Zero)
                throw new Exception("Failed to load library: " + libname);

            return ret;
        }

        public static IntPtr LoadLibrary(string libname)
        {
            if (OperatingSystem.IsWindows())
                return Windows.LoadLibraryW(libname);

            if (OperatingSystem.IsMacOS())
                return OSX.dlopen(libname, RTLD_LAZY);

            return Linux.dlopen(libname, RTLD_LAZY);
        }

        public static T LoadFunction<T>(IntPtr library, string function, bool throwIfNotFound = false)
        {
            var ret = IntPtr.Zero;

            if (OperatingSystem.IsWindows())
                ret = Windows.GetProcAddress(library, function);
            else if (OperatingSystem.IsMacOS())
                ret = OSX.dlsym(library, function);
            else
                ret = Linux.dlsym(library, function);

            if (ret == IntPtr.Zero)
            {
                if (throwIfNotFound)
                    throw new EntryPointNotFoundException(function);

                return default(T);
            }

#if NETSTANDARD
            return Marshal.GetDelegateForFunctionPointer<T>(ret);
#else
            return (T)(object)Marshal.GetDelegateForFunctionPointer(ret, typeof(T));
#endif
        }

        public static string Rid
        {
            get
            {
                if (OperatingSystem.IsWindows() && Environment.Is64BitProcess)
                    return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
                else if (OperatingSystem.IsWindows() && !Environment.Is64BitProcess)
                    return "win-x86";
                else if (OperatingSystem.IsLinux())
                    return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
                else if (OperatingSystem.IsMacOS())
                    return "osx";
                else
                    return "unknown";
            }
        }
    }
}
