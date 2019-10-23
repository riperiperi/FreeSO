#define IDE_COMPAT

using FSO.Client;
using FSO.Common.Utils;
using FSO.IDE.Common;
using FSO.SimAntics.JIT.Roslyn;
using FSO.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    static class Program
    {
        public static IFSOProgram FSOProgram;
        public static IGameStartProxy StartProxy;
        public static Thread MainThread;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            FSO.Windows.Program.InitWindows();
            TimedReferenceController.SetMode(CacheType.PERMANENT);

            try
            {
                var asm = Assembly.LoadFile(Path.GetFullPath(@"FSO.Client.dll"));
                var type = asm.GetType("FSO.Client.FSOProgram");
                FSOProgram = Activator.CreateInstance(type) as IFSOProgram;
                type = asm.GetType("FSO.Client.GameStartProxy");
                StartProxy = Activator.CreateInstance(type) as IGameStartProxy;
                AssemblyUtils.Entry = asm;
            } catch (Exception)
            {
                try
                {
                    var asm = Assembly.LoadFile(Path.GetFullPath(@"Simitone.exe"));
                    var type = asm.GetType("Simitone.Windows.FSOProgram");
                    FSOProgram = Activator.CreateInstance(type) as IFSOProgram;
                    type = asm.GetType("Simitone.Windows.GameStartProxy");
                    StartProxy = Activator.CreateInstance(type) as IGameStartProxy;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to find FreeSO or Simitone. Ensure their binary files have the correct name! \r\n" + e.ToString());
                    return;
                }
            }

            if (!FSOProgram.InitWithArguments(args)) return;
            (new VolcanicStartProxy()).Start(args);
        }
    }

    public class VolcanicStartProxy
    {
        public void Start(string[] args)
        {
            InitVolcanic(args);
            Program.StartProxy.Start(Program.FSOProgram.UseDX);
        }

        public void InitVolcanic(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Files.Formats.IFF.Chunks.SPR2FrameEncoder.QuantizeFrame = SpriteEncoderUtils.QuantizeFrame;
            FSO.Files.Formats.IFF.IffFile.RETAIN_CHUNK_DATA = true;
            FSO.SimAntics.VM.SignalBreaks = true;
            FSO.Client.Debug.IDEHook.SetIDE(new IDETester());

            //requires reference to FSO.SimAntics.JIT.Roslyn
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-jit":
                        {
                            var roslyn = new RoslynSimanticsJIT();
                            FSO.SimAntics.Engine.VMTranslator.INSTANCE = new VMRoslynTranslator(roslyn);
                            break;
                        }
                    case "-jitdebug":
                        {
                            var roslyn = new RoslynSimanticsJIT();
                            roslyn.Context.Debug = true;
                            FSO.SimAntics.Engine.VMTranslator.INSTANCE = new VMRoslynTranslator(roslyn);
                            break;
                        }
                }
            }
        }
    }
}
