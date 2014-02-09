using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;

namespace TSOClient.Code.Rendering.Lot.Components
{
    public class ObjectComponent : House2DComponent
    {
        public override int Height
        {
            get { return 0; }
        }

        public override void Draw(HouseRenderState state, HouseBatch batch)
        {

        }
    }
}
