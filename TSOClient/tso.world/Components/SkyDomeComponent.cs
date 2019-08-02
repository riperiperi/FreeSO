using FSO.Common;
using FSO.Common.Utils;
using FSO.Files;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components
{
    public class SkyDomeComponent : AbstractSkyDome
    {
        public Blueprint BP;

        public SkyDomeComponent(GraphicsDevice GD, Blueprint bp) : base(GD, (float)bp.OutsideTime)
        {
            BP = bp;
        }


        public void Draw(GraphicsDevice gd, WorldState state)
        {
            Draw(gd, state.OutsideColor,
                state.View,
                state.Projection, //((state.Camera as WorldCamera3D)?.BaseProjection() ?? state.Camera.Projection), 
                (float)BP.OutsideTime, 
                BP.Weather, 
                state.Light?.SunVector ?? 
                new Vector3(0, 1, 0),
                1f+((state.Camera as WorldCamera3D)?.FromIntensity ?? 0f) * 76);
        }
    }
}
