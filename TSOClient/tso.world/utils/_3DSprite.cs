/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework;
using FSO.Vitaboy;

namespace FSO.LotView.Utils
{
    public class _3DSprite {
        public _3DSpriteEffect Effect;
        public Matrix World;
        public Avatar Geometry;
        public short ObjectID;
    }

    public enum _3DSpriteEffect {
        CHARACTER
    }
}
