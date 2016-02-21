using FSO.Client;
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
        [STAThread]
        static void Main(string[] args)
        {
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
            FSO.Files.Formats.IFF.IffFile.RETAIN_CHUNK_DATA = true;
            FSO.Client.Debug.IDEHook.SetIDE(new IDETester());
            (new GameStartProxy()).Start(FSO.Client.Program.UseDX);
        }
    }
}
