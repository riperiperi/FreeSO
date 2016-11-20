using FSO.Common.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

namespace FSO.SimAntics.Engine.TSOGlobalLink.Model
{
    public class VMGLOutfit : IoBufferSerializable, IoBufferDeserializable
    {
        public uint outfit_id { get; set; }
        public ulong asset_id { get; set; }
        public int sale_price { get; set; }
        public int purchase_price { get; set; }

        public VMGLOutfitOwner owner_type { get; set; }
        public uint owner_id { get; set; }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(outfit_id);
            output.PutUInt64(asset_id);
            output.PutInt32(sale_price);
            output.PutInt32(purchase_price);
            output.PutEnum(owner_type);
            output.PutUInt32(owner_id);
        }

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            outfit_id = input.GetUInt32();
            asset_id = input.GetUInt64();
            sale_price = input.GetInt32();
            purchase_price = input.GetInt32();
            owner_type = input.GetEnum<VMGLOutfitOwner>();
            owner_id = input.GetUInt32();
        }
    }


    public enum VMGLOutfitOwner : byte
    {
        AVATAR = 1,
        OBJECT = 2
    }
}
