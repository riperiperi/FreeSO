using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common
{
    public static class FSOEnvironment
    {
        public static string ContentDir = "Content/";
        public static string UserDir = "Content/";
        public static string GFXContentDir = "Content/OGL";
        public static bool DirectX = false;
        public static bool Linux = false;
        public static bool UseMRT = false;
        /// <summary>
        /// True if system does not support gl_FragDepth (eg. iOS). Uses alternate pipeline that abuses stencil buffer.
        /// </summary>
        public static bool SoftwareDepth = true;
        public static float UIZoomFactor = 1f;
        public static int DPIScaleFactor = 1;
        public static bool SoftwareKeyboard = false;
    }
}
