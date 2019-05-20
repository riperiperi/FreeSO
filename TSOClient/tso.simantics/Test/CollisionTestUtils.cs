using FSO.SimAntics.Model.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Test
{
    public class CollisionTestUtils
    {
        public void VerifyAllCollision(VM vm)
        {
            // verifies that static and dynamic obstacles are in a valid state
            var context = vm.Context;
            var allRooms = context.RoomInfo;
            foreach (var room in allRooms)
            {
                var obs = room.StaticObstacles.All();
                var ents = new HashSet<VMEntity>(room.Entities);
                foreach (var node in obs)
                {
                    var rect = node.Rect as VMEntityObstacle;
                    if (rect != null)
                    {
                        var ent = rect.Parent;
                        ents.Remove(rect.Parent);

                        var footprint = ent.Footprint;
                        if (footprint != null)
                        {
                            if (rect.x1 != footprint.x1 || rect.x2 != footprint.x2 || rect.y1 != footprint.y1 || rect.y2 != footprint.y2)
                            {
                                throw new Exception("Static footprint mismatch with .Footprint on object!");
                            }
                        }
                        
                        if (rect != ent.Footprint) throw new Exception("Out of date footprint in static!");
                        if (!ent.StaticFootprint) throw new Exception("Object with dynamic footprint still present in static!");
                    }
                }
                foreach (var ent in ents)
                {
                    if (ent.StaticFootprint && ent.Footprint != null) throw new Exception("Object with static footprint missing from static list.");
                }
            }
        }
    }
}
