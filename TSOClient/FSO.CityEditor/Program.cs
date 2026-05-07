using FSO.Common.Utils;
using FSO.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using FSO.Client.Debug;

namespace FSO.CityEditor
{
    /// <summary>
    /// Entry point for FSO.CityEditor.exe — a Volcanic-style wrapper that
    /// loads FSO.Client.dll at runtime and runs the FreeSO client in
    /// city-editor mode. Authoring tools live alongside the live renderer
    /// instead of duplicating it.
    ///
    /// Ships next to FSO.exe / FSO.IDE.exe in the same install directory
    /// and inherits the user's existing TSO content tree.
    /// </summary>
    static class Program
    {
        public static IFSOProgram FSOProgram;
        public static IGameStartProxy StartProxy;

        [STAThread]
        static void Main(string[] args)
        {
            FSO.Windows.Program.InitWindows();

            // Mirrors FSO.IDE/Program.cs: dynamically load the client so we
            // share its renderer + content pipeline, with no compile-time
            // dependency on the game's massive transitive graph.
            try
            {
                var asm = Assembly.LoadFile(Path.GetFullPath(@"FSO.Client.dll"));
                var type = asm.GetType("FSO.Client.FSOProgram");
                FSOProgram = Activator.CreateInstance(type) as IFSOProgram;
                type = asm.GetType("FSO.Client.GameStartProxy");
                StartProxy = Activator.CreateInstance(type) as IGameStartProxy;
                AssemblyUtils.Entry = asm;
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "FSO.CityEditor needs FSO.Client.dll in the same directory. "
                    + "Drop this binary alongside FSO.exe in your FreeSO install.\r\n\r\n"
                    + e);
                return;
            }

            // Register the editor hook before the game boots so the client's
            // screen-transition logic can pick city-editor mode on the way up.
            CityEditorHook.SetEditor(new CityEditorBootstrap());

            // Parse editor-specific CLI args. Supports:
            //   --city <path>    absolute or relative path to a city dir
            //                    containing the seven PNG layers
            //   --cityid <num>   load a built-in city by ID (default 100)
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--city" && i + 1 < args.Length)
                {
                    CityEditorHook.RequestedCityPath = Path.GetFullPath(args[i + 1]);
                    i++;
                }
                else if (args[i] == "--cityid" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out var id))
                        CityEditorHook.RequestedCityId = id;
                    i++;
                }
            }

            if (!FSOProgram.InitWithArguments(args)) return;
            StartProxy.Start(FSOProgram.UseDX);
        }
    }

    /// <summary>
    /// Minimal bootstrap implementation. Future commits will route brush
    /// changes / save events / procgen invocations through this object.
    /// </summary>
    internal class CityEditorBootstrap : ICityEditorInjector
    {
        public void OnCityReady()
        {
            // Hook to be filled in once the screen-side handler exists.
        }
    }
}