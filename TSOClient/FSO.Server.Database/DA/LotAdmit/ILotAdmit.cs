using System.Collections.Generic;

namespace FSO.Server.Database.DA.LotAdmit
{
    public interface ILotAdmit
    {
        List<DbLotAdmit> GetLotInfo(int lot_id);
        List<uint> GetLotAdmitDeny(int lot_id, byte admit_mode);
        void Create(DbLotAdmit bookmark);
        bool Delete(DbLotAdmit bookmark);
    }
}
