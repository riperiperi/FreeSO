using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Engine
{
    public class VMScheduler
    {
        private VM vm;
        private Dictionary<uint, List<VMEntity>> TickSchedule = new Dictionary<uint, List<VMEntity>>();
        private List<VMEntity> TickThisFrame;
        public HashSet<VMEntity> PendingDeletion = new HashSet<VMEntity>();
        public uint CurrentTickID;
        public short CurrentObjectID;
        public bool RunningNow;

        public VMScheduler(VM vm)
        {
            this.vm = vm;
        }

        public void ScheduleTickIn(VMEntity ent, uint delay)
        {
            if (delay > 1 && ent.RunEveryFrame()) delay = 1;
            ScheduleTick(ent, CurrentTickID + delay);
        }

        public void ScheduleTick(VMEntity ent, uint tick)
        {
            List<VMEntity> targEnts;
            if (!TickSchedule.TryGetValue(tick, out targEnts))
            {
                targEnts = new List<VMEntity>();
                TickSchedule[tick] = targEnts;
            }
            ent.Thread.ScheduleIdleEnd = tick;
            VM.AddToObjList(targEnts, ent);
        }

        public void ScheduleCurrentTick(VMEntity ent)
        {
            ent.Thread.ScheduleIdleEnd = CurrentTickID;
            VM.AddToObjList(TickThisFrame, ent);
        }

        public void DescheduleTick(VMEntity ent)
        {
            //on delete or interrupt.
            if (ent.Thread != null && ent.Thread.ScheduleIdleEnd > CurrentTickID)
            {
                TickSchedule[ent.Thread.ScheduleIdleEnd].Remove(ent);
            }
        }

        public void RescheduleInterrupt(VMEntity ent)
        {
            //on interrupt.
            if (ent.Thread == null || ent.Thread.ScheduleIdleEnd == CurrentTickID || ent.Thread.ScheduleIdleEnd == 0) return;
            DescheduleTick(ent);
            if (ent.ObjectID > CurrentObjectID && TickThisFrame != null) ScheduleCurrentTick(ent);
            else ScheduleTickIn(ent, 1);
        }

        public void BeginTick(uint tickID)
        {
            if (CurrentTickID == 0)
            {
                //if we were on tick 0 it's likely we just resynced. Migrate ticks to the the correct tick id.
                List<VMEntity> firstTick;
                if (TickSchedule.TryGetValue(1, out firstTick))
                {
                    TickSchedule[tickID] = firstTick; //new objects are queued on next tick.
                    if (tickID != 1) TickSchedule.Remove(1);
                }
            }
            CurrentTickID = tickID;
        }

        public void RunTick()
        {
            RunningNow = true;
            if (TickSchedule.TryGetValue(CurrentTickID, out TickThisFrame))
            {
                //Console.WriteLine(TickThisFrame.Count + " entities ticked out of " + Entities.Count);
                for (int i = 0; i < TickThisFrame.Count; i++)
                {
                    var ent = TickThisFrame[i];
                    CurrentObjectID = ent.ObjectID;
                    ent.Tick();
                }
                TickSchedule.Remove(CurrentTickID);
            }

            vm.Context.RandomSeed += (ulong)vm.Entities.Count; // some "entropy" based on the number of entities present. forces a more strict sync
            CurrentObjectID = short.MaxValue;
            RunningNow = false;

            //delete all objets that were pending deletion
            foreach (var obj in PendingDeletion)
            {
                obj.Delete(false, vm.Context);
            }
            PendingDeletion.Clear();
        }

        public void Delete(VMEntity obj)
        {
            PendingDeletion.Add(obj);
        }

        public void Reset()
        {
            TickSchedule.Clear();
            CurrentTickID = 0;
            CurrentObjectID = short.MaxValue;
        }
    }
}
