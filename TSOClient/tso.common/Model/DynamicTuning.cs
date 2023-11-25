using System.Collections.Generic;
using System.IO;

namespace FSO.Common.Model
{
    public class DynamicTuning
    {
        //global tuning:
        // city -
        //  0: terrain
        //   0: forceSnow (0/1/null)
        public static DynamicTuning Global;

        //string type/iff, int table, int index.
        public Dictionary<string, Dictionary<int, Dictionary<int, float>>> Tuning = new Dictionary<string, Dictionary<int, Dictionary<int, float>>>();
        public const int CURRENT_VERSION = 0;
        public int Version = CURRENT_VERSION;

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(Tuning.Count);
            foreach (var type in Tuning)
            {
                writer.Write(type.Key);
                writer.Write(type.Value.Count);
                foreach (var table in type.Value)
                {
                    writer.Write(table.Key);
                    writer.Write(table.Value.Count);
                    foreach (var value in table.Value)
                    {
                        writer.Write(value.Key);
                        writer.Write(value.Value);
                    }
                }
            }
        }

        public DynamicTuning(IEnumerable<DynTuningEntry> entries)
        {
            foreach (var entry in entries)
            {
                AddTuning(entry);
            }
        }

        public void AddTuning(DynTuningEntry entry)
        {
            Dictionary<int, Dictionary<int, float>> tables;
            if (!Tuning.TryGetValue(entry.tuning_type, out tables))
            {
                tables = new Dictionary<int, Dictionary<int, float>>();
                Tuning[entry.tuning_type] = tables;
            }
            Dictionary<int, float> data;
            if (!tables.TryGetValue(entry.tuning_table, out data))
            {
                data = new Dictionary<int, float>();
                tables[entry.tuning_table] = data;
            }
            data[entry.tuning_index] = entry.value;
        }

        public DynamicTuning(DynamicTuning old)
        {
            foreach (var type in Tuning)
            {
                var newType = new Dictionary<int, Dictionary<int, float>>();
                foreach (var table in type.Value)
                {
                    var newTable = new Dictionary<int, float>();
                    foreach (var value in table.Value)
                    {
                        newTable[value.Key] = value.Value;
                    }
                    newType[table.Key] = newTable;
                }
                Tuning[type.Key] = newType;
            }
        }

        public DynamicTuning(BinaryReader reader)
        {
            Version = reader.ReadInt32();
            var count = reader.ReadInt32();
            for (int i=0; i<count; i++)
            {
                var key = reader.ReadString();
                var count2 = reader.ReadInt32();
                var newType = new Dictionary<int, Dictionary<int, float>>();
                for (int j = 0; j < count2; j++)
                {
                    var key2 = reader.ReadInt32();
                    var count3 = reader.ReadInt32();
                    var newTable = new Dictionary<int, float>();
                    for (int k=0; k<count3; k++)
                    {
                        var key3 = reader.ReadInt32();
                        newTable[key3] = reader.ReadSingle();
                    }
                    newType[key2] = newTable;
                }
                Tuning[key] = newType;
            }
        }

        public Dictionary<int, float> GetTable(string type, int table)
        {
            Dictionary<int, Dictionary<int, float>> tables;
            if (Tuning.TryGetValue(type, out tables))
            {
                Dictionary<int, float> data;
                if (tables.TryGetValue(table, out data))
                {
                    return data;
                }
            }
            return null;
        }

        public Dictionary<int, Dictionary<int, float>> GetTables(string type)
        {
            Dictionary<int, Dictionary<int, float>> tables;
            if (Tuning.TryGetValue(type, out tables))
            {
                return tables;
            }
            return null;
        }

        public float? GetTuning(string type, int table, int index)
        {
            Dictionary<int, Dictionary<int, float>> tables;
            if (Tuning.TryGetValue(type, out tables))
            {
                Dictionary<int, float> data;
                if (tables.TryGetValue(table, out data))
                {
                    float result;
                    if (data.TryGetValue(index, out result))
                        return result;
                }
            }
            return null;
        }
    }
}
