using FSO.Content.Model;
using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOSurroundingTerrain : VMSerializable
    {
        public TerrainBlend[,] BlendN = new TerrainBlend[3,3];
        public byte[,] Roads = new byte[3,3];
        public byte[,] Height = new byte[4,4];

        public void Deserialize(BinaryReader reader)
        {
            for (int y=0; y<3; y++)
            {
                for (int x=0; x<3; x++)
                {
                    BlendN[x, y].Base = (TerrainType)reader.ReadByte();
                    BlendN[x, y].Blend = (TerrainType)reader.ReadByte();
                    BlendN[x, y].AdjFlags = reader.ReadByte();
                    BlendN[x, y].WaterFlags = reader.ReadByte();
                }
            }

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Roads[x, y] = reader.ReadByte();
                }
            }

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    Height[x, y] = reader.ReadByte();
                }
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    writer.Write((byte)BlendN[x, y].Base);
                    writer.Write((byte)BlendN[x, y].Blend);
                    writer.Write(BlendN[x, y].AdjFlags);
                    writer.Write(BlendN[x, y].WaterFlags);
                }
            }

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    writer.Write(Roads[x, y]);
                }
            }

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    writer.Write(Height[x, y]);
                }
            }
        }
    }
}
