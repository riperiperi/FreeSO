using System;
using Mina.Core.Buffer;
using System.ComponentModel;
using FSO.Common.Serialization.TypeSerializers;

namespace FSO.Common.Serialization.Primitives
{
    [cTSOValue(0x125194E5)]
    public class cTSONetMessageStandard : IoBufferSerializable, IoBufferDeserializable
    {
        public uint Unknown_1 { get; set; }
        public uint SendingAvatarID { get; set; }
        public cTSOParameterizedEntityFlags Flags { get; set; }
        public uint MessageID { get; set; }

        public uint? DatabaseType { get; set; }
        public uint? DataServiceType { get; set; }

        public uint? Parameter { get; set; }
        public uint RequestResponseID { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object ComplexParameter { get; set; }

        public uint Unknown_2 { get; set; }

        public cTSONetMessageStandard(){
        }

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.Unknown_1 = input.GetUInt32();
            this.SendingAvatarID = input.GetUInt32();
            var flagsByte = input.Get();
            this.Flags = (cTSOParameterizedEntityFlags)flagsByte;
            this.MessageID = input.GetUInt32();

            if ((this.Flags & cTSOParameterizedEntityFlags.HAS_DS_TYPE) == cTSOParameterizedEntityFlags.HAS_DS_TYPE)
            {
                this.DataServiceType = input.GetUInt32();
            }else if ((this.Flags & cTSOParameterizedEntityFlags.HAS_DB_TYPE) == cTSOParameterizedEntityFlags.HAS_DB_TYPE){
                this.DatabaseType = input.GetUInt32();
            }

            if ((this.Flags & cTSOParameterizedEntityFlags.HAS_BASIC_PARAMETER) == cTSOParameterizedEntityFlags.HAS_BASIC_PARAMETER)
            {
                this.Parameter = input.GetUInt32();
            }

            if ((this.Flags & cTSOParameterizedEntityFlags.UNKNOWN) == cTSOParameterizedEntityFlags.UNKNOWN)
            {
                this.Unknown_2 = input.GetUInt32();
            }

            if ((this.Flags & cTSOParameterizedEntityFlags.HAS_COMPLEX_PARAMETER) == cTSOParameterizedEntityFlags.HAS_COMPLEX_PARAMETER)
            {
                uint typeId = DatabaseType.HasValue ? DatabaseType.Value : DataServiceType.Value;
                this.ComplexParameter = context.ModelSerializer.Deserialize(typeId, input, context);
            }
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(Unknown_1);
            output.PutUInt32(SendingAvatarID);

            byte flags = 0;
            if (this.DatabaseType.HasValue){
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_DB_TYPE;
            }

            if (this.DataServiceType.HasValue){
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_DB_TYPE;
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_DS_TYPE;
            }

            if (this.Parameter != null){
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_BASIC_PARAMETER;
            }

            if(this.ComplexParameter != null){
                flags |= (byte)cTSOParameterizedEntityFlags.HAS_COMPLEX_PARAMETER;
            }

            output.Put(flags);
            output.PutUInt32(MessageID);

            if (this.DataServiceType.HasValue)
            {
                output.PutUInt32(this.DataServiceType.Value);
            }else if (this.DatabaseType.HasValue){
                output.PutUInt32(this.DatabaseType.Value);
            }

            if (this.Parameter.HasValue){
                output.PutUInt32(this.Parameter.Value);
            }

            if (this.ComplexParameter != null){
                context.ModelSerializer.Serialize(output, ComplexParameter, context, false);
            }
        }
    }

    [Flags]
    public enum cTSOParameterizedEntityFlags
    {
        HAS_DB_TYPE = 1,
        HAS_DS_TYPE = 2,
        HAS_BASIC_PARAMETER = 4,
        UNKNOWN = 8,
        HAS_COMPLEX_PARAMETER = 32
    }
}
