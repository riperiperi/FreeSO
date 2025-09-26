using FSO.SimAntics.NetPlay.Model;
using FSO.LotView.Model;
using FSO.SimAntics.Model;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
                var savedWalls = VMSerializableUtils.ReadArray<WallTileSerialized>(reader, size);
                var level = new WallTile[size];
                for (int i = 0; i < size; i++) WallTileSerializer.Deserialize(in savedWalls[i], ref level[i]);
                Walls[l] = level;
            }

            Floors = new FloorTile[Stories][];
            for (int l = 0; l < Stories; l++)
            {
                Floors[l] = VMSerializableUtils.ReadArray<FloorTile>(reader, size);
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
                var savedWalls = new WallTileSerialized[level.Length];

                for (int i = 0; i < level.Length; i++)
                {
                    WallTileSerializer.SerializeInto(in level[i], ref savedWalls[i]);
                }

                VMSerializableUtils.WriteArray(writer, savedWalls);
            }

            foreach (var level in Floors)
            {
                VMSerializableUtils.WriteArray(writer, level);
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
                {
                    SerializeInto(io);
                    Preserialized = mem.ToArray();
                }
            }

            var test = new VMArchitectureMarshal();

            using (var mem = new MemoryStream(Preserialized))
            {
                using var io = new BinaryReader(mem);
                test.Deserialize(io);
            }
        }
    }

    public static class WallTileSerializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deserialize(in WallTileSerialized tile, ref WallTile output)
        {
            Span<WallTileSerialized> resultTruncated = MemoryMarshal.Cast<WallTile, WallTileSerialized>(MemoryMarshal.CreateSpan(ref output, 1));

            resultTruncated[0] = tile;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeInto(in WallTile tile, ref WallTileSerialized output)
        {
            ReadOnlySpan<WallTileSerialized> sourceTruncated = MemoryMarshal.Cast<WallTile, WallTileSerialized>(MemoryMarshal.CreateReadOnlySpan(in tile, 1));

            output = sourceTruncated[0];
        }
    }
}
