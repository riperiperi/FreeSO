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
using tso.world.model;

namespace tso.world
{
    public class World3D
    {
        private Blueprint Blueprint;

        public void Init(Blueprint blueprint){
            this.Blueprint = blueprint;
        }

        public void PreDraw(GraphicsDevice gd, WorldState state){

        }

        public void DrawBefore2D(GraphicsDevice gd, WorldState state){
        }

        public void DrawAfter2D(GraphicsDevice gd, WorldState state){
            //gd.RasterizerState.CullMode = CullMode.CullCounterClockwiseFace;
            foreach (var avatar in Blueprint.Avatars){
                avatar.Draw(gd, state);
            }
        }
    }
}
