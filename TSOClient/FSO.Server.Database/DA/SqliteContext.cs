using System;
using System.Data.Common;
using System.Data;
using Microsoft.Data.Sqlite;
using FSO.Server.Database.SqliteCompat;

namespace FSO.Server.Database.DA
{
    internal class SqliteContext : ISqlContext, IDisposable
    {
        public bool SupportsFunctions => false;
        public bool UseBlobInventory => true;
        private readonly string _connectionString;
        private DbConnection _connection;
        private SqliteConnectionPool _pool;

        public SqliteContext(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public SqliteContext(SqliteConnectionPool pool)
        {
            this._pool = pool;
        }

        public DbConnection Connection
        {
            get
            {
                if (_connection == null)
                {

                    if (_pool != null)
                    {
                        _connection = _pool.Rent();
                    }
                    else
                    {
                        _connection = new SqliteConnection(_connectionString);
                    }
                }

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                return _connection;
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_pool != null)
                {
                    _pool.Return((SqliteConnection)_connection);
                }
                else
                {
                    _connection.Dispose();
                }

                _connection = null;
            }
        }

        public void Flush()
        {
            Dispose();
        }

        public string CompatLayer(string sql, string updateKey = null)
        {
            if (sql.StartsWith("INSERT IGNORE"))
            {
                sql = "INSERT OR " + sql.Substring("INSERT ".Length);
            }

            sql = sql.Replace("LAST_INSERT_ID()", "last_insert_rowid()");
            sql = sql.Replace("NOW()", "CURRENT_TIMESTAMP");

            if (updateKey != null)
            {
                sql = sql.Replace("ON DUPLICATE KEY UPDATE", $"ON CONFLICT({updateKey}) DO UPDATE SET");

                var valuesPatch = "VALUES(`value`);";
                if (sql.EndsWith(valuesPatch))
                {
                    sql = string.Concat(sql.AsSpan(0, sql.Length - valuesPatch.Length), "excluded.`value`;");
                }
            }

            return sql;
        }
    }
}
