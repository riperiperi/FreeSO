using FSO.Content.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    public class CollectionCodec : IContentCodec<Collection>
    {
        public override object GenDecode(Stream stream)
        {
            Collection collection = new Collection();
            collection.Read(stream);
            return collection;
        }
    }
}
