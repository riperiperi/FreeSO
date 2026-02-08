using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FSO.Client;
using FSO.Client.UI.Panels;
using FSO.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace FSO.Unix
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            InitUnix();

            var mgAssembly = typeof(Microsoft.Xna.Framework.Game).Assembly;
            var platform = mgAssembly.GetType("MonoGame.Framework.Utilities.PlatformInfo");
            var backend = platform?.GetProperty("GraphicsBackend")?.GetValue(null);
            Console.WriteLine($"[FreeSO] MonoGame: {mgAssembly.GetName().Version} | Backend: {backend ?? "Unknown"}");

            FSOEnvironment.Enable3D = true;

            if ((new FSOProgram()).InitWithArguments(args))
            {
                var startProxy = new GameStartProxy();
                startProxy.Start(false);
            }

            Environment.Exit(0);
        }

        public static void InitUnix()
        {
            FSO.Files.ImageLoaderHelpers.BitmapFunction = BitmapReader;
            FSO.Files.ImageLoaderHelpers.SavePNGFunc = SavePNG;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            FSOProgram.ShowDialog = ShowDialog;
        }

        public static void ShowDialog(string text)
        {
            ShowDialog(text, "FreeSO Message");
        }

        private static string Escape(string s) => s.Replace("\"", "\\\"");

        private static void ShowDialog(string text, string title)
        {
            if (text.Length > 1500) text = text.Substring(0, 1500) + "...";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e \"display alert \\\"{Escape(title)}\\\" message \\\"{Escape(text)}\\\" giving up after 15\"",
                    UseShellExecute = true
                };
                Process.Start(psi)?.WaitForExit();
            }
            else
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "zenity",
                        Arguments = $"--error --title=\"{Escape(title)}\" --text=\"{Escape(text)}\"",
                        UseShellExecute = false
                    };
                    Process.Start(psi)?.WaitForExit();
                }
                catch
                {
                    Console.Error.WriteLine($"[{title}] {text}");
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string title = e.ExceptionObject is OutOfMemoryException
                ? "Out of Memory! FreeSO needs to close."
                : "A fatal error occured! Screenshot this dialog and post it on Discord.";

            ShowDialog(e.ExceptionObject.ToString(), title);
            Environment.Exit(1);
        }

        public static void SavePNG(byte[] data, int width, int height, Stream str)
        {
            using var image = new Image<Rgba32>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    image[x, y] = new Rgba32(data[i], data[i + 1], data[i + 2], data[i + 3]);
                }
            }

            image.Save(str, new PngEncoder());
        }

        public static Tuple<byte[], int, int> BitmapReader(Stream str)
        {
            using var image = Image.Load<Rgba32>(str);
            int width = image.Width;
            int height = image.Height;

            var data = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    Rgba32 px = image[x, y];
                    data[i] = px.R;
                    data[i + 1] = px.G;
                    data[i + 2] = px.B;
                    data[i + 3] = px.A;
                }
            }

            return new Tuple<byte[], int, int>(data, width, height);
        }
    }
}
