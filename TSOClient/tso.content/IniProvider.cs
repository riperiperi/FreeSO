using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Files;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    public class IniProvider : FileProvider<IniFile>
    {
        public IniProvider(Content content) : base(content, new IniCodec(), new Regex("^sys/.*\\.ini"))
        {
        }
    }
}
