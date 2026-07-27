using System.Threading;

namespace FSO.Common
{
    public static class FSOEnvironment
    {
        public static Thread GameThread;

        public static string ContentDir = "Content/";
        public static string UserDir = "Content/";
        public static string GFXContentDir = "Content/OGL";
        public static bool MissingTSO = false;
        private static bool _DirectX = false;
        public static bool DirectX
        {
            get { return _DirectX; }
            set
            {
                _DirectX = value;
                // MacOS opengl drivers seem to have broken texel centers.
                PxOffset2D = (_DirectX || !OperatingSystem.IsMacOS()) ? 0 : 0.5f;
            }
        }
        public static bool Linux = false;
        public static bool UseMRT = true;
        /// <summary>
        /// Some platforms require a UV offset to realign pixel centers to avoid visual issues in 2D mode.
        /// </summary>
        public static float PxOffset2D = 0f;
        /// <summary>
        /// True if system does not support gl_FragDepth (eg. iOS). Uses alternate pipeline that abuses stencil buffer.
        /// </summary>
        public static bool SoftwareDepth = false;
        public static int GLVer = 3;
        public static float UIZoomFactor = 1f;
        public static float DPIScaleFactor = 1;
        public static bool SoftwareKeyboard = false;
        public static bool NoSound = false;
        public static int RefreshRate = 60;

        /// <summary>
        /// True if 3D features are enabled (like smooth rotation + zoom). Loads some content with mipmaps and other things.
        /// Used to mean "3d camera" as well, though that has been moved to configuration and world state.
        /// </summary>
        public static bool Default3D = false;
        public static bool Enable3D = true;
        public static bool EnableNPOTMip = true;
        public static bool TexCompress = true;
        public static bool TexCompressSupport = true;
        public static bool MSAASupport = true;

        public static string Args = "";
    }
}
