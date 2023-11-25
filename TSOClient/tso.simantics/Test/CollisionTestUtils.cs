using FSO.SimAntics.Model.Routing;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.Test
{
    public class CollisionTestUtils
    {
        public string EntityInfo(VMEntity ent)
        {
            return $"{ent.ToString()} at {ent.Position.ToString()}. Contained in { ent.Container?.ToString() ?? "null" }, " +
                $"Dead: { ent.Dead.ToString() }, Flags: { ent.GetValue(Model.VMStackObjectVariable.Flags).ToString("X4") }.";
        }

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
                                throw new Exception($"Static footprint mismatch with .Footprint on object! \n{EntityInfo(ent)}");
                            }
                        }
                        
                        if (rect != ent.Footprint) throw new Exception($"Out of date footprint in static! \n{EntityInfo(ent)}");
                        if (!ent.StaticFootprint) throw new Exception($"Object with dynamic footprint still present in static! \n{EntityInfo(ent)}");
                        if (ent.Dead) throw new Exception($"Dead entity in static! \n{EntityInfo(ent)}");
                    }
                }
                foreach (var ent in ents)
                {
                    if (ent.StaticFootprint && ent.Footprint != null) throw new Exception($"Object with static footprint missing from static list. \n{EntityInfo(ent)}");
                }

                ents = new HashSet<VMEntity>(room.Entities);
                var dyn = room.DynamicObstacles;
                foreach (var entry in dyn)
                {
                    var rect = entry as VMEntityObstacle;
                    if (rect != null)
                    {
                        var ent = rect.Parent;
                        ents.Remove(rect.Parent);

                        var footprint = ent.Footprint;
                        if (footprint != null)
                        {
                            if (rect.x1 != footprint.x1 || rect.x2 != footprint.x2 || rect.y1 != footprint.y1 || rect.y2 != footprint.y2)
                            {
                                throw new Exception($"Dynamic footprint mismatch with .Footprint on object! \n{EntityInfo(ent)}");
                            }
                        }

                        if (rect != ent.Footprint) throw new Exception($"Out of date footprint in dynamic! \n{EntityInfo(ent)}");
                        if (ent.StaticFootprint) throw new Exception($"Object with static footprint still present in dynamic! \n{EntityInfo(ent)}");
                        if (ent.Dead) throw new Exception($"Dead entity in dynamic! \n{EntityInfo(ent)}");
                    }
                }

                foreach (var ent in ents)
                {
                    if (!ent.StaticFootprint && ent.Footprint != null) throw new Exception($"Object with dynamic footprint missing from dynamic list. \n{EntityInfo(ent)}");
                }
            }
        }
    }
}
