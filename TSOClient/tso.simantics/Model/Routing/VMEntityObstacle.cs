using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.Model.Routing
{
    public class VMEntityObstacle : VMObstacle
    {
        public VMEntity Parent;
        public VMObstacleSet Set;
        public List<VMObstacle> Dynamic;

        public VMEntityObstacle() { }

        public VMEntityObstacle(Point source, Point dest) : base(source, dest)
        {
        }

        public VMEntityObstacle(int x1, int y1, int x2, int y2, VMEntity ent) : base(x1, y1, x2, y2)
        {
            Parent = ent;
        }

        public void Unregister()
        {
            if (Parent.StaticFootprint) Set?.Delete(this);
            else Dynamic?.Remove(this);
            Set = null;
            Dynamic = null;
        }
    }
}
