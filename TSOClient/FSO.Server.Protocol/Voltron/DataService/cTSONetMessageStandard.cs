using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using System.ComponentModel;

namespace FSO.Server.Protocol.Voltron.DataService
{
    public class cTSONetMessageStandard : IoBufferSerializable, IoBufferDeserializable
    {
        public uint Unknown_1 { get; set; }
        public uint SendingAvatarID { get; set; }
        public cTSOParameterizedEntityFlags Flags { get; set; }
        public uint MessageID { get; set; }

        public uint? RequestParameter { get; set; }
        public uint? RequestResponseType { get; set; }
        
        public uint ResponsePayloadType { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object ResponsePayload { get; set; }
        
        public cTSONetMessageStandard(){
        }

        public void Deserialize(IoBuffer input)
        {
            this.Unknown_1 = input.GetUInt32();
            this.SendingAvatarID = input.GetUInt32();
            this.Flags = (cTSOParameterizedEntityFlags)input.Get();
            this.MessageID = input.GetUInt32();

            if ((this.Flags & cTSOParameterizedEntityFlags.HAS_RESPONSE_TYPE) == cTSOParameterizedEntityFlags.HAS_RESPONSE_TYPE){
                this.RequestResponseType = input.GetUInt32();
            }

            if ((this.Flags & cTSOParameterizedEntityFlags.HAS_REQUEST_PARAMETER) == cTSOParameterizedEntityFlags.HAS_REQUEST_PARAMETER)
            {
                this.RequestParameter = input.GetUInt32();
            }

            if ((this.Flags & cTSOParameterizedEntityFlags.HAS_RESPONSE_PAYLOAD) == cTSOParameterizedEntityFlags.HAS_RESPONSE_PAYLOAD)
            {
                //TODO: 
            }
        }

        public IoBuffer Serialize()
        {
            var result = AbstractVoltronPacket.Allocate(13);
            result.AutoExpand = true;

            result.PutUInt32(Unknown_1);
            result.PutUInt32(SendingAvatarID);

            byte flags = 0;
            if (this.RequestResponseType.HasValue){
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_RESPONSE_TYPE;
            }

            if(this.RequestParameter != null){
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_REQUEST_PARAMETER;
            }

            if(this.ResponsePayload != null){
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_RESPONSE_PAYLOAD;
            }

            result.Put(flags);
            result.PutUInt32(MessageID);

            if (this.RequestResponseType.HasValue){
                result.PutUInt32(this.RequestResponseType.Value);
            }

            if (this.RequestParameter.HasValue){
                result.PutUInt32(this.RequestParameter.Value);
            }

            if (this.ResponsePayload != null)
            {
                result.PutUInt32(this.ResponsePayloadType);
                result.PutSerializable(this.ResponsePayload);
            }

            return result;
        }
    }

    [Flags]
    public enum cTSOParameterizedEntityFlags
    {
        HAS_RESPONSE_TYPE = 2,
        HAS_REQUEST_PARAMETER = 4,
        HAS_RESPONSE_PAYLOAD = 8,
        HAS_DOT_PATH = 16
    }
}
