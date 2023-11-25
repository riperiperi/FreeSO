using System.Collections.Generic;

namespace FSO.Server.Database.DA.Tuning
{
    public interface ITuning
    {
        IEnumerable<DbTuning> All();
        IEnumerable<DbTuning> AllCategory(string type, int table);
        IEnumerable<DbTuningPreset> GetAllPresets();
        IEnumerable<DbTuningPresetItem> GetPresetItems(int preset_id);

        bool ActivatePreset(int preset_id, int owner_id);
        bool ClearPresetTuning(int owner_id);
        bool ClearInactiveTuning(int[] active_ids);

        int CreatePreset(DbTuningPreset preset);
        int CreatePresetItem(DbTuningPresetItem item);
        bool DeletePreset(int preset_id);
    }
}
