using FSO.SimAntics.NetPlay.Model;
using System.Linq;
using System.IO;
using FSO.LotView.Model;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Marshals
{
    public class VMArchitectureMarshal : VMSerializable
    {
        public int Width;
        public int Height;
        public int Stories;
        public VMArchitectureTerrain Terrain;

        //public for quick access and iteration. 
        //Make sure that on modifications you signal so that the render updates.
        public WallTile[][] Walls;
        public FloorTile[][] Floors;

        public bool WallsDirty;
        public bool FloorsDirty;

        public uint RoofStyle = 16;
        public float RoofPitch = 0.66f;

        public VMResourceIDMarshal IDMap;
        public bool[] FineBuildableArea;
        public bool BuildBuyEnabled = true;

        public byte[] Preserialized;

        public int Version;
        public VMArchitectureMarshal() { }
        public VMArchitectureMarshal(int version) { Version = version; }
        public void Deserialize(BinaryReader reader)
        {
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            Stories = reader.ReadInt32();
            Terrain = new VMArchitectureTerrain(Width, Height);
            Terrain.Version = Version;
            if (Version > 6) Terrain.Deserialize(reader);

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

            if (Version > 13)
            {
                RoofStyle = reader.ReadUInt32();
                RoofPitch = reader.ReadSingle();
            }

            if (Version > 21)
            {
                var hasIDMap = reader.ReadBoolean();
                if (hasIDMap)
                {
                    IDMap = new VMResourceIDMarshal();
                    IDMap.Deserialize(reader);
                }
            }

            if (Version > 22)
            {
                var hasFineBuild = reader.ReadBoolean();
                if (hasFineBuild)
                {
                    FineBuildableArea = reader.ReadBytes(size).Select(x => x>0).ToArray();
                }
            }
            if (Version > 25) BuildBuyEnabled = reader.ReadBoolean();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            if (Preserialized != null)
            {
                writer.Write(Preserialized);
                return;
            }
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Stories);
            Terrain.SerializeInto(writer);

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

            writer.Write(RoofStyle);
            writer.Write(RoofPitch);

            writer.Write(IDMap != null);
            if (IDMap != null) IDMap.SerializeInto(writer);

            writer.Write(FineBuildableArea != null);
            if (FineBuildableArea != null) writer.Write(FineBuildableArea.Select(x => (byte)(x?1:0)).ToArray());

            writer.Write(BuildBuyEnabled);
        }

        public void Preserialize()
        {
            using (var mem = new MemoryStream())
            {
                using (var io = new BinaryWriter(mem))
                    SerializeInto(io);
                Preserialized = mem.ToArray();
            }
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
