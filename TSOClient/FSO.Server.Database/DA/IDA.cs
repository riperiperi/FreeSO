using FSO.Server.Database.DA.AuthTickets;
using FSO.Server.Database.DA.AvatarClaims;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Bans;
using FSO.Server.Database.DA.Bonus;
using FSO.Server.Database.DA.Bookmarks;
using FSO.Server.Database.DA.DbEvents;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Database.DA.Inbox;
using FSO.Server.Database.DA.LotAdmit;
using FSO.Server.Database.DA.LotClaims;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.LotTop100;
using FSO.Server.Database.DA.LotVisitors;
using FSO.Server.Database.DA.LotVisitTotals;
using FSO.Server.Database.DA.Objects;
using FSO.Server.Database.DA.Outfits;
using FSO.Server.Database.DA.Relationships;
using FSO.Server.Database.DA.Roommates;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Database.DA.Users;
using FSO.Server.Database.DA.Tuning;
using System;
using FSO.Server.Database.DA.Transactions;
using FSO.Server.Database.DA.DynPayouts;
using FSO.Server.Database.DA.EmailConfirmation;
using FSO.Server.Database.DA.Neighborhoods;
using FSO.Server.Database.DA.Elections;
using FSO.Server.Database.DA.Bulletin;
using FSO.Server.Database.DA.Updates;
using FSO.Server.Database.DA.GlobalCooldowns;

namespace FSO.Server.Database.DA
{
    public interface IDA : IDisposable
    {
        IUsers Users { get; }
        IBans Bans { get; }
        IAuthTickets AuthTickets { get; }
        IShards Shards { get; }
        IAvatars Avatars { get; }
        IObjects Objects { get; }
        IRelationships Relationships { get; }
        IRoommates Roommates { get; }
        ILots Lots { get; }
        ILotAdmit LotAdmit { get; }
        ILotClaims LotClaims { get; }
        INeighborhoods Neighborhoods { get; }
        IElections Elections { get; }
        IBulletinPosts BulletinPosts { get; }
        IAvatarClaims AvatarClaims { get; }
        IBookmarks Bookmarks { get; }
        IOutfits Outfits { get; }
        ILotVisits LotVisits { get; }
        ILotVisitTotals LotVisitTotals { get; }
        ILotTop100 LotTop100 { get; }
        IBonus Bonus { get; }
        IInbox Inbox { get; }
        IEvents Events { get; }
        ITuning Tuning { get; }
        IDynPayouts DynPayouts { get; }
        ITransactions Transactions { get; }

        //System tables
        IHosts Hosts { get; }
        ITasks Tasks { get; }
        IEmailConfirmations EmailConfirmations { get; }
        IUpdates Updates { get; }
        IGlobalCooldowns GlobalCooldowns { get; }
        void Flush();
    }
}
