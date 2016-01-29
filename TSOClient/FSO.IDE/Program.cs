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
            FSO.Files.Formats.IFF.IffFile.RETAIN_CHUNK_DATA = true;
            FSO.Client.Debug.IDEHook.SetIDE(new IDETester());
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            FSO.Client.Program.Main(args);
        }
    }
}
