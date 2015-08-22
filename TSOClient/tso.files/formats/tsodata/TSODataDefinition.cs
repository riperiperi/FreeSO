using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.Files.Formats.tsodata
{
    public class TSODataDefinition
    {
        private List<List1Entry> List1;
        private List<List1Entry> List2;
        private List<List1Entry> List3;
        public List<StringTableEntry> Strings;

        public Struct[] Structs;
        public DerivedStruct[] DerivedStructs;

        public void Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var fileID = reader.ReadUInt32();
                this.List1 = ReadList(reader, false);
                this.List2 = ReadList(reader, false);
                this.List3 = ReadList(reader, true);

                var numStrings = reader.ReadUInt32();
                this.Strings = new List<StringTableEntry>();
                for (var i = 0; i < numStrings; i++)
                {
                    var stringEntry = new StringTableEntry();
                    stringEntry.ID = reader.ReadUInt32();
                    stringEntry.Value = ReadNullTerminatedString(reader);
                    stringEntry.Unknown = reader.ReadByte();
                    this.Strings.Add(stringEntry);
                }
            }

            var Structs = new List<Struct>();
            var DerivedStructs = new List<DerivedStruct>();


            foreach (var item in List1){
                var fields = new List<StructField>();

                foreach(var field in item.Entries){
                    fields.Add(new StructField {
                        ID = field.NameStringID,
                        Name = GetString(field.NameStringID),
                        TypeID = field.TypeStringID,
                        Classification = (StructFieldClassification)field.Unknonw
                    });
                }

                Structs.Add(new Struct {
                    ID = item.NameStringID,
                    Name = GetString(item.NameStringID),
                    Fields = fields.ToArray()
                });
            }

            foreach (var item in List2)
            {
                var fields = new List<StructField>();

                foreach (var field in item.Entries)
                {
                    fields.Add(new StructField
                    {
                        ID = field.NameStringID,
                        Name = GetString(field.NameStringID),
                        TypeID = field.TypeStringID,
                        Classification = (StructFieldClassification)field.Unknonw
                    });
                }

                Structs.Add(new Struct
                {
                    ID = item.NameStringID,
                    Name = GetString(item.NameStringID),
                    Fields = fields.ToArray()
                });
            }

            foreach (var item in List3)
            {
                var fields = new List<DerivedStructFieldMask>();

                foreach (var field in item.Entries)
                {
                    fields.Add(new DerivedStructFieldMask
                    {
                        ID = field.NameStringID,
                        Name = GetString(field.NameStringID),
                        Type = (DerivedStructFieldMaskType)field.Unknonw
                    });
                }

                DerivedStructs.Add(new DerivedStruct
                {
                    ID = item.NameStringID,
                    Parent = item.ParentStringID,
                    Name = GetString(item.NameStringID),
                    FieldMasks = fields.ToArray()
                });
            }

            this.Structs = Structs.ToArray();
            this.DerivedStructs = DerivedStructs.ToArray();
        }

        private string GetString(uint id)
        {
            var item = Strings.FirstOrDefault(x => x.ID == id);
            if (item == null)
            {
                return null;
            }
            return item.Value;
        }

        private string GetString(List<StringTableEntry> strings, uint id)
        {
            var item = strings.FirstOrDefault(x => x.ID == id);
            if (item == null)
            {
                return "";
            }
            return item.Value;
        }

        private string ReadNullTerminatedString(BinaryReader reader)
        {
            var result = "";
            while (true)
            {
                var ch = (char)reader.ReadByte();
                if (ch == '\0')
                {
                    break;
                }
                else
                {
                    result += ch;
                }
            }
            return result;
        }

        private List<List1Entry> ReadList(BinaryReader reader, bool parentID)
        {
            var list1Count = reader.ReadUInt32();

            var list1 = new List<List1Entry>();
            for (int i = 0; i < list1Count; i++)
            {
                var entry = new List1Entry();
                entry.NameStringID = reader.ReadUInt32();
                if (parentID == true)
                {
                    entry.ParentStringID = reader.ReadUInt32();
                }
                entry.Entries = new List<List1EntryEntry>();

                var subEntryCount = reader.ReadUInt32();
                for (int y = 0; y < subEntryCount; y++)
                {
                    var subEntry = new List1EntryEntry();
                    subEntry.NameStringID = reader.ReadUInt32();
                    subEntry.Unknonw = reader.ReadByte();
                    if (parentID == false)
                    {
                        subEntry.TypeStringID = reader.ReadUInt32();
                    }
                    entry.Entries.Add(subEntry);
                }

                list1.Add(entry);
            }
            return list1;
        }
    }

    public class StringTableEntry
    {
        public uint ID;
        public string Value;
        public byte Unknown;
    }

    public class List1Entry
    {
        public uint NameStringID;
        public uint ParentStringID;
        public List<List1EntryEntry> Entries;
    }


    public class List1EntryEntry
    {
        public uint NameStringID;
        public byte Unknonw;
        public uint TypeStringID;
    }

    public class Struct {
        public uint ID;
        public string Name;

        public StructField[] Fields;
    }

    public class StructField {
        public uint ID;
        public string Name;
        public StructFieldClassification Classification;
        public uint TypeID;
    }

    public enum StructFieldClassification
    {
        SingleField = 0,
        Unknown = 1,
        List = 2
    }

    public class DerivedStruct
    {
        public uint ID;
        public string Name;
        public uint Parent;

        public DerivedStructFieldMask[] FieldMasks;
    }

    public class DerivedStructFieldMask
    {
        public uint ID;
        public string Name;
        public DerivedStructFieldMaskType Type;
    }

    public enum DerivedStructFieldMaskType
    {
        KEEP = 0x01,
        REMOVE = 0x02
    }
}
