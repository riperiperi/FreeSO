using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace FSO.Files.Formats.tsodata
{
    public class TSODataDefinition
    {
        public List<ListEntry> List1;
        public List<ListEntry> List2;
        public List<ListEntry> List3;
        public List<StringTableEntry> Strings;

        public Struct[] Structs;
        public DerivedStruct[] DerivedStructs;
        public uint FileID;

        public static TSODataDefinition Active;

        private Dictionary<string, Struct> StructsByName = new Dictionary<string, Struct>();

        public void Read(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                FileID = reader.ReadUInt32();
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
                    stringEntry.Category = (StringTableType)reader.ReadByte();
                    this.Strings.Add(stringEntry);
                }
            }
            Activate();
        }

        public void Write(Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(FileID);
                WriteList(List1, writer, false);
                WriteList(List2, writer, false);
                WriteList(List3, writer, true);

                writer.Write(Strings.Count);
                foreach (var str in Strings)
                {
                    writer.Write(str.ID);
                    writer.Write(Encoding.ASCII.GetBytes(str.Value));
                    writer.Write((byte)0);
                    writer.Write((byte)str.Category);
                }
            }
        }

        public void Activate()
        {
            var Structs = new List<Struct>();
            var DerivedStructs = new List<DerivedStruct>();

            //1st level structs. all fields are primitive types
            foreach (var item in List1)
            {
                var fields = new List<StructField>();

                foreach (var field in item.Entries)
                {
                    if (field.TypeStringID == 0xA99AF3AC) Console.WriteLine("unknown value: " + GetString(item.NameStringID) + "::" + GetString(field.NameStringID));
                    fields.Add(new StructField
                    {
                        ID = field.NameStringID,
                        Name = GetString(field.NameStringID),
                        TypeID = field.TypeStringID,
                        Classification = (StructFieldClassification)field.TypeClass,
                        ParentID = item.NameStringID
                    });
                }

                Structs.Add(new Struct
                {
                    ID = item.NameStringID,
                    Name = GetString(item.NameStringID),
                    Fields = fields.ToList()
                });
            }

            //2nd level structs. fields can be first level structs
            //note: this might be a hard limit in tso, but it is not particularly important in freeso
            //      the game will even behave correctly if a 2nd level references another 2nd level,
            //      though a circular reference will likely still break everything.
            foreach (var item in List2)
            {
                var fields = new List<StructField>();

                foreach (var field in item.Entries)
                {
                    if (field.TypeStringID == 0xA99AF3AC) Console.WriteLine("unknown value: " + GetString(item.NameStringID) + "::" + GetString(field.NameStringID));
                    fields.Add(new StructField
                    {
                        ID = field.NameStringID,
                        Name = GetString(field.NameStringID),
                        TypeID = field.TypeStringID,
                        Classification = (StructFieldClassification)field.TypeClass,
                        ParentID = item.NameStringID
                    });
                }

                Structs.Add(new Struct
                {
                    ID = item.NameStringID,
                    Name = GetString(item.NameStringID),
                    Fields = fields.ToList()
                });
            }

            //derived structs. serve as valid subsets of fields of an entity that the client can request.
            foreach (var item in List3)
            {
                var fields = new List<DerivedStructFieldMask>();

                foreach (var field in item.Entries)
                {
                    if (field.TypeStringID == 0xA99AF3AC) Console.WriteLine("unknown value: " + GetString(item.NameStringID) + "::" + GetString(field.NameStringID));
                    fields.Add(new DerivedStructFieldMask
                    {
                        ID = field.NameStringID,
                        Name = GetString(field.NameStringID),
                        Type = (DerivedStructFieldMaskType)field.TypeClass
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

            foreach (var _struct in Structs)
            {
                StructsByName.Add(_struct.Name, _struct);
            }

            //InjectStructs();
            Active = this;
        }

        private void InjectStructs()
        {
            //this is just an example of how to do this.
            //todo: a format we can easily create and read from to provide these new fields

            StructsByName["Lot"].Fields.Add(new StructField()
            {
                Name = "Lot_SkillGamemode",
                ID = 0xaabbccdd,
                Classification = StructFieldClassification.SingleField,
                ParentID = StructsByName["Lot"].ID,
                TypeID = 1768755593 //uint32
            });

            var fields = DerivedStructs[17].FieldMasks.ToList();
            fields.Add(new DerivedStructFieldMask()
            {
                ID = 0xaabbccdd,
                Name = "Lot_SkillGamemode",
                Type = DerivedStructFieldMaskType.KEEP
            });
            DerivedStructs[17].FieldMasks = fields.ToArray();
        }

        public Struct GetStructFromValue(object value)
        {
            if (value == null) { return null; }
            return GetStruct(value.GetType());
        }

        public Struct GetStruct(Type type)
        {
            return GetStruct(type.Name);
        }

        public Struct GetStruct(string name)
        {
            if (StructsByName.ContainsKey(name))
            {
                return StructsByName[name];
            }
            return null;
        }

        public Struct GetStruct(uint id)
        {
            return Structs.FirstOrDefault(x => x.ID == id);
        }

        public StringTableType GetStringType(uint id)
        {
            var item = Strings.FirstOrDefault(x => x.ID == id);
            if (item == null)
            {
                return StringTableType.Field;
            }
            return item.Category;
        }

        public string GetString(uint id)
        {
            var item = Strings.FirstOrDefault(x => x.ID == id);
            if (item == null)
            {
                return null;
            }
            return item.Value;
        }


        public void SetString(uint id, string value)
        {
            var item = Strings.FirstOrDefault(x => x.ID == id);
            if (item == null)
            {
                return;
            }
            item.Value = value;
        }

        public void RemoveString(uint id)
        {
            Strings.RemoveAll(x => x.ID == id);
        }

        public string GetString(List<StringTableEntry> strings, uint id)
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

        private List<ListEntry> ReadList(BinaryReader reader, bool parentID)
        {
            var list1Count = reader.ReadUInt32();

            var list1 = new List<ListEntry>();
            for (int i = 0; i < list1Count; i++)
            {
                var entry = new ListEntry();
                entry.Parent = this;
                entry.NameStringID = reader.ReadUInt32();
                if (parentID)
                {
                    entry.ParentStringID = reader.ReadUInt32();
                }
                entry.Entries = new List<ListEntryEntry>();

                var subEntryCount = reader.ReadUInt32();
                for (int y = 0; y < subEntryCount; y++)
                {
                    var subEntry = new ListEntryEntry();
                    subEntry.Parent = entry;
                    subEntry.NameStringID = reader.ReadUInt32();
                    subEntry.TypeClass = reader.ReadByte();
                    if (!parentID)
                    {
                        subEntry.TypeStringID = reader.ReadUInt32();
                    }
                    entry.Entries.Add(subEntry);
                }

                list1.Add(entry);
            }
            return list1;
        }

        private void WriteList(List<ListEntry> list, BinaryWriter writer, bool parentID)
        {
            writer.Write(list.Count);
            foreach (var entry in list)
            {
                writer.Write(entry.NameStringID);
                if (parentID) writer.Write(entry.ParentStringID);
                writer.Write(entry.Entries.Count);
                foreach (var subEntry in entry.Entries)
                {
                    writer.Write(subEntry.NameStringID);
                    writer.Write(subEntry.TypeClass);
                    if (!parentID) writer.Write(subEntry.TypeStringID);
                }
            }
        }
    }

    public enum StringTableType : byte
    {
        Field = 1,
        Primitive = 2,
        Level1 = 3,
        Level2 = 4,
        Derived = 5
    }

    public class StringTableEntry
    {
        public uint ID;
        public string Value;
        public StringTableType Category;
    }

    public class ListEntry
    {
        public TSODataDefinition Parent;

        public uint NameStringID { get; set; }
        public uint ParentStringID { get; set; }
        public List<ListEntryEntry> Entries;
    }

    public class ListEntryEntry
    {
        public ListEntry Parent;

        public uint NameStringID;
        public byte TypeClass;
        public uint TypeStringID;

        [Category("Struct Properties")]
        [Description("The name for this field/struct. (ONLY FOR STRUCTS)")]
        public string Name
        {
            get
            {
                return Parent.Parent.GetString(NameStringID);
            }
            set
            {
                Parent.Parent.SetString(NameStringID, value);
            }
        }

        [Category("Struct Properties")]
        [Description("The type this field should have. (ONLY FOR STRUCTS)")]
        [TypeConverter(typeof(TypeSelector))]
        public string FieldType {
            get
            {
                return Parent.Parent.GetString(TypeStringID);
            }
            set
            {
                TypeStringID = Parent.Parent.Strings.First(x => x.Value == value).ID;
            }
        }

        [Category("Struct Properties")]
        [Description("What kind of collection this field is. (ONLY FOR STRUCTS)")]
        public StructFieldClassification FieldClass {
            get {
                return (StructFieldClassification)TypeClass;
            }
            set
            {
                TypeClass = (byte)value;
            }
        }

        private class TypeSelector : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(
                    TSODataDefinition.Active.Strings
                    .Where(x => x.Category == StringTableType.Level1 || x.Category == StringTableType.Primitive)
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Value)
                    .Select(x => x.Value)
                    .ToList()
                    );
            }
        }

        [Category("Mask Properties")]
        [TypeConverter(typeof(NameSelector))]
        [Description("The field to mask. (ONLY FOR MASKS)")]
        public string MaskField
        {
            get
            {
                return Parent.Parent.GetString(NameStringID);
            }
            set
            {
                NameStringID = Parent.Parent.Strings.First(x => x.Value == value).ID;
            }
        }

        [Category("Mask Properties")]
        [Description("If this field should be kept or removed for this request. (ONLY FOR MASKS)")]
        public DerivedStructFieldMaskType MaskMode
        {
            get
            {
                return (DerivedStructFieldMaskType)TypeClass;
            }
            set
            {
                TypeClass = (byte)value;
            }
        }

        private class NameSelector : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(
                    TSODataDefinition.Active.Strings
                    .Where(x => x.Category == StringTableType.Field)
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Value)
                    .Select(x => x.Value)
                    .ToList()
                    );
            }
        }
    }

    public class Struct {
        public uint ID;
        public string Name;

        public List<StructField> Fields;
    }

    public class StructField {
        public uint ID;
        public string Name;
        public StructFieldClassification Classification;
        public uint TypeID;
        public uint ParentID;
    }

    public enum StructFieldClassification
    {
        SingleField = 0,
        Map = 1,
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
