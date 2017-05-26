using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Vitaboy
{
    public class BCF
    {
        public Skeleton[] Skeletons;
        public Appearance[] Appearances;
        public Animation[] Animations;

        public BCF(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Skeletons = new Skeleton[io.ReadInt32()];
                for (int i = 0; i < Skeletons.Length; i++)
                {
                    Skeletons[i] = new Skeleton();
                    Skeletons[i].Read(stream, true);
                }
                Appearances = new Appearance[io.ReadInt32()];
                for (int i = 0; i < Appearances.Length; i++)
                {
                    Appearances[i] = new Appearance();
                    Appearances[i].ReadBCF(stream);
                }
                Animations = new Animation[io.ReadInt32()];
                for (int i = 0; i < Animations.Length; i++)
                {
                    Animations[i] = new Animation();
                    Animations[i].Read(stream, true);
                }
            }
        }
    }
}
