using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Server.Database.DA.Tuning
{
    public class SqlTuning : AbstractSqlDA, ITuning
    {
        public SqlTuning(ISqlContext context) : base(context)
        {
        }

        public IEnumerable<DbTuning> All()
        {
            return Context.Connection.Query<DbTuning>("SELECT * FROM fso_tuning");
        }

        public IEnumerable<DbTuning> AllCategory(string type, int table)
        {
            return Context.Connection.Query<DbTuning>("SELECT * FROM fso_tuning WHERE tuning_type = @type AND tuning_table = @table", new { type = type, table = table });
        }

        public IEnumerable<DbTuningPreset> GetAllPresets()
        {
            return Context.Connection.Query<DbTuningPreset>("SELECT * FROM fso_tuning_presets");
        }

        public IEnumerable<DbTuningPresetItem> GetPresetItems(int preset_id)
        {
            return Context.Connection.Query<DbTuningPresetItem>("SELECT * FROM fso_tuning_preset_items WHERE preset_id = @preset_id", new { preset_id });
        }

        public bool ActivatePreset(int preset_id, int owner_id)
        {
            return Context.Connection.Execute("INSERT IGNORE INTO fso_tuning (tuning_type, tuning_table, tuning_index, value, owner_type, owner_id) " +
                "SELECT p.tuning_type, p.tuning_table, p.tuning_index, p.value, 'EVENT' as owner_type, @owner_id as owner_id " +
                "FROM fso_tuning_preset_items as p " +
                "WHERE p.preset_id = @preset_id", new { preset_id, owner_id }) > 0;
        }

        public bool ClearPresetTuning(int owner_id)
        {
            return Context.Connection.Execute("DELETE FROM fso_tuning WHERE owner_type = 'EVENT' AND owner_id = owner_id", new { owner_id } ) > 0;
        }

        public bool ClearInactiveTuning(int[] active_ids)
        {
            return Context.Connection.Execute("DELETE FROM fso_tuning WHERE owner_type = 'EVENT' AND owner_id NOT IN @active_ids", new { active_ids }) > 0;
        }

        public int CreatePreset(DbTuningPreset preset)
        {
            var result = Context.Connection.Query<int>("INSERT INTO fso_tuning_presets (name, description, flags) "
                + "VALUES (@name, @description, @flags); SELECT LAST_INSERT_ID();",
                preset).FirstOrDefault();
            return result;
        }

        public int CreatePresetItem(DbTuningPresetItem item)
        {
            var result = Context.Connection.Query<int>("INSERT INTO fso_tuning_preset_items (preset_id, tuning_type, tuning_table, tuning_index, value) "
                + "VALUES (@preset_id, @tuning_type, @tuning_table, @tuning_index, @value); SELECT LAST_INSERT_ID();",
                item).FirstOrDefault();
            return result;
        }

        public bool DeletePreset(int preset_id)
        {
            return Context.Connection.Execute("DELETE FROM fso_tuning_presets WHERE preset_id = @preset_id", new { preset_id }) > 0;
        }
    }
}
