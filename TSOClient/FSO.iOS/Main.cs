#region Using Statements
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
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

namespace FSO.iOS
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
			var test2 = new Point();
			var test3 = new Rectangle();

			FSOEnvironment.ContentDir = (test2.X + test3.Height).ToString();
			FSOEnvironment.ContentDir = "Content/";
			FSOEnvironment.GFXContentDir = "Content/iOS/";
			FSOEnvironment.UserDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			FSOEnvironment.Linux = true;
			FSOEnvironment.DirectX = false;
			FSOEnvironment.SoftwareKeyboard = true;
			FSOEnvironment.SoftwareDepth = true;
			FSOEnvironment.SmallScreen = true;

			var start = new GameStartProxy();
			start.SetPath("/private/var/mobile/Documents/The Sims Online/TSOClient/");
			start.Start(false);
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

