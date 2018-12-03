#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Ninject.Injection;
using Common.Logging;
using System.Threading;
using FSO.Files;
using FSO.Client.UI.Panels;
//using Ninject;
#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#elif __IOS__ || __TVOS__
using Foundation;
using FSO.Client;
using FSO.Common;
using UIKit;
#endif
#endregion

namespace FSOiOS
{
#if __IOS__ || __TVOS__
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
#else
	static class Program
#endif
	{

        public static Action<string> MainOrg;

		internal static void RunGame()
		{
            ImageLoader.BaseFunction = iOSImageLoader.iOSFromStream;
            var iPad = UIDevice.CurrentDevice.Model.Contains("iPad");
            //TODO: disable iPad retina somehow
            FSOEnvironment.ContentDir = "Content/";
			FSOEnvironment.GFXContentDir = "Content/iOS/";
			FSOEnvironment.UserDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			FSOEnvironment.Linux = true;
			FSOEnvironment.DirectX = false;
			FSOEnvironment.SoftwareKeyboard = true;
			FSOEnvironment.SoftwareDepth = true;
            FSOEnvironment.EnableNPOTMip = true;
            FSOEnvironment.GLVer = 2;
            FSOEnvironment.UseMRT = false;
			FSOEnvironment.UIZoomFactor = iPad?1:2;
            FSOEnvironment.DPIScaleFactor = iPad ? 2 : 1;
            FSOEnvironment.TexCompress = false;
            FSOEnvironment.TexCompressSupport = false;

            FSOEnvironment.GameThread = Thread.CurrentThread;
            FSOEnvironment.Enable3D = true;
            ITTSContext.Provider = AppleTTSContext.PlatformProvider;

            FSO.Files.ImageLoader.UseSoftLoad = false;

            /*
            var settings = new NinjectSettings();
            settings.LoadExtensions = false;
            */

            if (MainOrg != null)
            {
                var cont = new FSO.Client.GameController(null);
            }
            MainOrg = FSO.Client.FSOProgram.ShowDialog;

            GlobalSettings.Default.CityShadows = false;


            var set = GlobalSettings.Default;
            set.TargetRefreshRate = 60;
            set.CurrentLang = "english";
            set.Lighting = true;
            set.SmoothZoom = true;
            set.AntiAlias = false;
            set.LightingMode = 3;
            set.AmbienceVolume = 10;
            set.FXVolume = 10;
            set.MusicVolume = 10;
            set.VoxVolume = 10;
            set.GraphicsWidth = (int)UIScreen.MainScreen.Bounds.Width;
            set.DirectionalLight3D = false;
            set.GraphicsHeight = (int)UIScreen.MainScreen.Bounds.Height;
            set.CitySelectorUrl = "http://46.101.67.219:8081";
            set.GameEntryUrl = "http://46.101.67.219:8081";

            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip")))
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));

			var start = new GameStartProxy();
            start.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/"));//"/private/var/mobile/Documents/The Sims Online/TSOClient/");

            TSOGame game = new TSOGame();
            GameFacade.DirectX = false;
            FSO.LotView.World.DirectX = false;
            game.Run(Microsoft.Xna.Framework.GameRunBehavior.Asynchronous);

#if !__IOS__ && !__TVOS__
			game.Dispose();
#endif
        }

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
#if !MONOMAC && !__IOS__ && !__TVOS__
		[STAThread]
#endif
		static void Main(string[] args)
		{
#if MONOMAC
            NSApplication.Init ();

            using (var p = new NSAutoreleasePool ()) {
                NSApplication.SharedApplication.Delegate = new AppDelegate();
                NSApplication.Main(args);
            }
#elif __IOS__ || __TVOS__
            UIApplication.Main(args, null, "AppDelegate");
#else
			RunGame();
#endif
		}

#if __IOS__ || __TVOS__
        public override void FinishedLaunching(UIApplication app)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/tuning.dat")))
            {
                RunGame();
            }
            else
            {
                UIStoryboard storyboard = UIStoryboard.FromName("Installer", null);
                var window = new UIWindow(UIScreen.MainScreen.Bounds);
                var viewController = storyboard.InstantiateViewController("Main") as FSOInstallViewController;
                viewController.OnInstalled += FSOInstalled;
                window.MakeKeyAndVisible();
                window.RootViewController = viewController;
            }
        }

        private void FSOInstalled()
        {
            RunGame();
        }
#endif
    }

#if MONOMAC
    class AppDelegate : NSApplicationDelegate
    {
        public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs a) =>  {
                if (a.Name.StartsWith("MonoMac")) {
                    return typeof(MonoMac.AppKit.AppKitFramework).Assembly;
                }
                return null;
            };
            Program.RunGame();
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
        {
            return true;
        }
    }  
#endif
}

