using FSO.IDE.EditorComponent.UI;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.DataView
{
    public class VMDataPropertyDescriptor : PropertyDescriptor
    {
        public static Dictionary<VMExtDataType, string> Categories = new Dictionary<VMExtDataType, string> {
            //tabs to force ordering
            { VMExtDataType.StackObject, "\t\t\t\t\t\t\tLocals" },
            { VMExtDataType.Parameter, "\t\t\t\t\t\t\t\tParameters" },
            { VMExtDataType.Local, "\t\t\t\t\t\t\tLocals" },
            { VMExtDataType.Attributes, "\t\t\t\t\t\tAttributes" },
            { VMExtDataType.PersonData, "\t\t\t\t\tPerson Data" },
            { VMExtDataType.Motives, "\t\t\t\tMotives" },
            { VMExtDataType.ObjectData, "\t\t\tObject Data" },
            { VMExtDataType.Temp, "\t\tTemps" },
            { VMExtDataType.TempXL, "\tTempXL" },
            { VMExtDataType.Globals, "Globals" }
        };

        public VMExtDataType Type;
        public int ID;

        public VMEntity Object;
        public VMStackFrame Frame;
        public UIBHAVEditor Editor; //where to send value change commands
        public string _Description;

        private int UseLastValue; //times to use last believed value before we poll the real entity.
        private int LastValue; //used to circumvent the fact that we don't change the value till a while later.

        public VMDataPropertyDescriptor(string name, string description, Attribute[] attrs, VMExtDataType type, int id, 
            VMEntity obj, VMStackFrame frame, UIBHAVEditor editor) 
            : base(name, attrs)
        {
            Type = type;
            ID = id;
            Object = obj;
            Frame = frame;
            Editor = editor;
            _Description = description;
        }
        public override Type ComponentType
        {
            get
            {
                return null;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return typeof(int);
            }
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            if (UseLastValue > 0)
            {
                UseLastValue--;
                return LastValue;
            }
            switch (Type)
            {
                case VMExtDataType.StackObject:
                    return (Frame.StackObject == null)?0:(int)Frame.StackObject.ObjectID;
                case VMExtDataType.Parameter:
                    if (ID >= Frame.Args.Length) return 0;
                    return (int)Frame.Args[ID];
                case VMExtDataType.Local:
                    if (ID >= Frame.Locals.Length) return 0;
                    return (int)Frame.Locals[ID];
                case VMExtDataType.Attributes:
                    return (int)Object.GetAttribute((ushort)ID);
                case VMExtDataType.Temp:
                    return (int)Object.Thread.TempRegisters[ID];
                case VMExtDataType.TempXL:
                    return Object.Thread.TempXL[ID];
                case VMExtDataType.ObjectData:
                    return (int)Object.GetValue((VMStackObjectVariable)ID);
                case VMExtDataType.PersonData:
                    try
                    {
                        return (int)((VMAvatar)Object).GetPersonData((VMPersonDataVariable)ID);
                    } catch (IndexOutOfRangeException) { return 0; }
                case VMExtDataType.Motives:
                    return (int)((VMAvatar)Object).GetMotiveData((VMMotive)ID);
                case VMExtDataType.Globals:
                    //maybe make this less unbelievably ugly
                    return (int)Object.Thread.Context.VM.GetGlobalValue((ushort)ID);
            }
            return 0;
        }

        public override void ResetValue(object component)
        {
            Editor.QueueValueChange(new VMModifyDataCommand(Type, ID, 0, Object, Frame));
        }

        public override void SetValue(object component, object value)
        {
            UseLastValue = 2;
            LastValue = (int)value;
            Editor.QueueValueChange(new VMModifyDataCommand(Type, ID, (int)value, Object, Frame));
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override string Description
        {
            get
            {
                return _Description;
            }
        }

        public override string Category
        {
            get
            {
                return Categories[Type];
            }
        }
    }
}
