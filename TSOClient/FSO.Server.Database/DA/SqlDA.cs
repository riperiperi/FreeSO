using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Database.DA.Users;

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

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
