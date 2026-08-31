using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FSO.Common.Utils.Interop
{
    [InlineArray(32)]
    public struct DeviceName
    {
        private byte _elem;
    }

    [InlineArray(32)]
    public struct FormName
    {
        private byte _elem;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceMode
    {
        public DeviceName Name;
        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;

        // this is a union, but we only care about refresh rate which comes after it

        public int PositionX;
        public int PositionY;
        public uint DisplayOrientation;
        public uint DisplayFixedOutput;

        // Union end

        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;
        public FormName FormName;
        public ushort LogPixels;
        public uint BitsPerPel;
        public uint PelsWidth;
        public uint PelsHeight;
        public uint DisplayFlags;
        public uint DisplayFrequency;
        public uint ICMMethod;
        public uint ICMIntent;
        public uint MediaType;
        public uint DitherType;
        public uint Reserved1;
        public uint Reserved2;
        public uint PanningWidth;
        public uint PanningHeight;
    }

    public static class Win32Interop
    {
        private const int ENUM_CURRENT_SETTINGS = -1;

        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettingsA(string lpszDeviceName, int iModeNum, out DeviceMode lpDevMode);

        public static bool TryGetCurrentMode(string deviceName, out DeviceMode mode)
        {
            return EnumDisplaySettingsA(deviceName, ENUM_CURRENT_SETTINGS, out mode);
        }
    }
}
