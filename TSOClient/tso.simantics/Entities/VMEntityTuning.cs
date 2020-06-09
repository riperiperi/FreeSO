using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Entities
{
    public class VMEntityTuning
    {
        private Dictionary<int, Dictionary<int, short>> Table;

        public VMEntityTuning(VMEntity owner, VM vm)
        {
            // load any upgrade changes
            // first search for upgrades for this file
            var iffname = owner.Object.Resource.MainIff.Filename;
            if (owner.PlatformState is VMTSOObjectState)
            {
                var upgrades = Content.Content.Get().Upgrades?.GetUpgrade(iffname, (owner.MasterDefinition ?? owner.Object.OBJ).GUID, ((VMTSOObjectState)owner.PlatformState).UpgradeLevel);
                if (upgrades != null)
                {
                    AppendTable(upgrades);
                }
            }

            var tuning = vm.Tuning;
            if (tuning != null)
            {
                // load any global changes
                AppendTable(tuning.GetTables(iffname), 4096);
                if (owner.SemiGlobal != null)
                {
                    AppendTable(tuning.GetTables(owner.SemiGlobal.MainIff.Filename), 8192);
                }
                // todo: global tuning replacement (?)
                // AppendTable(?, 256);
            }
        }

        public void AppendTable(Dictionary<int, Dictionary<int, float>> table, int tableOff)
        {
            if (table == null) return;
            if (Table == null) Table = new Dictionary<int, Dictionary<int, short>>();
            foreach (var bcon in table)
            {
                Dictionary<int, short> targBcon = null;
                if (!Table.TryGetValue(bcon.Key + tableOff, out targBcon))
                {
                    targBcon = new Dictionary<int, short>();
                    Table[bcon.Key + tableOff] = targBcon;
                }
                foreach (var item in bcon.Value)
                {
                    targBcon[item.Key] = (short)item.Value;
                }
            }
        }

        public void AppendTable(Dictionary<int, Dictionary<int, short>> table)
        {
            if (table == null) return;
            if (Table == null) Table = new Dictionary<int, Dictionary<int, short>>();
            foreach (var bcon in table)
            {
                Dictionary<int, short> targBcon = null;
                if (!Table.TryGetValue(bcon.Key, out targBcon))
                {
                    targBcon = new Dictionary<int, short>();
                    Table[bcon.Key] = targBcon;
                }
                foreach (var item in bcon.Value)
                {
                    targBcon[item.Key] = item.Value;
                }
            }
        }

        public short? TryGetEntry(int table, int index)
        {
            if (Table == null) return null;
            Dictionary<int, short> t;
            if (!Table.TryGetValue(table, out t)) return null;
            short result;
            if (!t.TryGetValue(index, out result)) return null;
            return result;
        }
    }
}
