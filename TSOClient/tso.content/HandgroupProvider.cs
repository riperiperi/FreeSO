/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Content.Framework;
using FSO.Content.Codecs;
using FSO.Vitaboy;

namespace FSO.Content
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
            : base(contentManager, "packingslips/handgroups.xml", new HandgroupCodec())
        {
        }
    }
}
