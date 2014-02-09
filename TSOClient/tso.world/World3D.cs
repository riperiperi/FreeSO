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
            //Blueprint.Terrain.Draw(gd, state);
        }

        public void DrawAfter2D(GraphicsDevice gd, WorldState state){
            gd.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            foreach (var avatar in Blueprint.Avatars){
                avatar.Draw(gd, state);
            }
        }
    }
}
