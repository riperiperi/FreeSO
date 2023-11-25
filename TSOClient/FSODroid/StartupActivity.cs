using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Content.PM;
using System.IO;
using FSODroid.Resources.Layout;
using System.Threading;

namespace FSODroid
{
    [Activity(Label = "FreeSO"
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = Android.Content.PM.LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.SensorLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
        , MainLauncher = true)]
    public class StartupActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            new Thread(() =>
            {
                Thread.Sleep(100);
                RunOnUiThread(() =>
                {
                    Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
                    Window.AddFlags(WindowManagerFlags.Fullscreen); //to show
                    if (File.Exists(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/tuning.dat")))
                    {
                        var activity2 = new Intent(this, typeof(FSOActivity));
                        StartActivity(activity2);
                    }
                    else
                    {
                        var activity2 = new Intent(this, typeof(InstallerActivity));
                        StartActivity(activity2);
                    }
                });
            }).Start();
        }
    }
}