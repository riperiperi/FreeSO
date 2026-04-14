using FSO.Files.Utils;
using System.IO;

namespace FSO.Vitaboy
{
    public class BCF
    {
        public Skeleton[] Skeletons;
        public Appearance[] Appearances;
        public Animation[] Animations;

        public BCF(Skeleton[] skeletons, Appearance[] appearances, Animation[] animations)
        {
            Skeletons = skeletons;
            Appearances = appearances;
            Animations = animations;
        }

        public BCF(Stream stream, bool cmx)
        {
            using (var io = (cmx) ? new BCFReadString(stream, true) : (BCFReadProxy)IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                int skelCount = io.ReadInt32();
                // Sanity checks for corrupted files
                if (skelCount < 0 || skelCount > 1000)
                {
                    throw new InvalidDataException($"Invalid skeleton count: {skelCount}. File may be corrupted or in wrong format.");
                }
                
                Skeletons = new Skeleton[skelCount];
                for (int i = 0; i < Skeletons.Length; i++)
                {
                    Skeletons[i] = new Skeleton();
                    Skeletons[i].Read(io, true);
                    Skeletons[i].ParentBCF = this;
                }
                
                int appCount = io.ReadInt32();
                if (appCount < 0 || appCount > 10000)
                {
                    throw new InvalidDataException($"Invalid appearance count: {appCount}. File may be corrupted or in wrong format.");
                }
                
                Appearances = new Appearance[appCount];
                for (int i = 0; i < Appearances.Length; i++)
                {
                    Appearances[i] = new Appearance();
                    Appearances[i].ReadBCF(io);
                    Appearances[i].ParentBCF = this;
                }
                
                int animCount = io.ReadInt32();
                if (animCount < 0 || animCount > 10000)
                {
                    throw new InvalidDataException($"Invalid animation count: {animCount}. File may be corrupted or in wrong format.");
                }
                
                Animations = new Animation[animCount];
                for (int i = 0; i < Animations.Length; i++)
                {
                    Animations[i] = new Animation();
                    Animations[i].Read(io, true);
                    Animations[i].ParentBCF = this;
                }
            }
        }

        public void Write(Stream stream, bool cmx)
        {
            using (var io = (cmx) ? new BCFWriteString(stream, true) : (BCFWriteProxy)IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(Skeletons.Length);
                for (int i = 0; i < Skeletons.Length; i++)
                {
                    Skeletons[i].Write(io, true);
                }
                io.WriteInt32(Appearances.Length);
                for (int i = 0; i < Appearances.Length; i++)
                {
                    Appearances[i].WriteBCF(io);
                }
                io.WriteInt32(Animations.Length);
                for (int i = 0; i < Animations.Length; i++)
                {
                    Animations[i].Write(io, true);
                }
            }
        }
    }
}
