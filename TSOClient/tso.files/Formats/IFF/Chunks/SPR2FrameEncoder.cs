using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.IFF.Chunks
{
	public static class SPR2FrameEncoder
    {

        public delegate Color[] QuantizerFunction(SPR2Frame frame, out byte[] bytes);

        public static QuantizerFunction QuantizeFrame;

        public static void WriteFrame(SPR2Frame frame, IoWriter output)
        {
            var bytes = frame.PalData;
            var col = frame.PixelData;
            var zs = frame.ZBufferData;

            int index = 0;
            int blankLines = 0;
            for (int y=0; y<frame.Height; y++)
            {
                byte lastCmd = 0;
                List<byte> dataBuf = new List<byte>();
                int rlecount = 0;
                bool anySolid = false;

                var scanStream = new MemoryStream();
                var scanOut = IoWriter.FromStream(scanStream, ByteOrder.LITTLE_ENDIAN);

                for (int x=0; x<frame.Width; x++)
                {
                    byte plt = bytes[index];
                    byte a = col[index].A;
                    byte z = zs[index];

                    var cmd = getCmd(plt, a, z);

                    if (x == 0 || cmd != lastCmd)
                    {
                        if (x != 0)
                        {
                            //write a command to write the last sequence of pixels
                            scanOut.WriteUInt16((ushort)(((int)lastCmd<<13)|rlecount));
                            if ((dataBuf.Count % 2) != 0) dataBuf.Add(0);
                            scanOut.WriteBytes(dataBuf.ToArray());
                            dataBuf.Clear();
                        }

                        lastCmd = cmd;
                        rlecount = 0;
                    }

                    switch (cmd)
                    {
                        case 0x1:
                        case 0x2:
                            dataBuf.Add(z);
                            dataBuf.Add(plt);
                            if (cmd == 0x2) dataBuf.Add((byte)Math.Ceiling(a/ 8.2258064516129032258064516129032));
                            anySolid = true;
                            break;
                        case 0x6:
                            dataBuf.Add(plt);
                            anySolid = true;
                            break;
                        default:
                            break;
                    }
                    rlecount++;

                    index++;
                }

                if (anySolid)
                {
                    //write a command to write the last sequence of pixels
                    scanOut.WriteUInt16((ushort)(((int)lastCmd << 13) | rlecount));
                    if ((dataBuf.Count % 2) != 0) dataBuf.Add(0);
                    scanOut.WriteBytes(dataBuf.ToArray());
                    dataBuf.Clear();
                }

                var scanData = scanStream.ToArray();

                if (scanData.Length == 0)
                    blankLines++; //line is transparent
                else 
                {
                    if (blankLines > 0)
                    {
                        //add transparent lines before our new command
                        output.WriteUInt16((ushort)((0x4<<13) | blankLines));
                        blankLines = 0;
                    }
                    output.WriteUInt16((ushort)(scanData.Length+2));
                    output.WriteBytes(scanData);
                }

            }

            if (blankLines > 0)
            {
                //add transparent lines before our new command
                output.WriteUInt16((ushort)((0x4 << 13) | blankLines));
                blankLines = 0;
            }
            output.WriteUInt16((ushort)(0x5<<13));

            return;
        }

        private static byte getCmd(byte col, byte a, byte z)
        {
            if (a == 0) return 0x03; //transparent fill
            else if (a < 255) return 0x02; // col,a,z
            else if (z > 0) return 0x01; // col,z
            else return 0x06; // col
        }
    }
}
