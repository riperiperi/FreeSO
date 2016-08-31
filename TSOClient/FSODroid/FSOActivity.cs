using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using FSO.Client;
using FSO.Common;
using FSO.LotView;
using FSODroid.Resources.Layout;
using System;
using System.IO;

namespace FSODroid
{
    [Activity(Label = "FreeSO"
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = Android.Content.PM.LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.SensorLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class FSOActivity : Microsoft.Xna.Framework.AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
            Window.AddFlags(WindowManagerFlags.Fullscreen); //to show
            if (File.Exists(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/tuning.dat")))
            {
                RunGame();
            }
            else
            {
                var activity2 = new Intent(this, typeof(InstallerActivity));
                StartActivity(activity2);
            }
        }

        public void CopyAssets(string folder, string dest)
        {
            string[] fileNames = Assets.List(folder);
            var bDir = Path.Combine(dest, folder);
            Directory.CreateDirectory(bDir);
            foreach (string name in fileNames)
            {
                var path = Path.Combine(bDir, name);
                var assetPath = Path.Combine(folder, name);
                if (name.Contains("."))
                {
                    //it's a file, copy it. kind of hacky.
                    var output = File.OpenWrite(path);
                    var input = Assets.Open(assetPath);
                    input.CopyTo(output);
                    output.Close();
                    input.Close();
                } else
                {
                    CopyAssets(assetPath, dest);
                }
            }
        }

        private void RunGame()
        {
            var disp = new DisplayMetrics();
            WindowManager.DefaultDisplay.GetMetrics(disp);

            var initSF = 1 + ((int)disp.DensityDpi / 200);
            var dpiScaleFactor = initSF;
            var width = Math.Max(disp.WidthPixels, disp.HeightPixels);
            var height = Math.Min(disp.WidthPixels, disp.HeightPixels);
            float uiZoomFactor = 1f;
            while (width / dpiScaleFactor < 800 && dpiScaleFactor > 1)
            {
                dpiScaleFactor--;
                uiZoomFactor = (initSF / dpiScaleFactor);
            }

            var iPad = ((int)disp.DensityDpi < 200);
            var docs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            CopyAssets("", docs);
            FSOEnvironment.ContentDir = Path.Combine(docs, "Content/"); //copied to storage
            FSOEnvironment.GFXContentDir = "Content/iOS/"; //in assets
            FSOEnvironment.UserDir = docs;
            FSOEnvironment.Linux = true;
            FSOEnvironment.DirectX = false;
            FSOEnvironment.SoftwareKeyboard = true;
            FSOEnvironment.SoftwareDepth = true;
            FSOEnvironment.UseMRT = false;
            FSOEnvironment.UIZoomFactor = uiZoomFactor;
            FSOEnvironment.DPIScaleFactor = dpiScaleFactor;
            FSO.Files.ImageLoader.UseSoftLoad = false;

            //below is just to get blueprint.dtd reading. Should be only thing using relative directories.
            Directory.SetCurrentDirectory(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "Content/Blueprints/"));

            if (File.Exists(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online.zip")))
                File.Delete(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));

            var start = new GameStartProxy();
            start.SetPath(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/"));

            TSOGame game = new TSOGame();
            new Microsoft.Xna.Framework.GamerServices.GamerServicesComponent(game);
            GameFacade.DirectX = false;
            World.DirectX = false;
            SetContentView((View)game.Services.GetService(typeof(View)));
            game.Run();
        }
    }
}

