using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using SimsLib.XA;
using SimsLib.UTK;

namespace XaToWav
{
    /// <summary>
    /// This application will automatically convert all *.xa and *.utk files it can find in the directory
    /// it resides in into *.wav files.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string[] XAFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.xa");
            string[] UTKFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.utk");

            for (int i = 0; i < XAFiles.Length; i++)
            {
                XAFile XA = new XAFile();
                XA.LoadFile(XAFiles[i]);
                XA.DecompressFile();

                BinaryWriter Writer = new BinaryWriter(File.Create(XAFiles[i].Replace(".xa", ".wav")));
                Writer.Write(XA.DecompressedData);
                Writer.Close();
            }
            /*
            UTKFunctions.UTKGenerateTables();

            UTKWrapper UTK = new UTKWrapper();

            unsafe
            {
                for (int i = 0; i < UTKFiles.Length; i++)
                {
                    UTK.LoadUTK(UTKFiles[i]);
                    BinaryWriter Writer = new BinaryWriter(File.Create(UTKFiles[i].Replace(".utk", ".wav")));
                    Writer.Write(UTK.Wav);
                    Writer.Close();
                }
            }
            */
        }
    }
}
