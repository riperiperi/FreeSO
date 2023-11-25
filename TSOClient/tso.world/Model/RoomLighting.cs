using FSO.LotView.LMap;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FSO.LotView.Model
{
    public class RoomLighting
    {
        public List<LightData> Lights = new List<LightData>();
        public List<Rectangle> ObjectFootprints = new List<Rectangle>();
        public List<Components.ObjectComponent> Components = new List<Components.ObjectComponent>();

        public ushort OutsideLight;
        public ushort AmbientLight;
        public short RoomScore;
        public Rectangle Bounds;
    }
}
