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
using FSO.LotView.Model;

namespace FSO.LotView
{
    /// <summary>
    /// Handles rendering the 3D world.
    /// </summary>
    public class World3D
    {
        private Blueprint Blueprint;

        public void Init(Blueprint blueprint){
            this.Blueprint = blueprint;
        }

        public void PreDraw(GraphicsDevice gd, WorldState state)
        {

        }

        public void DrawBefore2D(GraphicsDevice gd, WorldState state){

        }

        public void DrawAfter2D(GraphicsDevice gd, WorldState state){
            var pxOffset = state.WorldSpace.GetScreenOffset();
            var _2d = state._2D;
            foreach (var avatar in Blueprint.Avatars)
            {
                if ((avatar.Position.Z + 0.05f) / 2.95f < state.Level)
                {
                    _2d.OffsetPixel(state.WorldSpace.GetScreenFromTile(avatar.Position));
                    _2d.OffsetTile(avatar.Position);
                    avatar.Draw(gd, state);
                }
            }
        }
    }
}
