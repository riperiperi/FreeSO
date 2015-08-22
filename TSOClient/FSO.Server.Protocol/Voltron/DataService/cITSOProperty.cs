using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Voltron.DataService
{
    public class cITSOProperty : IoBufferSerializable
    {
        public uint StructType;
        public List<cITSOField> StructFields;
        
        /**cTSOValue<class cRZAutoRefCount<class cITSOProperty> > body:
* dword Body clsid (iid=896E3E90 or "GZIID_cITSOProperty"; clsid should be 0x89739A79 for cTSOProperty)
* dword Body
  * dword Struct type (e.g. 0x3B0430BF for AvatarAppearance)
  * dword Field count
  * Fields - for each field:
    * dword Field name (e.g. 0x1D530275 for AvatarAppearance_BodyOutfitID)
    * dword cTSOValue clsid
    * cTSOValue body**/

        public IoBuffer Serialize()
        {
            var result = AbstractVoltronPacket.Allocate(12);
            result.AutoExpand = true;
            result.PutUInt32(0x89739A79);
            result.PutUInt32(StructType);
            result.PutUInt32((uint)StructFields.Count);

            foreach(var item in StructFields){
                result.PutUInt32(item.ID);
                result.PutUInt32(item.Value.Type);
                result.PutSerializable(item.Value.Value);
            }

            return result;
        }
    }

    public class cITSOField
    {
        public uint ID;
        public cTSOValue Value;
    }
}
