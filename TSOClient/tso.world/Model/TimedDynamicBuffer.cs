using FSO.Common;
using FSO.LotView.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Model
{
    public class TimedDynamicStaticLayers
    {
        public HashSet<ObjectComponent>[] Schedule;

        public HashSet<ObjectComponent> StaticObjects = new HashSet<ObjectComponent>();
        public HashSet<ObjectComponent> DynamicObjects = new HashSet<ObjectComponent>();

        private int Timer = 0;
        private int CurrentRing = 0;
        private int HalfSeconds;
        private bool Dirty;

        public TimedDynamicStaticLayers(int halfSeconds)
        {
            HalfSeconds = halfSeconds;
            Schedule = new HashSet<ObjectComponent>[halfSeconds];
            for (int i = 0; i < halfSeconds; i++)
            {
                Schedule[i] = new HashSet<ObjectComponent>();
            }
        }

        public bool Update()
        {
            if (++Timer >= FSOEnvironment.RefreshRate / 2)
            {
                CurrentRing = (CurrentRing + 1 % HalfSeconds);
                var toClear = Schedule[CurrentRing];
                var newEntry = new List<ObjectComponent>();
                foreach (var obj in toClear)
                {
                    //move objects in the dynamic layer back to static
                    if (!obj.ForceDynamic)
                    {
                        MoveToStatic(obj);
                    }
                    else
                    {
                        //stay in dynamic, and reschedule the check.
                        newEntry.Add(obj);
                    }
                }
                toClear.Clear();
                foreach (var item in newEntry) toClear.Add(item);
            }
            var dirty = Dirty;
            Dirty = false;
            return dirty;
        }

        public void RegisterObject(ObjectComponent obj)
        {
            StaticObjects.Add(obj);
        }

        public void UnregisterObject(ObjectComponent obj)
        {
            StaticObjects.Remove(obj);
            DynamicObjects.Remove(obj);
            var ring = obj.RenderInfo.DynamicRemoveCycle;
            Schedule[ring].Remove(obj);
        }

        public void MoveToStatic(ObjectComponent obj)
        {
            if (obj.RenderInfo.Layer == WorldObjectRenderLayer.STATIC) return;
            obj.RenderInfo.Layer = WorldObjectRenderLayer.STATIC;
            DynamicObjects.Remove(obj);
            StaticObjects.Add(obj);
        }

        public void EnsureDynamic(ObjectComponent obj)
        {
            if (obj.RenderInfo.Layer == WorldObjectRenderLayer.DYNAMIC)
            {
                var ring = obj.RenderInfo.DynamicRemoveCycle;
                if (ring != CurrentRing)
                {
                    Schedule[ring].Remove(obj);
                    Schedule[CurrentRing].Add(obj);
                    obj.RenderInfo.DynamicRemoveCycle = CurrentRing; //reset timer for moving this object back to static
                }
                return;
            }
            obj.RenderInfo.Layer = WorldObjectRenderLayer.DYNAMIC;
            StaticObjects.Remove(obj);
            DynamicObjects.Add(obj);
            Dirty = true;

            Schedule[CurrentRing].Add(obj);
            obj.RenderInfo.DynamicRemoveCycle = CurrentRing;
        }
    }
}
