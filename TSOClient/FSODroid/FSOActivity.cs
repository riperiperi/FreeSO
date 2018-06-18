using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using FSO.Client;
using FSO.Common;
using FSO.Common.Utils;
using FSO.LotView;
using FSO.SimAntics.NetPlay.Model;
using FSODroid.Resources.Layout;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Linq;

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
            if (File.Exists(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/tuning.dat")))
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
            var bDir = System.IO.Path.Combine(dest, folder);
            if (File.Exists(bDir)) File.Delete(bDir);
            Directory.CreateDirectory(bDir);
            foreach (string name in fileNames)
            {
                var path = System.IO.Path.Combine(bDir, name);
                var assetPath = System.IO.Path.Combine(folder, name);
                if (name.Contains(".") && !name.Contains(".dir"))
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

        public static HashSet<uint> MASK_COLORS = new HashSet<uint>{
            new Microsoft.Xna.Framework.Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue,
        };

        public static Texture2D AndroidFromStream(GraphicsDevice gd, Stream str)
        {
            var magic = (str.ReadByte() | (str.ReadByte() << 8));
            str.Seek(0, SeekOrigin.Begin);
            magic += 0;
            if (magic == 0x4D42)
            {
                try
                {
                    //it's a bitmap. 
                    var data = BitmapReader(str);
                    ManualTextureMaskSingleThreaded(data.Item1, MASK_COLORS.ToArray());
                    var tex = new Texture2D(gd, data.Item2, data.Item3);
                    tex.SetData(data.Item1);
                    return tex;
                }
                catch (Exception)
                {
                    return null; //bad bitmap :(
                }
            }
            else
            {
                //test for targa
                str.Seek(-18, SeekOrigin.End);
                byte[] sig = new byte[16];
                str.Read(sig, 0, 16);
                str.Seek(0, SeekOrigin.Begin);
                if (ASCIIEncoding.Default.GetString(sig) == "TRUEVISION-XFILE")
                {
                    try
                    {
                        var tga = new TargaImagePCL.TargaImage(str);
                        var tex = new Texture2D(gd, tga.Image.Width, tga.Image.Height);
                        tex.SetData(tga.Image.ToBGRA(true));
                        return tex;
                    }
                    catch (Exception)
                    {
                        return null; //bad tga
                    }
                }
                else
                {
                    //anything else
                    try
                    {
                        var data = BitmapReader(str);
                        var tex = new Texture2D(gd, data.Item2, data.Item3);
                        tex.SetData(data.Item1);
                        //var tex = Texture2D.FromStream(gd, str);
                        return tex;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error: " + e.ToString());
                        return new Texture2D(gd, 1, 1);
                    }
                }
            }
        }

        public static void ManualTextureMaskSingleThreaded(byte[] buffer, uint[] ColorsFrom)
        {
            var ColorTo = Microsoft.Xna.Framework.Color.Transparent.PackedValue;

            for (int i = 0; i < buffer.Length; i += 4)
            {
                if (buffer[i] >= 248 && buffer[i + 2] >= 248 && buffer[i + 1] <= 4)
                {
                    buffer[i] = buffer[i + 1] = buffer[i + 2] = buffer[i + 3] = 0;
                }
            }
        }

        public static Tuple<byte[], int, int> BitmapReader(Stream str)
        {
            Bitmap image = BitmapFactory.DecodeStream(str, null, new BitmapFactory.Options
            {
                InScaled = false,
                InDither = false,
                InJustDecodeBounds = false,
                InPurgeable = true,
                InInputShareable = true,
            });


            var width = image.Width;
            var height = image.Height;

            int[] pixels = new int[width * height];
            if ((width != image.Width) || (height != image.Height))
            {
                using (Bitmap imagePadded = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888))
                {
                    Canvas canvas = new Canvas(imagePadded);
                    canvas.DrawARGB(0, 0, 0, 0);
                    canvas.DrawBitmap(image, 0, 0, null);
                    imagePadded.GetPixels(pixels, 0, width, 0, 0, width, height);
                    imagePadded.Recycle();
                }
            }
            else
            {
                image.GetPixels(pixels, 0, width, 0, 0, width, height);
            }
            image.Recycle();

            // Convert from ARGB to ABGR
            ConvertToABGR(height, width, pixels);

            image.Dispose();

            return new Tuple<byte[], int, int>(Premult(VMSerializableUtils.ToByteArray(pixels)), width, height);
        }

        private static byte[] Premult(byte[] data)
        {
            for (int i=0; i<data.Length; i+=4)
            {
                int a = data[i + 3];
                data[i] = (byte)((data[i] * a) / 255);
                data[i + 1] = (byte)((data[i + 1] * a) / 255);
                data[i + 2] = (byte)((data[i + 2] * a) / 255);
            }
            return data;
        }

        //Converts Pixel Data from ARGB to ABGR
        private static void ConvertToABGR(int pixelHeight, int pixelWidth, int[] pixels)
        {
            int pixelCount = pixelWidth * pixelHeight;
            for (int i = 0; i < pixelCount; ++i)
            {
                uint pixel = (uint)pixels[i];
                pixels[i] = (int)((pixel & 0xFF00FF00) | ((pixel & 0x00FF0000) >> 16) | ((pixel & 0x000000FF) << 16));
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

            FSO.Files.ImageLoader.BaseFunction = AndroidFromStream;

            var iPad = ((int)disp.DensityDpi < 200);
            var docs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            CopyAssets("", docs);
            FSOEnvironment.ContentDir = System.IO.Path.Combine(docs, "Content/"); //copied to storage
            FSOEnvironment.GFXContentDir = "Content/iOS/"; //in assets
            FSOEnvironment.UserDir = docs;
            FSOEnvironment.Linux = true;
            FSOEnvironment.DirectX = false;
            FSOEnvironment.SoftwareKeyboard = true;
            FSOEnvironment.SoftwareDepth = true;
            FSOEnvironment.UseMRT = false;
            FSOEnvironment.UIZoomFactor = uiZoomFactor;
            FSOEnvironment.DPIScaleFactor = dpiScaleFactor;
            FSOEnvironment.Enable3D = true;
            FSO.Files.ImageLoader.UseSoftLoad = false;

            FSOEnvironment.EnableNPOTMip = false;
            FSOEnvironment.GLVer = 2;
            FSOEnvironment.TexCompress = false;
            FSOEnvironment.TexCompressSupport = false;

            //below is just to get blueprint.dtd reading. Should be only thing using relative directories.
            Directory.SetCurrentDirectory(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));

            if (File.Exists(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online.zip")))
                File.Delete(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));

            FSOEnvironment.GameThread = Thread.CurrentThread;
            var asm = Assembly.Load("FSO.Client"); //Assembly.LoadFile(Path.GetFullPath(@"FSO.Client.dll"));
            var type = asm.GetType("FSO.Client.FSOProgram");
            AssemblyUtils.Entry = asm;

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
            set.DirectionalLight3D = false;

            var start = new GameStartProxy();
            start.SetPath(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online/TSOClient/"));

            TSOGame game = new TSOGame();
            new Microsoft.Xna.Framework.GamerServices.GamerServicesComponent(game);
            GameFacade.DirectX = false;
            World.DirectX = false;
            SetContentView((View)game.Services.GetService(typeof(View)));
            game.Run();
        }
    }
}

