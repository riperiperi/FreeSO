using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LogThis;

namespace PDPatcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Log.UseSensibleDefaults();

            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log.LogThis("Unhandled exception: \n" + e.Exception.ToString(), eloglevel.warn);
        }
    }
}
