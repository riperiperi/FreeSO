using FSO.Files.Utils;

namespace FSO.Files.RC
{
    public class FSO3DPackageMetadata
    {
        public string Name;
        public string ID;
        public string Description;
        public string Url;

        public void Read(IoBuffer io)
        {
            Name = io.ReadVariableLengthPascalString();
            ID = io.ReadVariableLengthPascalString();
            Description = io.ReadVariableLengthPascalString();
            Url = io.ReadVariableLengthPascalString();
        }

        public void Write(IoWriter io)
        {
            io.WriteVariableLengthPascalString(Name);
            io.WriteVariableLengthPascalString(ID);
            io.WriteVariableLengthPascalString(Description);
            io.WriteVariableLengthPascalString(Url);
        }
    }

    public class FSO3DGroupMetadata
    {
        public string Name;
        public string Description;

        public void Read(IoBuffer io)
        {
            Name = io.ReadVariableLengthPascalString();
            Description = io.ReadVariableLengthPascalString();
        }

        public void Write(IoWriter io)
        {
            io.WriteVariableLengthPascalString(Name);
            io.WriteVariableLengthPascalString(Description);
        }
    }

    public class FSO3DAuthorMetadata
    {
        public string Name;
        public string Description;

        public void Read(IoBuffer io)
        {
            Name = io.ReadVariableLengthPascalString();
            Description = io.ReadVariableLengthPascalString();
        }

        public void Write(IoWriter io)
        {
            io.WriteVariableLengthPascalString(Name);
            io.WriteVariableLengthPascalString(Description);
        }
    }

    public class FSO3DCreditsGroup
    {
        public FSO3DGroupMetadata Metadata;
        public List<FSO3DRef> Files;

        public void Read(IoBuffer io)
        {
            Metadata = new FSO3DGroupMetadata();
            Metadata.Read(io);

            var fileCount = io.ReadInt32();
            Files = new List<FSO3DRef>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                Files.Add(new FSO3DRef(io.ReadUInt16(), io.ReadUInt32(), io.ReadUInt32()));
            }
        }

        public void Write(IoWriter io)
        {
            Metadata.Write(io);

            io.WriteInt32(Files.Count);
            foreach (var file in Files)
            {
                io.WriteUInt16(file.ID);
                io.WriteUInt32(file.FileID);
                io.WriteUInt32(file.TypeID);
            }
        }
    }

    public class FSO3DCreditsAuthor
    {
        public FSO3DAuthorMetadata Metadata;
        public List<FSO3DCreditsGroup> Groups;

        public void Read(IoBuffer io)
        {
            Metadata = new FSO3DAuthorMetadata();
            Metadata.Read(io);

            var groupCount = io.ReadInt32();
            Groups = [];
            for (int i = 0; i < groupCount; i++)
            {
                var group = new FSO3DCreditsGroup();
                group.Read(io);
                Groups.Add(group);
            }
        }

        public void Write(IoWriter io)
        {
            Metadata.Write(io);

            io.WriteInt32(Groups.Count);
            foreach (var group in Groups)
            {
                group.Write(io);
            }
        }
    }

    public class FSO3DCredits
    {
        private const int CURRENT_VERSION = 1;

        public int Version = CURRENT_VERSION;
        public FSO3DPackageMetadata Metadata;
        public List<FSO3DCreditsAuthor> Authors;

        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var fdir = io.ReadCString(4);
                if (fdir != "fCRE") throw new Exception("Invalid FSO3DCredits!");
                Version = io.ReadInt32();

                Metadata = new FSO3DPackageMetadata();
                Metadata.Read(io);

                int authorCount = io.ReadInt32();
                Authors = [];
                for (int i = 0; i < authorCount; i++)
                {
                    var author = new FSO3DCreditsAuthor();
                    author.Read(io);
                    Authors.Add(author);
                }
            }
        }

        public void Write(Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteCString("fCRE", 4);
                io.WriteInt32(Version);

                Metadata.Write(io);

                io.WriteInt32(Authors.Count);
                foreach (var author in Authors)
                {
                    author.Write(io);
                }
            }
        }
    }
}
