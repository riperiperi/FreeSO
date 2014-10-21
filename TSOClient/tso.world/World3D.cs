/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
