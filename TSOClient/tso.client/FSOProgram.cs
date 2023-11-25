using FSO.Client.Utils;
using FSO.Client.Utils.GameLocator;
using FSO.Common;
using FSO.UI;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FSO.Client
{
    public class FSOProgram : IFSOProgram
    {
        public bool UseDX { get; set; }

        public static Action<string> ShowDialog = DefaultShowDialog;

        public static void DefaultShowDialog(string text)
        {
            Console.WriteLine(text);
        }

        public bool InitWithArguments(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(baseDir);
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            //AppDomain.CurrentDomain.SetDynamicBase(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/JITCache/"));

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;

            ILocator gameLocator;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (linux && Directory.Exists("/Users"))
                gameLocator = new MacOSLocator();
            else if (linux)
                gameLocator = new LinuxLocator();
            else
                gameLocator = new WindowsLocator();

            bool useDX = false;

            #region User resolution parmeters

            foreach (var arg in args)
            {
                if (char.IsDigit(arg[0]))
                {
                    //attempt parsing resoulution
                    try
                    {
                        var split = arg.Split("x".ToCharArray());
                        int ScreenWidth = int.Parse(split[0]);
                        int ScreenHeight = int.Parse(split[1]);

                        GlobalSettings.Default.GraphicsWidth = ScreenWidth;
                        GlobalSettings.Default.GraphicsHeight = ScreenHeight;
                    }
                    catch (Exception) { }
                }
                else if (arg[0] == '-')
                {
                    var cmd = arg.Substring(1);
                    if (cmd.StartsWith("lang"))
                    {
                        GlobalSettings.Default.LanguageCode = byte.Parse(cmd.Substring(4));
                    }
                    else if (cmd.StartsWith("hz")) GlobalSettings.Default.TargetRefreshRate = int.Parse(cmd.Substring(2));
                    else
                    {
                        //normal style param
                        switch (cmd)
                        {
                            case "dx11":
                            case "dx":
                                useDX = true;
                                break;
                            case "gl":
                            case "ogl":
                                useDX = false;
                                break;
                            case "ts1":
                                GlobalSettings.Default.TS1HybridEnable = true;
                                break;
                            case "tso":
                                GlobalSettings.Default.TS1HybridEnable = false;
                                break;
                            case "3d":
                                FSOEnvironment.Enable3D = true;
                                break;
                            case "touch":
                                FSOEnvironment.SoftwareKeyboard = true;
                                break;
                            case "nosound":
                                FSOEnvironment.NoSound = true;
                                break;
                        }
                    }
                }
                else
                {
                    if (arg.Equals("w", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = true;
                    else if (arg.Equals("f", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = false;
                }
            }

            #endregion

            UseDX = MonogameLinker.Link(useDX);

            var path = gameLocator.FindTheSimsOnline();

            if (path != null)
            {
                //check if this path has tso in it. tuning.dat should be a good indication.
                if (!File.Exists(Path.Combine(path, "tuning.dat")))
                {
                    ShowDialog("The Sims Online appears to be missing. The game expects TSO at directory '"+path+"', but some core files are missing from that folder. If you know you installed TSO into a different directory, please move it into the directory specified.");
                    return false;
                }

                FSOEnvironment.Args = string.Join(" ", args);
                FSOEnvironment.ContentDir = "Content/";
                FSOEnvironment.GFXContentDir = "Content/" + (UseDX ? "DX/" : "OGL/");
                FSOEnvironment.Linux = linux;
                FSOEnvironment.DirectX = UseDX;
                FSOEnvironment.GameThread = Thread.CurrentThread;
                if (GlobalSettings.Default.LanguageCode == 0) GlobalSettings.Default.LanguageCode = 1;
                Files.Formats.IFF.Chunks.STR.DefaultLangCode = (Files.Formats.IFF.Chunks.STRLangCode)GlobalSettings.Default.LanguageCode;

                GlobalSettings.Default.StartupPath = path;
                GlobalSettings.Default.ClientVersion = GetClientVersion();
                return true;
            }
            else
            {
                ShowDialog("The Sims Online was not found on your system. FreeSO will not be able to run without access to the original game files.");
                return false;
            }
        }

        private static System.Reflection.Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var name = args.Name;
                if (name.StartsWith("FSO.Scripts"))
                {
                    return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == name);
                }
                else
                {
                    var assemblyPath = Path.Combine(MonogameLinker.AssemblyDir, args.Name.Substring(0, name.IndexOf(',')) + ".dll");
                    var assembly = Assembly.LoadFrom(assemblyPath);
                    return assembly;
                }
            }
            catch (Exception e)
            {
                return null;
            }

        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
        }

        private string GetClientVersion()
        {
            string ExeDir = GlobalSettings.Default.StartupPath;

            if (File.Exists("version.txt"))
            {
                using (StreamReader Reader = new StreamReader(File.Open("version.txt", FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    return Reader.ReadLine();
                }
            }
            else
            {
                return "(?)";
            }
        }
    }
}
