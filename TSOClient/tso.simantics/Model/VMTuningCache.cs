using System.Collections.Generic;

namespace FSO.SimAntics.Model
{
    public class VMTuningCache
    {
        public Dictionary<int, float> MotiveOverfill = new Dictionary<int, float>();

        public void UpdateTuning(VM vm)
        {
            if (vm.TS1) return;
            var table = vm.Tuning?.GetTable("overfill", vm.TSOState.PropertyCategory);
            if (table != null) MotiveOverfill = table;
        }

        public short GetLimit(VMMotive motive)
        {
            float result;
            if (MotiveOverfill.TryGetValue((int)motive, out result)) return (short)result;
            return 100;
        }
    }
}
