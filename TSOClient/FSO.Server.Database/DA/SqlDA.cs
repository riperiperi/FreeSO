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
using FSO.Server.Database.DA.Bans;
using FSO.Server.Database.DA.Inbox;
using FSO.Server.Database.DA.DbEvents;
using FSO.Server.Database.DA.Tuning;
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

        private IBans _bans;
        public IBans Bans
        {
            get
            {
                if (_bans == null)
                {
                    _bans = new SqlBans(Context);
                }
                return _bans;
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

        private INeighborhoods _Neighborhoods;
        public INeighborhoods Neighborhoods
        {
            get
            {
                if (_Neighborhoods == null)
                {
                    _Neighborhoods = new SqlNeighborhoods(Context);
                }
                return _Neighborhoods;
            }
        }

        private IElections _Elections;
        public IElections Elections
        {
            get
            {
                if (_Elections == null)
                {
                    _Elections = new SqlElections(Context);
                }
                return _Elections;
            }
        }

        private IBulletinPosts _BulletinPosts;
        public IBulletinPosts BulletinPosts
        {
            get
            {
                if (_BulletinPosts == null)
                {
                    _BulletinPosts = new SqlBulletinPosts(Context);
                }
                return _BulletinPosts;
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

        private IInbox _Inbox;
        public IInbox Inbox
        {
            get
            {
                if (_Inbox == null) _Inbox = new SqlInbox(Context);
                return _Inbox;
            }
        }

        private IEvents _Events;
        public IEvents Events
        {
            get
            {
                if (_Events == null) _Events = new SqlEvents(Context);
                return _Events;
            }
        }

        private ITuning _Tuning;
        public ITuning Tuning
        {
            get
            {
                if (_Tuning == null) _Tuning = new SqlTuning(Context);
                return _Tuning;
            }
        }


        private IDynPayouts _DynPayouts;
        public IDynPayouts DynPayouts
        {
            get
            {
                if (_DynPayouts == null) _DynPayouts = new SqlDynPayouts(Context);
                return _DynPayouts;
            }
        }

        private ITransactions _Transactions;
        public ITransactions Transactions
        {
            get
            {
                if (_Transactions == null) _Transactions = new SqlTransactions(Context);
                return _Transactions;
            }
        }

        private IUpdates _Updates;
        public IUpdates Updates
        {
            get
            {
                if (_Updates == null) _Updates = new SqlUpdates(Context);
                return _Updates;
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

        private IEmailConfirmations _Confirmations;
        public IEmailConfirmations EmailConfirmations
        {
            get
            {
                if (_Confirmations == null)
                {
                    _Confirmations = new SqlEmailConfirmations(Context);
                }
                return _Confirmations;
            }
        }

        private IGlobalCooldowns _Cooldowns;
        public IGlobalCooldowns GlobalCooldowns
        {
            get
            {
                if (_Cooldowns == null)
                {
                    _Cooldowns = new SqlGlobalCooldowns(Context);
                }
                return _Cooldowns;
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
