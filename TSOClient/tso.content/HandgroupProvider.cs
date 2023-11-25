using FSO.Content.Framework;
using FSO.Content.Codecs;
using FSO.Vitaboy;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to handgroup (*.hag) data in FAR3 archives.
    /// </summary>
    public class HandgroupProvider : FAR3Provider<HandGroup>
    {
        public HandgroupProvider(Content contentManager)
            : base(contentManager, new HandgroupCodec(), new Regex(".*/hands/groups/.*\\.dat"))
        {
        }
    }
}
