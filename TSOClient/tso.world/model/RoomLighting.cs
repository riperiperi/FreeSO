using FSO.LotView.LMap;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Model
{
    public class RoomLighting
    {
        //TODO: point lights

        public List<LightData> Lights = new List<LightData>();
        public List<Rectangle> ObjectFootprints = new List<Rectangle>();

        public ushort OutsideLight;
        public ushort AmbientLight;
        public short RoomScore;
        public Rectangle Bounds;
    }
}
