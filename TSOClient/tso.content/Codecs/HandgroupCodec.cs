using System.IO;
using FSO.Content.Framework;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    public class HandgroupCodec : IContentCodec<HandGroup>
    {
        #region IContentCodec<Binding> Members

        public override object GenDecode(Stream stream)
        {
            HandGroup Hag = new HandGroup();
            Hag.Read(stream);
            return Hag;
        }

        #endregion
    }
}
