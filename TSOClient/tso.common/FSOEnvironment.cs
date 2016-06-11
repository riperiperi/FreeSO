using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FSO.Common
{
    public static class FSOEnvironment
    {
        public static Thread GameThread;

        public static string ContentDir = "Content/";
        public static string GFXContentDir = "Content/OGL";
        public static bool DirectX = false;
        public static bool Linux = false;
    }
}
