using FSO.Content.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FSO.Files.XA;
using FSO.Files.UTK;

namespace FSO.Content.Codecs
{
    public class SFXCodec : IContentCodec<DecodedSFX>
    {
        public override object GenDecode(Stream stream)
        {
            var dat = new byte[4];
            stream.Read(dat, 0, 4);
            stream.Seek(-4, SeekOrigin.Current);
            string head = new string(new char[] { (char)dat[0], (char)dat[1], (char)dat[2], (char)dat[3] });
            if (head.StartsWith("XA"))
            {
                return new DecodedSFX(1, new XAFile(ReadAllBytes(stream)).DecompressedData);
            }
            else if (head.StartsWith("UTM0"))
            {
                var utk = new UTKFile2(ReadAllBytes(stream));
                utk.UTKDecode();
                return new DecodedSFX(2, utk.DecompressedWav);
            }
            else
            {
                return new DecodedSFX(3, ReadAllBytes(stream)); //either wav or mp3.
            }
        }

        private byte[] ReadAllBytes(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }

    public class DecodedSFX {
        public int Filetype;
        public byte[] Data;

        public DecodedSFX(int filetype, byte[] data) {
            Filetype = filetype;
            Data = data;
        }
    }
}
