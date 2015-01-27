/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace tso.world.utils
{
    /// <summary>
    /// Represents a 2D sprite in the game.
    /// </summary>
    public class _2DSprite
    {
        public _2DBatchRenderMode RenderMode;
        public Texture2D Pixel;
        public Texture2D Depth;
        public Texture2D Mask;
        public Vector3 TilePosition;
        public Vector3 WorldPosition;
        public short ObjectID; //used for mouse hit test render mode

        public Rectangle SrcRect;
        public Rectangle DestRect;

        //For internal use, do not set this
        public int DrawOrder;
        public bool FlipHorizontally;
        public bool FlipVertically;

        public Rectangle AbsoluteDestRect;
        public Vector3 AbsoluteWorldPosition;
        public Vector3 AbsoluteTilePosition;

    }
}
