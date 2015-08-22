using FSO.Files.Formats.tsodata;
using FSO.Server.Protocol.Utils;
using Mina.Core.Buffer;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron.DataService
{
    /// <summary>
    /// TODO: Rewrite this to have much tighter performance
    /// </summary>
    public class cTSOSerializer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

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

        private TSODataDefinition Format;

        public cTSOSerializer(TSODataDefinition data){
            this.Format = data;
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

                var objectField = instance.GetType().GetProperty(field.Name);
                if (objectField == null) { continue; }

                var value = objectField.GetValue(instance);
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
        }

        private cTSOTopicUpdateMessage SerializeField(StructField field, object value){
            cTSOTopicUpdateMessage result = new cTSOTopicUpdateMessage();
            result.StructField = field.ID;

            IoBuffer buffer = null;

            switch (field.TypeID){
                case 0x13FF06C5:
                    if(!(value is bool)){
                        throw new Exception("Expected boolean");
                    }

                    result.cTSOValueType = cTSOValue_bool;
                    result.cTSOValue = new byte[] { ((bool)value == true ? (byte)0x01 : (byte)0x00) };
                    break;

                case 0xE0FF5CB4:
                    if(!(value is string)){
                        throw new Exception("Expected string");
                    }

                    result.cTSOValueType = cTSOValue_string;
                    result.cTSOValue = IoBufferUtils.GetPascalVLCString((string)value);
                    break;

                case 0x5BB0333A:
                    if(!(value is byte)){
                        throw new Exception("Expected byte");
                    }
                    result.cTSOValueType = cTSOValue_uint8;
                    result.cTSOValue = new byte[] { (byte)value };
                    break;

                case 0x48BC841E:
                    if(!(value is sbyte)){
                        throw new Exception("Expected sbyte");
                    }
                    result.cTSOValueType = cTSOValue_sint8;
                    result.cTSOValue = new byte[] { (byte)value };
                    break;

                case 0x74336731:
                    if (!(value is ushort)){
                        throw new Exception("Expected ushort");
                    }

                    buffer = AbstractVoltronPacket.Allocate(2);
                    buffer.PutUInt16((ushort)value);
                    buffer.Flip();

                    result.cTSOValueType = cTSOValue_uint16;
                    result.cTSOValue = buffer;
                    break;

                case 0xF192ECA6:
                    if (!(value is short))
                    {
                        throw new Exception("Expected short");
                    }

                    buffer = AbstractVoltronPacket.Allocate(2);
                    buffer.PutInt16((short)value);
                    buffer.Flip();

                    result.cTSOValueType = cTSOValue_sint16;
                    result.cTSOValue = buffer;
                    break;

                case 0xE0463A2F:
                    if (!(value is uint))
                    {
                        throw new Exception("Expected uint");
                    }

                    buffer = AbstractVoltronPacket.Allocate(4);
                    buffer.PutUInt32((uint)value);
                    buffer.Flip();

                    result.cTSOValueType = cTSOValue_uint32;
                    result.cTSOValue = buffer;
                    break;

                case 0xA0587098:
                    if (!(value is int))
                    {
                        throw new Exception("Expected int");
                    }

                    buffer = AbstractVoltronPacket.Allocate(4);
                    buffer.PutInt32((int)value);
                    buffer.Flip();

                    result.cTSOValueType = cTSOValue_sint32;
                    result.cTSOValue = buffer;
                    break;

                case 0x385070C9:
                    if (!(value is ulong))
                    {
                        throw new Exception("Expected ulong");
                    }

                    buffer = AbstractVoltronPacket.Allocate(8);
                    buffer.PutUInt64((uint)value);
                    buffer.Flip();

                    result.cTSOValueType = cTSOValue_uint64;
                    result.cTSOValue = buffer;
                    break;

                case 0x90D315F7:
                    if (!(value is long))
                    {
                        throw new Exception("Expected long");
                    }

                    buffer = AbstractVoltronPacket.Allocate(8);
                    buffer.PutInt64((int)value);
                    buffer.Flip();

                    result.cTSOValueType = cTSOValue_sint64;
                    result.cTSOValue = buffer;
                    break;

                default:
                    throw new Exception("Unknown type:" + field.TypeID);
            }

            /**
        public const uint cTSOValue_bool = 0x696D1183;
        public const uint cTSOValue_uint8 = 0xC976087C;
        public const uint cTSOValue_uint16 = 0xE9760891;
        public const uint cTSOValue_uint32 = 0x696D1189;
        public const uint cTSOValue_uint64 = 0x69D3E3DB;
        public const uint cTSOValue_sint8 = 0xC976087C;
        public const uint cTSOValue_sint16 = 0xE9760897;
        public const uint cTSOValue_sint32 = 0x896D1196;
        public const uint cTSOValue_sint64 = 0x89D3E3EF;
        public const uint cTSOValue_string = 0x896D1688;**/

            return result;
            /*if (value is bool){
                buffer.PutUInt32(cTSOValue_bool);
                buffer.Put((bool)value == true ? (byte)0x01 : (byte)0x00);
            }*/
        }
    }
}
