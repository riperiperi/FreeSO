using System;
using System.IO;

namespace CASOutfitImporter.Formats
{
    // RefPack / QFS decompressor — port of FSO.Files.FAR3.Decompresser.Decompress.
    // Reference: http://wiki.niotso.org/RefPack
    //
    // The bitstream uses 1- to 4-byte control codes that mix "copy this much
    // plaintext from input" with "back-copy this much from already-decompressed
    // output at offset X". 0xFC-0xFF terminates with up to 3 plaintext bytes.
    internal static class RefPack
    {
        public static byte[] Decompress(byte[] data, int decompressedSize)
        {
            var dst = new byte[decompressedSize];
            int srcPos = 0;
            int dstPos = 0;

            while (srcPos < data.Length)
            {
                int control1 = data[srcPos++];

                if (control1 <= 0x7F)
                {
                    // 0x00–0x7F: 2-byte control — short back-copy with up to 3 plaintext.
                    int control2 = data[srcPos++];
                    int plain = control1 & 0x03;
                    Array.Copy(data, srcPos, dst, dstPos, plain);
                    dstPos += plain; srcPos += plain;
                    if (dstPos == decompressedSize) break;

                    int offset = ((control1 & 0x60) << 3) + control2 + 1;
                    int copyLen = ((control1 & 0x1C) >> 2) + 3;
                    OffsetCopy(dst, dstPos - offset, dstPos, copyLen);
                    dstPos += copyLen;
                    if (dstPos == decompressedSize) break;
                }
                else if (control1 <= 0xBF)
                {
                    // 0x80–0xBF: 3-byte control — medium back-copy with up to 3 plaintext.
                    int control2 = data[srcPos++];
                    int control3 = data[srcPos++];
                    int plain = (control2 >> 6) & 0x03;
                    Array.Copy(data, srcPos, dst, dstPos, plain);
                    dstPos += plain; srcPos += plain;
                    if (dstPos == decompressedSize) break;

                    int offset = ((control2 & 0x3F) << 8) + control3 + 1;
                    int copyLen = (control1 & 0x3F) + 4;
                    OffsetCopy(dst, dstPos - offset, dstPos, copyLen);
                    dstPos += copyLen;
                    if (dstPos == decompressedSize) break;
                }
                else if (control1 <= 0xDF)
                {
                    // 0xC0–0xDF: 4-byte control — long back-copy with up to 3 plaintext.
                    int control2 = data[srcPos++];
                    int control3 = data[srcPos++];
                    int control4 = data[srcPos++];
                    int plain = control1 & 0x03;
                    Array.Copy(data, srcPos, dst, dstPos, plain);
                    dstPos += plain; srcPos += plain;
                    if (dstPos == decompressedSize) break;

                    int offset = ((control1 & 0x10) << 12) + (control2 << 8) + control3 + 1;
                    int copyLen = ((control1 & 0x0C) << 6) + control4 + 5;
                    OffsetCopy(dst, dstPos - offset, dstPos, copyLen);
                    dstPos += copyLen;
                    if (dstPos == decompressedSize) break;
                }
                else if (control1 <= 0xFB)
                {
                    // 0xE0–0xFB: 1-byte control — large plaintext copy, no back-copy.
                    int plain = ((control1 & 0x1F) << 2) + 4;
                    Array.Copy(data, srcPos, dst, dstPos, plain);
                    dstPos += plain; srcPos += plain;
                    if (dstPos == decompressedSize) break;
                }
                else
                {
                    // 0xFC–0xFF: end marker with up to 3 plaintext bytes.
                    int plain = control1 & 0x03;
                    Array.Copy(data, srcPos, dst, dstPos, plain);
                    dstPos += plain; srcPos += plain;
                    if (dstPos == decompressedSize) break;
                }
            }

            if (dstPos != decompressedSize)
                throw new InvalidDataException(
                    $"RefPack underflow: decoded {dstPos} bytes, expected {decompressedSize}");
            return dst;
        }

        // Byte-by-byte copy (NOT Buffer.BlockCopy) so overlapping regions resolve as
        // run-length expansion — the decoder's defining trick.
        private static void OffsetCopy(byte[] buf, int srcPos, int dstPos, int len)
        {
            for (int i = 0; i < len; i++) buf[dstPos + i] = buf[srcPos + i];
        }
    }
}