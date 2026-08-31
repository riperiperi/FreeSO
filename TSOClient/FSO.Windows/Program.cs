using FSO.Client;
using FSO.Client.UI.Panels;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Utils;
using FSO.Common.Utils.Interop;
using FSO.Windows.Platform;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FSO.Windows
{
    public static class Program
    {

        public static bool UseDX = true;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        [STAThread]
        public static void Main(string[] args)
        {
            InitWindows();

            if ((new FSOProgram()).InitWithArguments(args))
            {
                var startProxy = new GameStartProxy();
                startProxy.Start(UseDX);
            }

            TimerControl?.Dispose();
        }

        public static IDisposable TimerControl;

        public static void InitWindows()
        {
            //initialize some platform specific stuff
            FSO.Files.ImageLoaderHelpers.BitmapFunction = BitmapReader;
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            ClipboardHandler.Default = new WinFormsClipboard();
            FSO.Files.ImageLoaderHelpers.SavePNGFunc = SavePNG;

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (!linux) ITTSContext.Provider = UITTSContext.PlatformProvider;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            FSOProgram.ShowDialog = ShowDialog;

            if (OperatingSystem.IsWindows())
            {
                // Monogame sleeps between frames to control update timing, which is governed by the timer resolution.
                // Windows timer precision is low by default, so push it to give us better frame timing.
                // We could actually get 0.5ms timing with another method, but this is a lot hackier and not too important for us.
                TimerControl = new WindowsMultimediaTimerResolution(1);

                // On linux and macos, timers are a lot more precise.

                FSOProgram.GetDisplayInfo = (window) =>
                {
                    try
                    {
                        var form = System.Windows.Forms.Form.FromHandle(window) as System.Windows.Forms.Form;
                        var screen = Screen.FromControl(form);

                        int refreshRate = 0;
                        if (Win32Interop.TryGetCurrentMode(screen.DeviceName, out var mode) && mode.DisplayFrequency < int.MaxValue)
                        {
                            refreshRate = (int)mode.DisplayFrequency;
                        }

                        return new(form.DeviceDpi / 96f, refreshRate);
                    }
                    catch
                    {
                        return new(1f);
                    }
                };

                FSOProgram.RegisterDragCallback = (window, callback) =>
                {
                    var bindThread = new Thread(x =>
                    {
                        var form = System.Windows.Forms.Form.FromHandle(window) as System.Windows.Forms.Form;

                        form?.BeginInvoke(() =>
                        {
                            form.AllowDrop = true;
                            form.DragEnter += (sender, e) =>
                            {
                                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                                {
                                    e.Effect = DragDropEffects.Copy;
                                }
                                else
                                {
                                    e.Effect = DragDropEffects.None;
                                }
                            };
                            form.DragDrop += (sender, e) =>
                            {
                                var path = (e.Data.GetData(DataFormats.FileDrop) as string[])[0];
                                callback(path);
                            };
                        });
                    });
                    bindThread.SetApartmentState(ApartmentState.STA);
                    bindThread.Start();
                };
            }
        }

        public static void ShowDialog(string text)
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (linux)
            {
                Console.WriteLine(text);
                Environment.Exit(0);
            }
            else
            {
                MessageBox.Show(text);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject;

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (linux)
            {
                Console.WriteLine("===== FATAL ERROR =====");
                Console.WriteLine(e.ExceptionObject.ToString());
                Environment.Exit(0);
            }
            else
            {
                if (exception is OutOfMemoryException)
                {
                    MessageBox.Show(e.ExceptionObject.ToString(), "Out of Memory! FreeSO needs to close.");
                }
                else
                {
                    MessageBox.Show(e.ExceptionObject.ToString(), "A fatal error occured! Screenshot this dialog and post it on Discord.");
                }
            }
        }

        public static void SavePNG(byte[] data, int width, int height, Stream str)
        {
            Bitmap image = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            // Fix up the Image to match the expected format
            //image = (Bitmap)image.RGBToBGR();

            BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            if (bitmapData.Stride != image.Width * 4)
                throw new NotImplementedException();


            for (int i = 0; i < data.Length; i += 4)
            {
                //if (data[i+3] == 0) { }
                //var temp = data[i];
                //data[i] = data[i + 2];
                //data[i + 2] = temp;
            }

            Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);
            image.UnlockBits(bitmapData);

            image.Save(str, ImageFormat.Png);
        }

        public static Tuple<byte[], int, int> BitmapReader(Stream str)
        {
            Bitmap image = (Bitmap)Bitmap.FromStream(str);
            try
            {
                var data = new byte[image.Width * image.Height * 4];

                BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                if (bitmapData.Stride != image.Width * 4)
                    throw new NotImplementedException();
                Marshal.Copy(bitmapData.Scan0, data, 0, data.Length);
                image.UnlockBits(bitmapData);

                RGBtoBGR.Convert(data);

                return new Tuple<byte[], int, int>(data, image.Width, image.Height);
            }
            finally
            {
                image.Dispose();
            }
        }
    }
}
