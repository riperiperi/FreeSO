using FSO.Server.Database.DA.AuthTickets;
using FSO.Server.Database.DA.AvatarClaims;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Bookmarks;
using FSO.Server.Database.DA.LotAdmit;
using FSO.Server.Database.DA.LotClaims;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.LotTop100;
using FSO.Server.Database.DA.LotVisitors;
using FSO.Server.Database.DA.Objects;
using FSO.Server.Database.DA.Outfits;
using FSO.Server.Database.DA.Relationships;
using FSO.Server.Database.DA.Roommates;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Database.DA.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA
{
    public interface IDA : IDisposable
    {
        IUsers Users { get; }
        IAuthTickets AuthTickets { get; }
        IShards Shards { get; }
        IAvatars Avatars { get; }
        IObjects Objects { get; }
        IRelationships Relationships { get; }
        IRoommates Roommates { get; }
        ILots Lots { get; }
        ILotAdmit LotAdmit { get; }
        ILotClaims LotClaims { get; }
        IAvatarClaims AvatarClaims { get; }
        IBookmarks Bookmarks { get; }
        IOutfits Outfits { get; }
        ILotVisits LotVisits { get; }
        ILotTop100 LotTop100 { get; }

        void Flush();
    }
}
