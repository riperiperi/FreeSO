using FSO.Content.Framework;
using FSO.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.Content.Codecs
{
    public class IniCodec : IContentCodec<IniFile>
    {
        public override object GenDecode(Stream stream)
        {
            var result = new IniFile();
            result.Decode(stream);
            return result;
        }
    }
}
