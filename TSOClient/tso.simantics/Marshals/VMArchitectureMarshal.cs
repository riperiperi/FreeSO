using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.LotView.Model;

namespace FSO.SimAntics.Marshals
{
    public class VMArchitectureMarshal : VMSerializable
    {
        public int Width;
        public int Height;
        public int Stories;

        //public for quick access and iteration. 
        //Make sure that on modifications you signal so that the render updates.
        public WallTile[][] Walls;
        public FloorTile[][] Floors;

        public bool WallsDirty;
        public bool FloorsDirty;
        public void Deserialize(BinaryReader reader)
        {
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            Stories = reader.ReadInt32();

            var size = Width * Height;

            Walls = new WallTile[Stories][];
            for (int l=0;l<Stories;l++)
            {
                Walls[l] = new WallTile[size];
                for (int i = 0; i < size; i++) Walls[l][i] = WallTileSerializer.Deserialize(reader);
            }

            Floors = new FloorTile[Stories][];
            for (int l = 0; l < Stories; l++)
            {
                Floors[l] = new FloorTile[size];
                for (int i = 0; i < size; i++) Floors[l][i] = new FloorTile { Pattern = reader.ReadUInt16() };
            }

            WallsDirty = reader.ReadBoolean();
            FloorsDirty = reader.ReadBoolean();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Stories);

            foreach (var level in Walls)
            {
                foreach (var wall in level)
                {
                    WallTileSerializer.SerializeInto(wall, writer);
                }
            }

            foreach (var level in Floors)
            {
                foreach (var floor in level)
                {
                    writer.Write(floor.Pattern);
                }
            }

            writer.Write(WallsDirty);
            writer.Write(FloorsDirty);
        }
    }

    public static class WallTileSerializer
    {
        public static WallTile Deserialize(BinaryReader reader)
        {
            var result = new WallTile();
            result.Segments = (WallSegments)reader.ReadByte();
            result.TopLeftPattern = reader.ReadUInt16();
            result.TopRightPattern = reader.ReadUInt16();
            result.BottomLeftPattern = reader.ReadUInt16();
            result.BottomRightPattern = reader.ReadUInt16();
            result.TopLeftStyle = reader.ReadUInt16();
            result.TopRightStyle = reader.ReadUInt16();
            return result;
        }

        public static void SerializeInto(WallTile wall, BinaryWriter writer)
        {
            writer.Write((byte)wall.Segments);
            writer.Write(wall.TopLeftPattern);
            writer.Write(wall.TopRightPattern);
            writer.Write(wall.BottomLeftPattern);
            writer.Write(wall.BottomRightPattern);
            writer.Write(wall.TopLeftStyle);
            writer.Write(wall.TopRightStyle);
        }
    }
}
