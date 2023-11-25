using FSO.Common.Serialization;
using FSO.Files.Formats.tsodata;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FSO.Server.Protocol.Voltron.DataService
{
    /// <summary>
    /// TODO: Rewrite this to have much tighter performance
    /// </summary>
    public class cTSOSerializer
    {
        
        //private static Logger LOG = LogManager.GetCurrentClassLogger();

        public static cTSOSerializer INSTANCE = null;
        public static cTSOSerializer Get(){
            return INSTANCE;
        }


        public const uint cTSOValue_bool = 0x696D1183;
        public const uint cTSOValue_uint8 = 0xC976087C;
        public const uint cTSOValue_uint16 = 0xE9760891;
        public const uint cTSOValue_uint32 = 0x696D1189;
        public const uint cTSOValue_uint64 = 0x69D3E3DB;
        public const uint cTSOValue_sint8 = 0xE976088A;
        public const uint cTSOValue_sint16 = 0xE9760897;
        public const uint cTSOValue_sint32 = 0x896D1196;
        public const uint cTSOValue_sint64 = 0x89D3E3EF;
        public const uint cTSOValue_string = 0x896D1688;
        public const uint cTSOValue_property = 0xA96E7E5B;

        private TSODataDefinition Format;
        private Dictionary<uint, Type> ClassesById = new Dictionary<uint, Type>();
        private Dictionary<Type, uint> IdByClass = new Dictionary<Type, uint>();

        private Dictionary<uint, Type> cNetMessageParametersById = new Dictionary<uint, Type>();


        public cTSOSerializer(TSODataDefinition data){
            INSTANCE = this;

            this.Format = data;

            //Scan for classes with decorations
            var assembly = Assembly.GetAssembly(typeof(cTSOSerializer));
            
            foreach (Type type in assembly.GetTypes())
            {
                System.Attribute[] attributes = System.Attribute.GetCustomAttributes(type);

                foreach (Attribute attribute in attributes)
                {
                    if (attribute is clsid){
                        ClassesById.Add(((clsid)attribute).Value, type);
                        IdByClass.Add(type, ((clsid)attribute).Value);
                    }else if(attribute is cTSONetMessageParameter)
                    {
                        var param = (cTSONetMessageParameter)attribute;
                        object paramValue = param.Value;

                        /*if(paramValue is DBRequestType){
                            paramValue = ((DBRequestType)paramValue).GetRequestID();
                        }else if(paramValue is DBResponseType)
                        {
                            paramValue = ((DBResponseType)paramValue).GetResponseID();
                        }*/
                        cNetMessageParametersById.Add((uint)paramValue, type);
                    }
                }
            }
        }
        

        public object Deserialize(uint clsid, IoBuffer buffer)
        {
            if (cNetMessageParametersById.ContainsKey(clsid))
            {
                var instance = (IoBufferDeserializable)Activator.CreateInstance(cNetMessageParametersById[clsid]);
                //instance.Deserialize(buffer);
                return instance;
            }
            else if (ClassesById.ContainsKey(clsid))
            {
                var instance = (IoBufferDeserializable)Activator.CreateInstance(ClassesById[clsid]);
                //instance.Deserialize(buffer);
                return instance;
            }
            else if(clsid == cTSOValue_string)
            {
                return buffer.GetPascalVLCString();
            }
            return null;
        }

        public object Serialize(object obj)
        {
            if(obj is IoBufferSerializable)
            {
                return (IoBufferSerializable)obj;
            }
            return GetValue(obj).Value;
        }

        public cTSOValue GetValue(object obj){
            var type = obj.GetType();
            if (IdByClass.ContainsKey(type))
            {
                uint clsid = IdByClass[type];
                return new cTSOValue() { Type = clsid, Value = obj };
            }

            throw new Exception("Unknown class " + type);
        }

        public DerivedStruct GetDerivedStruct(uint id)
        {
            return Format.DerivedStructs.FirstOrDefault(x => x.ID == id);
        }

        public DerivedStruct GetDerivedStruct(string name)
        {
            return Format.DerivedStructs.FirstOrDefault(x => x.Name == name);
        }

        /*public List<DataServiceWrapperPDU> SerializeDerivedUpdate(uint avatarId, string derivedTypeName, uint structId, object instance)
        {
            var type = GetDerivedStruct(derivedTypeName);

            var fields = SerializeDerived(derivedTypeName, structId, instance);
            var result = new List<DataServiceWrapperPDU>();

            foreach(var update in fields){
                result.Add(new DataServiceWrapperPDU() {
                    SendingAvatarID = avatarId,
                    RequestTypeID = 0x3998426C,
                    Body = update
                });
            }

            return result;
        }*/

        /*
        public List<cTSOTopicUpdateMessage> SerializeDerived(string derivedTypeName, uint structId, object instance)
        {
            return SerializeDerived(GetDerivedStruct(derivedTypeName).ID, structId, instance);
        }

        public List<cTSOTopicUpdateMessage> SerializeDerived(uint derivedType, uint structId, object instance){
            var result = new List<cTSOTopicUpdateMessage>();
            var type = Format.DerivedStructs.First(x => x.ID == derivedType);
            var parent = Format.Structs.First(x => x.ID == type.Parent);

            foreach(var field in parent.Fields){
                var mask = type.FieldMasks.FirstOrDefault(x => x.ID == field.ID);
                var action = DerivedStructFieldMaskType.KEEP;
                if (mask != null){
                    action = mask.Type;
                }

                if(action == DerivedStructFieldMaskType.REMOVE){
                    continue;
                }

                object value = GetFieldValue(instance, field.Name);
                if (value == null) { continue; }

                try {
                    var serialized = SerializeField(field, value);
                    serialized.StructType = parent.ID;
                    serialized.StructId = structId;
                    result.Add(serialized);
                }catch(Exception ex)
                {
                    LOG.Error(ex);
                }
            }
            
            return result;
        }*/

        private object GetFieldValue(object obj, string fieldName)
        {
            var objectField = obj.GetType().GetProperty(fieldName);
            if (objectField == null) { return null; }

            var value = objectField.GetValue(obj);

            return value;
        }

        /*private cTSOTopicUpdateMessage SerializeField(StructField field, object value){
            cTSOTopicUpdateMessage result = new cTSOTopicUpdateMessage();
            result.StructField = field.ID;

            if(field.Classification == StructFieldClassification.List)
            {
                IoBuffer resultBytes = AbstractVoltronPacket.Allocate(4);
                resultBytes.AutoExpand = true;
                var serializedValues = new List<cTSOValue>();
                System.Collections.ICollection list = (System.Collections.ICollection)value;
                
                foreach (var item in list){
                    serializedValues.Add(SerializeValue(field.TypeID, item));
                }

                var itemType = serializedValues.First().Type;
                var vectorType = GetVectorClsId(itemType);

                resultBytes.PutUInt32((uint)serializedValues.Count);

                foreach (var serializedValue in serializedValues){
                    //resultBytes.PutSerializable(serializedValue.Value);
                }

                resultBytes.Flip();

                /*result.cTSOValue = new cTSOValue {
                    Type = 0xA97384A3,//field.TypeID,
                    Value = resultBytes
                };

            }else if(field.Classification == StructFieldClassification.SingleField)
            {
                var serializedValue = SerializeValue(field.TypeID, value);
                //result.cTSOValue = serializedValue;
            }

            return result;
        }*/

        private uint GetVectorClsId(uint clsid)
        {
            switch (clsid)
            {
                //cTSOValue < unsigned long>
                case 0x696D1189:
                    //cTSOValueVector < unsigned long>
                    return 0x89738496;
                //cTSOValue < long >
                case 0x896D1196:
                    //cTSOValueVector < long >
                    return 0x8973849A;
                //cTSOValue < unsigned short>
                case 0xE9760891:
                    //cTSOValueVector < unsigned short>
                    return 0x097608B3;
                //cTSOValue < short >
                case 0xE9760897:
                    //cTSOValueVector < short >
                    return 0x097608B6;
                //cTSOValue<class cRZAutoRefCount<class cITSOProperty> >
                case 0xA96E7E5B:
                    //cTSOValueVector<class cRZAutoRefCount<class cITSOProperty> >
                    return 0xA97384A3;
                //cTSOValue <class cRZAutoRefCount<class cIGZString> >
                default:
                    throw new Exception("Cannot map clsid to vector clsid, " + clsid);
            }
        }

        private cTSOValue SerializeValue(uint type, object value)
        {
            var result = new cTSOValue();
            IoBuffer buffer = null;

            switch (type)
            {
                case 0x48BC841E:
                    if (!(value is sbyte) && !(value is Enum))
                    {
                        return null;
                    }
                    result.Type = cTSOValue_sint8;
                    result.Value = new sbyte[] { Convert.ToSByte(value) };
                    break;

                case 0x74336731:
                    if (!(value is ushort))
                    {
                        return null;
                    }

                    buffer = AbstractVoltronPacket.Allocate(2);
                    buffer.PutUInt16((ushort)value);
                    buffer.Flip();

                    result.Type = cTSOValue_uint16;
                    result.Value = buffer;
                    break;

                case 0xF192ECA6:
                    if (!(value is short))
                    {
                        return null;
                    }

                    buffer = AbstractVoltronPacket.Allocate(2);
                    buffer.PutInt16((short)value);
                    buffer.Flip();

                    result.Type = cTSOValue_sint16;
                    result.Value = buffer;
                    break;

                case 0xE0463A2F:
                    if (!(value is uint))
                    {
                        return null;
                    }

                    buffer = AbstractVoltronPacket.Allocate(4);
                    buffer.PutUInt32((uint)value);
                    buffer.Flip();

                    result.Type = cTSOValue_uint32;
                    result.Value = buffer;
                    break;

                case 0xA0587098:
                    if (!(value is int))
                    {
                        return null;
                    }

                    buffer = AbstractVoltronPacket.Allocate(4);
                    buffer.PutInt32((int)value);
                    buffer.Flip();

                    result.Type = cTSOValue_sint32;
                    result.Value = buffer;
                    break;

                case 0x385070C9:
                    if (!(value is ulong))
                    {
                        return null;
                    }

                    buffer = AbstractVoltronPacket.Allocate(8);
                    buffer.PutUInt64((ulong)value);
                    buffer.Flip();

                    result.Type = cTSOValue_uint64;
                    result.Value = buffer;
                    break;

                case 0x90D315F7:
                    if (!(value is long))
                    {
                        return null;
                    }

                    buffer = AbstractVoltronPacket.Allocate(8);
                    buffer.PutInt64((long)value);
                    buffer.Flip();

                    result.Type = cTSOValue_sint64;
                    result.Value = buffer;
                    break;

                default:
                    //It may be a struct
                    var _struct = Format.Structs.FirstOrDefault(x => x.ID == type);
                    if (_struct != null)
                    {
                        var body = new cITSOProperty();
                        body.StructType = _struct.ID;
                        body.StructFields = new List<cITSOField>();

                        foreach(var field in _struct.Fields){
                            object fieldValue = GetFieldValue(value, field.Name);
                            if (fieldValue == null) { continue; }

                            body.StructFields.Add(new cITSOField {
                                ID = field.ID,
                                Value = SerializeValue(field.TypeID, fieldValue)
                            });
                        }

                        result.Type = cTSOValue_property;
                        result.Value = body;
                        return result;
                    }

                    return null;

            }

            return result;
        }
    }
}
