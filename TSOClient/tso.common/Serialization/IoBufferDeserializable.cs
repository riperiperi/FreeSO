using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Serialization
{
    public interface IoBufferDeserializable
    {
        void Deserialize(IoBuffer input, ISerializationContext context);
    }
}
