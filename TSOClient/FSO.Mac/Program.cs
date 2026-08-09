using System;
using FSO.Client;

namespace FSO.Mac
{
    /// <summary>
    /// macOS platform head — same role as FSO.Windows/Program.cs (references FSO.Client,
    /// hosts it), minus anything Windows-specific. Not yet a polished app bundle launcher;
    /// goal per this pass is "a window opens and the game reaches its first screen," not a
    /// finished distributable.
    ///
    /// Deliberately NOT wired up (left at their safe defaults — see each type's own default
    /// behavior, not a crash risk, just missing functionality):
    /// - ClipboardHandler.Default — stays the built-in no-op (Get() returns "", Set() no-ops).
    ///   Clipboard copy/paste in text fields won't work.
    /// - ITTSContext.Provider — stays null. No text-to-speech.
    /// - FSO.Files.ImageLoaderHelpers.BitmapFunction / SavePNGFunc — stay null. These are
    ///   only consulted conditionally (`if (BitmapFunction != null)`) by ImageLoader, so
    ///   leaving them unset is safe, but some non-standard image load paths (custom skins,
    ///   certain screenshot saves) will silently no-op rather than work. Windows implements
    ///   these via System.Drawing.Bitmap; a macOS equivalent would need a cross-platform
    ///   image library (e.g. ImageSharp) — not done here.
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            FSOProgram.ShowDialog = ShowDialog;

            if ((new FSOProgram()).InitWithArguments(args))
                (new GameStartProxy()).Start(false);
        }

        static void ShowDialog(string text)
        {
            Console.WriteLine(text);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("===== FATAL ERROR =====");
            Console.WriteLine(e.ExceptionObject.ToString());
        }
    }
}
