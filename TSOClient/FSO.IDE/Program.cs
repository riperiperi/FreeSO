#define IDE_COMPAT

using FSO.Client;
using FSO.Common.Utils;
using FSO.IDE.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            TimedReferenceController.SetMode(CacheType.PERMANENT);
            if (!FSO.Client.Program.InitWithArguments(args)) return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            (new VolcanicStartProxy()).Start();
        }
    }

    class VolcanicStartProxy
    {
        public void Start()
        {
            Files.Formats.IFF.Chunks.SPR2FrameEncoder.QuantizeFrame = SpriteEncoderUtils.QuantizeFrame;
            FSO.Files.Formats.IFF.IffFile.RETAIN_CHUNK_DATA = true;
            FSO.Client.Debug.IDEHook.SetIDE(new IDETester());
            (new GameStartProxy()).Start(FSO.Client.Program.UseDX);
        }
    }
}
