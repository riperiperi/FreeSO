using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TSO.Content.framework
{
    public interface IContentCodec <T>
    {
        T Decode(Stream stream);
    }
}
