using Dapper;
using FSO.Server.Database.DA.Tuning;
using FSO.Server.Database.SqliteCompat;
using System.Net.NetworkInformation;

namespace FSO.Server.Database.DA
{
    public class SqliteDAFactory : IDAFactory
    {
        private DatabaseConfiguration Config;

        private SqliteConnectionPool _pool;

        public SqliteDAFactory(DatabaseConfiguration config)
        {
            this.Config = config;

            // TODO: pass config connection string
            // _pool = new SqliteConnectionPool("Data Source=fsoarchive.db;Version=3;UTF8Encoding=True");

            SqlMapper.AddTypeHandler(new ByteHandler());
            SqlMapper.AddTypeHandler(new SbyteHandler());
            SqlMapper.AddTypeHandler(new Uint16Handler());
            SqlMapper.AddTypeHandler(new Uint32Handler());
            SqlMapper.AddTypeHandler(new Int32Handler());
            SqlMapper.AddTypeHandler(new DbEnumHandler<DbTuningType>());
        }

        public IDA Get()
        {
            // Currently not using the pool.
            return new SqlDA(new SqliteContext(Config.ConnectionString));//new SqliteContext(_pool));
        }
    }
}
