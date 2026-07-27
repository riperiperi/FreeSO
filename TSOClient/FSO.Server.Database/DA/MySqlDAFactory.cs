using Dapper;
using FSO.Server.Database.DA.Tuning;
using FSO.Server.Database.SqliteCompat;

namespace FSO.Server.Database.DA
{
    public class MySqlDAFactory : IDAFactory
    {
        private DatabaseConfiguration Config;

        public MySqlDAFactory(DatabaseConfiguration config)
        {
            this.Config = config;
            SqlMapper.AddTypeHandler(new DbEnumHandler<DbTuningType>());
        }

        public IDA Get()
        {
            return new SqlDA(new MySqlContext(Config.ConnectionString));
        }
    }
}
