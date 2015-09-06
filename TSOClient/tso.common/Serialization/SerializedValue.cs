using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Serialization
{
    public class SerializedValue
    {
        public uint ClsId { get; internal set; }
        public byte[] Data { get; internal set; }

        public SerializedValue(uint clsid, byte[] data)
        {
            this.ClsId = clsid;
            this.Data = data;
        }
    }
}
