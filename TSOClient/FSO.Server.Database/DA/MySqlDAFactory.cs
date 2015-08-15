using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA
{
    public class MySqlDAFactory : IDAFactory
    {
        private DatabaseConfiguration Config;

        public MySqlDAFactory(DatabaseConfiguration config)
        {
            this.Config = config;
        }

        public IDA Get()
        {
            return new SqlDA(new MySqlContext(Config.ConnectionString));
        }
    }
}
