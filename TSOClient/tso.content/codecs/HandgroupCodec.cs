using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Content.framework;
using TSO.Vitaboy;

namespace TSO.Content.codecs
{
    public class HandgroupCodec : IContentCodec<HandGroup>
    {
        #region IContentCodec<Binding> Members

        public HandGroup Decode(Stream stream)
        {
            HandGroup Hag = new HandGroup();
            Hag.Read(stream);
            return Hag;
        }

        #endregion
    }
}
