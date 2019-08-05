﻿using FSO.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotClaims
{
    public interface ILotClaims
    {
        uint? TryCreate(DbLotClaim claim);
        IEnumerable<DbLotClaim> GetAllByOwner(string owner);

        bool Claim(uint id, string previousOwner, string newOwner);
        DbLotClaim Get(uint id);
        DbLotClaim GetByLotID(int id);

        void RemoveAllByOwner(string owner);
        void Delete(uint id, string owner);
        List<DbLotStatus> AllLocations(int shardId);
        List<DbLotActive> AllActiveLots(int shardId);
        List<DbLotStatus> Top100Filter(int shard_id, LotCategory category, int limit);
        List<uint> RecentsFilter(uint avatar_id, int limit);
    }
}
