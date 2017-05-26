using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Compressed Floating Point
    /// 
    /// Used to contain the animation and translation vectors for animations
    /// separately from their "head" information. This makes it easier for the
    /// TS1 content system to scan them without loading all of the animation data.
    /// </summary>
    public class CFP
    {
        public static float[] Delta = Enumerable.Range(0, 256).Select(x => (float)(3.9676e-10 * (Math.Pow((double)x - 126, 3) * Math.Abs(x - 126)))).ToArray();

        public byte[] Data;

        public void Read(Stream stream)
        {
            using (var dest = new MemoryStream())
            {
                stream.CopyTo(dest);
                Data = dest.ToArray();
            }
        }

        public void EnrichAnim(Animation anim)
        {
            using (var stream = new MemoryStream(Data))
            {
                using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
                {

                    anim.Translations = new Vector3[anim.TranslationCount];
                    anim.Rotations = new Quaternion[anim.RotationCount];
                    //read the floating point values and give them to the anim.
                    //the anim supplies us with the count for the translation and rotation vector arrays.
                    ReadNFloats(io, anim.Translations.Count(), (i, x) => anim.Translations[i].X = -x);
                    ReadNFloats(io, anim.Translations.Count(), (i, y) => anim.Translations[i].Y = y);
                    ReadNFloats(io, anim.Translations.Count(), (i, z) => anim.Translations[i].Z = z);

                    ReadNFloats(io, anim.Rotations.Count(), (i, x) => anim.Rotations[i].X = x);
                    ReadNFloats(io, anim.Rotations.Count(), (i, y) => anim.Rotations[i].Y = -y);
                    ReadNFloats(io, anim.Rotations.Count(), (i, z) => anim.Rotations[i].Z = -z);
                    ReadNFloats(io, anim.Rotations.Count(), (i, w) => anim.Rotations[i].W = -w);
                }
            }
            Data = null;
        }

        public static void ReadNFloats(IoBuffer io, int floats, Action<int, float> output)
        {
            float lastValue = 0;
            for (int i=0; i<floats; i++)
            {
                var code = io.ReadByte();
                switch (code)
                {
                    case 0xFF:
                        lastValue = io.ReadFloat();
                        break;
                    case 0xFE:
                        //repeat count
                        var repeats = io.ReadUInt16();
                        for (int j=0; j<repeats; j++)
                        {
                            output(i++, lastValue);
                        }
                        break;
                    default:
                        lastValue += Delta[code];
                        break;
                }

                output(i, lastValue);
            }
        }
    }
}
