using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Database.DA.Users;
using FSO.Server.Database.DA.AuthTickets;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.LotClaims;
using FSO.Server.Database.DA.AvatarClaims;
using FSO.Server.Database.DA.Objects;
using FSO.Server.Database.DA.Relationships;
using FSO.Server.Database.DA.Roommates;
using FSO.Server.Database.DA.Bookmarks;
using FSO.Server.Database.DA.LotAdmit;
using FSO.Server.Database.DA.Outfits;
using FSO.Server.Database.DA.LotVisitors;
using FSO.Server.Database.DA.LotTop100;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Database.DA.Bonus;
using FSO.Server.Database.DA.LotVisitTotals;

namespace FSO.Server.Database.DA
{
    public class SqlDA : IDA
    {
        public ISqlContext Context;

        public SqlDA(ISqlContext context)
        {
            this.Context = context;
        }

        private IUsers _users;
        public IUsers Users
        {
            get
            {
                if(_users == null)
                {
                    _users = new SqlUsers(Context);
                }
                return _users;
            }
        }

        private IAuthTickets _authTickets;
        public IAuthTickets AuthTickets
        {
            get
            {
                if (_authTickets == null)
                {
                    _authTickets = new SqlAuthTickets(Context);
                }
                return _authTickets;
            }
        }

        private IShards _shards;
        public IShards Shards
        {
            get
            {
                if(_shards == null)
                {
                    _shards = new SqlShards(Context);
                }
                return _shards;
            }
        }

        private IAvatars _avatars;
        public IAvatars Avatars
        {
            get
            {
                if (_avatars == null)
                {
                    _avatars = new SqlAvatars(Context);
                }
                return _avatars;
            }
        }

        private IObjects _objects;
        public IObjects Objects
        {
            get
            {
                if (_objects == null)
                {
                    _objects = new SqlObjects(Context);
                }
                return _objects;
            }
        }

        private IRelationships _relationships;
        public IRelationships Relationships
        {
            get
            {
                if (_relationships == null)
                {
                    _relationships = new SqlRelationships(Context);
                }
                return _relationships;
            }
        }

        private IRoommates _roommates;
        public IRoommates Roommates
        {
            get
            {
                if (_roommates == null)
                {
                    _roommates = new SqlRoommates(Context);
                }
                return _roommates;
            }
        }

        private ILots _lots;
        public ILots Lots
        {
            get
            {
                if(_lots == null){
                    _lots = new SqlLots(Context);
                }
                return _lots;
            }
        }

        private ILotAdmit _LotAdmit;
        public ILotAdmit LotAdmit
        {
            get
            {
                if (_LotAdmit == null)
                {
                    _LotAdmit = new SqlLotAdmit(Context);
                }
                return _LotAdmit;
            }
        }

        private ILotClaims _LotClaims;
        public ILotClaims LotClaims
        {
            get
            {
                if(_LotClaims == null){
                    _LotClaims = new SqlLotClaims(Context);
                }
                return _LotClaims;
            }
        }

        private IAvatarClaims _AvatarClaims;
        public IAvatarClaims AvatarClaims
        {
            get
            {
                if(_AvatarClaims == null){
                    _AvatarClaims = new SqlAvatarClaims(Context);
                }
                return _AvatarClaims;
            }
        }

        private IBookmarks _Bookmarks;
        public IBookmarks Bookmarks
        {
            get
            {
                if(_Bookmarks == null)
                {
                    _Bookmarks = new SqlBookmarks(Context);
                }
                return _Bookmarks;
            }
        }

        private IOutfits _Outfits;
        public IOutfits Outfits
        {
            get
            {
                if(_Outfits == null)
                {
                    _Outfits = new SqlOutfits(Context);
                }
                return _Outfits;
            }
        }

        private ILotVisits _Visits;
        public ILotVisits LotVisits
        {
            get
            {
                if(_Visits == null)
                {
                    _Visits = new SqlLotVisits(Context);
                }
                return _Visits;
            }
        }

        private ILotTop100 _LotTop100;
        public ILotTop100 LotTop100
        {
            get
            {
                if(_LotTop100 == null)
                {
                    _LotTop100 = new SqlLotTop100(Context);
                }
                return _LotTop100;
            }
        }

        private IHosts _Hosts;
        public IHosts Hosts
        {
            get
            {
                if(_Hosts == null)
                {
                    _Hosts = new SqlHosts(Context);
                }
                return _Hosts;
            }
        }

        private ITasks _Tasks;
        public ITasks Tasks
        {
            get
            {
                if(_Tasks == null){
                    _Tasks = new SqlTasks(Context);
                }
                return _Tasks;
            }
        }

        private IBonus _Bonus;
        public IBonus Bonus
        {
            get
            {
                if(_Bonus == null)
                {
                    _Bonus = new SqlBonus(Context);
                }
                return _Bonus;
            }
        }

        private ILotVisitTotals _Totals;
        public ILotVisitTotals LotVisitTotals
        {
            get
            {
                if(_Totals == null)
                {
                    _Totals = new SqlLotVisitTotals(Context);
                }
                return _Totals;
            }
        }

        public void Flush()
        {
            Context.Flush();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
