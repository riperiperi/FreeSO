using FSO.Client.Network;
using FSO.Client.UI.Hints;
using FSO.Client.UI.Panels;
using FSO.Common;
using Ninject;
using System.Diagnostics;

namespace FSO.Client
{
    public class FSOFacade
    {
        public static KernelBase Kernel;
        public static GameController Controller;
        public static UIMessageController MessageController = new UIMessageController();
        public static NetworkStatus NetStatus = new NetworkStatus();

        public static UIHintManager Hints;

        private static string GetFreeSOName()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                return "FreeSO";
            }
            else
            {
                return "FreeSO.exe";
            }
        }

        public static void RestartGame()
        {
            try
            {
                var fsoExe = GetFreeSOName();

                var args = FSOEnvironment.Args;
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(fsoExe, string.Join(" ", FSOEnvironment.Args));
                }
                else if (OperatingSystem.IsMacOS())
                {
                    var startArgs = new ProcessStartInfo("open", $"../../ --args " + args)
                    {
                        UseShellExecute = false
                    };

                    Process.Start(startArgs);
                }
                else
                {
                    var startArgs = new ProcessStartInfo(fsoExe, args)
                    {
                        UseShellExecute = false
                    };

                    Process.Start(startArgs);
                }
            }
            catch
            {

            }

            GameFacade.Kill();
        }
    }
}
