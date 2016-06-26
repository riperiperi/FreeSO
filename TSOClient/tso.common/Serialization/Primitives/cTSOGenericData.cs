using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Serialization.Primitives
{
    public class cTSOGenericData
    {
        public byte[] Data;

        public cTSOGenericData() { }
        public cTSOGenericData(byte[] data)
        {
            Data = data;
        }
    }
}
