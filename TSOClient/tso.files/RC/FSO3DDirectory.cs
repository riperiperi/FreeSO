using FSO.Files.Utils;
using System.Diagnostics.CodeAnalysis;

namespace FSO.Files.RC
{
    public struct FSO3DRef(ushort id, uint fileID, uint typeID) : IEquatable<FSO3DRef>
    {
        public ushort ID = id;
        public uint FileID = fileID;
        public uint TypeID = typeID;

        public bool Equals(FSO3DRef other)
        {
            return (FileID == other.FileID && TypeID == other.TypeID && ID == other.ID);
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return obj is FSO3DRef oRef && Equals(oRef); 
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(ID, FileID, TypeID);
        }

        public static bool operator ==(FSO3DRef left, FSO3DRef right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FSO3DRef left, FSO3DRef right)
        {
            return !(left == right);
        }
    }

    public class FSO3DDirectoryEntry
    {
        public int ID;
        public string Filename;
        public Dictionary<int, FSO3DRef> Meshes;
        public Dictionary<int, FSO3DRef> Textures;

        public void Read(IoBuffer io)
        {
            ID = io.ReadInt32();
            Filename = io.ReadVariableLengthPascalString();

            var meshCount = io.ReadInt32();
            Meshes = new Dictionary<int, FSO3DRef>(meshCount);
            for (int i = 0; i < meshCount; i++)
            {
                var mesh = new FSO3DRef(io.ReadUInt16(), io.ReadUInt32(), io.ReadUInt32());
                Meshes[mesh.ID] = mesh;
            }

            var textureCount = io.ReadInt32();
            Textures = new Dictionary<int, FSO3DRef>(textureCount);
            for (int i = 0; i < textureCount; i++)
            {
                var tex = new FSO3DRef(io.ReadUInt16(), io.ReadUInt32(), io.ReadUInt32());
                Textures[tex.ID] = tex;
            }
        }

        public void Write(IoWriter io)
        {
            io.WriteInt32(ID);
            io.WriteVariableLengthPascalString(Filename);

            io.WriteInt32(Meshes.Count);
            foreach (var mesh in Meshes.Values)
            {
                io.WriteUInt16(mesh.ID);
                io.WriteUInt32(mesh.FileID);
                io.WriteUInt32(mesh.TypeID);
            }

            io.WriteInt32(Textures.Count);
            foreach (var tex in Textures.Values)
            {
                io.WriteUInt16(tex.ID);
                io.WriteUInt32(tex.FileID);
                io.WriteUInt32(tex.TypeID);
            }
        }
    }

    public class FSO3DDirectory
    {
        private const int CURRENT_VERSION = 1;

        public int Version = CURRENT_VERSION;
        public Dictionary<string, FSO3DDirectoryEntry> Entries;

        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var fdir = io.ReadCString(4);
                if (fdir != "fDIR") throw new Exception("Invalid FSO3DDirectory!");
                Version = io.ReadInt32();

                int entryCount = io.ReadInt32();
                Entries = new Dictionary<string, FSO3DDirectoryEntry>(entryCount);
                for (int i = 0; i < entryCount; i++)
                {
                    var entry = new FSO3DDirectoryEntry();
                    entry.Read(io);
                    Entries[entry.Filename] = entry;
                }
            }
        }

        public void Write(Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteCString("fDIR", 4);
                io.WriteInt32(Version);
                io.WriteInt32(Entries.Count);
                foreach (var entry in Entries.Values)
                {
                    entry.Write(io);
                }
            }
        }
    }
}
