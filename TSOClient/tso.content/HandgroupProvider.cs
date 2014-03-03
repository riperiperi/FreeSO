using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content.framework;
using TSO.Content.codecs;
using TSO.Vitaboy;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to handgroup (*.hag) data in FAR3 archives.
    /// </summary>
    public class HandgroupProvider : PackingslipProvider<HandGroup>
    {
        /// <summary>
        /// Creates a new instance of HandgroupProvider.
        /// </summary>
        /// <param name="contentManager">A Content instance.</param>
        /// <param name="device">A GraphicsDevice instance.</param>
        public HandgroupProvider(Content contentManager, GraphicsDevice device)
            : base(contentManager, "packingslips\\handgroups.xml", new HandgroupCodec())
        {
        }
    }
}
