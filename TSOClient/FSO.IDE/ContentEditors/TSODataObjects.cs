using FSO.Files.Formats.tsodata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.ContentEditors
{
    public class TSODataStructWrapper
    {
        public ListEntryEntry Item;
        
        public TSODataStructWrapper(ListEntryEntry item)
        {
            Item = item;
        }

        [Category("Struct Properties")]
        [Description("The name for this field/struct.")]
        public string Name
        {
            get
            {
                return Item.Parent.Parent.GetString(Item.NameStringID);
            }
            set
            {
                Item.Parent.Parent.SetString(Item.NameStringID, value);
            }
        }

        [Category("Struct Properties")]
        [Description("The type this field should have.")]
        [TypeConverter(typeof(TypeSelector))]
        public string FieldType
        {
            get
            {
                return Item.Parent.Parent.GetString(Item.TypeStringID);
            }
            set
            {
                Item.TypeStringID = Item.Parent.Parent.Strings.First(x => x.Value == value).ID;
            }
        }

        [Category("Struct Properties")]
        [Description("What kind of collection this field is.")]
        public StructFieldClassification FieldClass
        {
            get
            {
                return (StructFieldClassification)Item.TypeClass;
            }
            set
            {
                Item.TypeClass = (byte)value;
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
    }

    public class TSODataMaskWrapper
    {
        public ListEntryEntry Item;

        public TSODataMaskWrapper(ListEntryEntry item)
        {
            Item = item;
        }

        [Category("Mask Properties")]
        [TypeConverter(typeof(NameSelector))]
        [Description("The field to mask.")]
        public string MaskField
        {
            get
            {
                return Item.Parent.Parent.GetString(Item.NameStringID);
            }
            set
            {
                Item.NameStringID = Item.Parent.Parent.Strings.First(x => x.Value == value).ID;
            }
        }

        [Category("Mask Properties")]
        [Description("If this field should be kept or removed for this request.")]
        public DerivedStructFieldMaskType MaskMode
        {
            get
            {
                return (DerivedStructFieldMaskType)Item.TypeClass;
            }
            set
            {
                Item.TypeClass = (byte)value;
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

    public class TSODataDefinitionWrapper
    {
        public ListEntry Item;

        public TSODataDefinitionWrapper(ListEntry item)
        {
            Item = item;
        }

        [Category("Definition Properties")]
        [Description("The name for this object.")]
        public string Name
        {
            get
            {
                return Item.Parent.GetString(Item.NameStringID);
            }
            set
            {
                Item.Parent.SetString(Item.NameStringID, value);
            }
        }

        [Category("Definition Properties")]
        [TypeConverter(typeof(NameSelector))]
        [Description("The parent struct this mask references. (MASKS ONLY!)")]
        public string ParentStruct
        {
            get
            {
                return Item.Parent.GetString(Item.ParentStringID);
            }
            set
            {
                Item.ParentStringID = Item.Parent.Strings.First(x => x.Value == value).ID;
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
                    .Where(x => x.Category == StringTableType.Level2)
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Value)
                    .Select(x => x.Value)
                    .ToList()
                    );
            }
        }
    }
}
