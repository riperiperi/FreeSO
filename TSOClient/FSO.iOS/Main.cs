#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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

		internal static void RunGame()
		{
            var iPad = UIDevice.CurrentDevice.Model.Contains("iPad");
            //TODO: disable iPad retina somehow
            FSOEnvironment.ContentDir = "Content/";
			FSOEnvironment.GFXContentDir = "Content/iOS/";
			FSOEnvironment.UserDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			FSOEnvironment.Linux = true;
			FSOEnvironment.DirectX = false;
			FSOEnvironment.SoftwareKeyboard = true;
			FSOEnvironment.SoftwareDepth = true;
            FSOEnvironment.UseMRT = false;
			FSOEnvironment.UIZoomFactor = iPad?1:2;
            FSOEnvironment.DPIScaleFactor = iPad ? 2 : 1;
            FSO.Files.ImageLoader.UseSoftLoad = false;

            GlobalSettings.Default.CityShadows = false;

            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip")))
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));

			var start = new GameStartProxy();
            start.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/"));//"/private/var/mobile/Documents/The Sims Online/TSOClient/");

            TSOGame game = new TSOGame();
            GameFacade.DirectX = false;
            FSO.LotView.World.DirectX = false;
            game.Run();

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

