using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace tso.content.framework
{
    public interface IContentCodec <T>
    {
        T Decode(Stream stream);
    }
}
