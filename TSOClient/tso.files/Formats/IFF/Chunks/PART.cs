using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class PART : IffChunk
    {
        public static PART BROKEN = new PART()
        {
            Gravity = 0.15f,
            RandomVel = 0.15f,
            RandomRotVel = 1f,
            Size = 0.75f,
            SizeVel = 2.5f,
            Duration = 3f,
            FadeIn = 0.15f,
            FadeOut = 0.6f,
            SizeVariation = 0.4f,

            TargetColor = Color.Gray,
            TargetColorVar = 0.5f,

            Frequency = 6f,

            ChunkID = 256
        };

        public static int CURRENT_VERSION = 1;
        public int Version = CURRENT_VERSION;
        public int Type = 0; // default/manualbounds

        public float Frequency;
        public ushort TexID; //id for MTEX resource
        public BoundingBox Bounds;

        public Vector3 Velocity;
        public float Gravity = -0.8f;
        public float RandomVel;
        public float RandomRotVel;
        public float Size = 1;
        public float SizeVel;
        public float Duration = 1;
        public float FadeIn;
        public float FadeOut;
        public float SizeVariation;
        public Color TargetColor;
        public float TargetColorVar;
        public int Particles = 15;

        public Vector4[] Parameters = null;

        //(deltax, deltay, deltaz, gravity)
        //(deltavar, rotdeltavar, size, sizevel)
        //(duration, fadein, fadeout, sizevar)
        //(targetColor.rgb, variation)

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Version = io.ReadInt32();
                Type = io.ReadInt32();

                Frequency = io.ReadFloat();
                TexID = io.ReadUInt16();
                Particles = io.ReadInt32();
                if (Type == 1)
                {
                    Bounds = new BoundingBox(
                        new Vector3(io.ReadFloat(), io.ReadFloat(), io.ReadFloat()),
                        new Vector3(io.ReadFloat(), io.ReadFloat(), io.ReadFloat()));
                }

                Velocity = new Vector3(io.ReadFloat(), io.ReadFloat(), io.ReadFloat());
                Gravity = io.ReadFloat();
                RandomVel = io.ReadFloat();
                RandomRotVel = io.ReadFloat();
                Size = io.ReadFloat();
                SizeVel = io.ReadFloat();
                Duration = io.ReadFloat();
                FadeIn = io.ReadFloat();
                FadeOut = io.ReadFloat();
                SizeVariation = io.ReadFloat();
                TargetColor.PackedValue = io.ReadUInt32();
                TargetColorVar = io.ReadFloat();
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(Version);
                io.WriteInt32(Type);

                io.WriteFloat(Frequency);
                io.WriteUInt16(TexID);
                io.WriteInt32(Particles);
                if (Type == 1)
                {
                    io.WriteFloat(Bounds.Min.X);
                    io.WriteFloat(Bounds.Min.Y);
                    io.WriteFloat(Bounds.Min.Z);

                    io.WriteFloat(Bounds.Max.X);
                    io.WriteFloat(Bounds.Max.Y);
                    io.WriteFloat(Bounds.Max.Z);
                }

                io.WriteFloat(Velocity.X);
                io.WriteFloat(Velocity.Y);
                io.WriteFloat(Velocity.Z);

                io.WriteFloat(Gravity);
                io.WriteFloat(RandomVel);
                io.WriteFloat(RandomRotVel);
                io.WriteFloat(Size);
                io.WriteFloat(SizeVel);
                io.WriteFloat(Duration);
                io.WriteFloat(FadeIn);
                io.WriteFloat(FadeOut);
                io.WriteFloat(SizeVariation);
                io.WriteUInt32(TargetColor.PackedValue);
                io.WriteFloat(TargetColorVar);
            }
            return true;
        }

        public void BakeParameters()
        {
            Parameters = new Vector4[4];
            Parameters[0] = new Vector4(Velocity, Gravity);
            Parameters[1] = new Vector4(RandomVel, RandomRotVel, Size, SizeVel);
            Parameters[2] = new Vector4(Duration, FadeIn, FadeOut, SizeVariation);
            Parameters[3] = TargetColor.ToVector4();
            Parameters[3].W = TargetColorVar;
        }
    }
}
