using FSO.Common.Content;
using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    public class AvatarCollectionsProvider : FAR3Provider<Collection>
    {
        public AvatarCollectionsProvider(Content contentManager)
            : base(contentManager, new CollectionCodec(), new Regex(".*/collections/.*\\.dat"))
        {
        }
    }
}
