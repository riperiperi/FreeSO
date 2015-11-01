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
