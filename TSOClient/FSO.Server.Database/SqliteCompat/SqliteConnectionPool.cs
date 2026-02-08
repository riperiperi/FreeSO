using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace FSO.Server.Database.SqliteCompat
{
    internal class SqliteConnectionPool
    {
        private string _connectionString;
        private Stack<SqliteConnection> _pool = new Stack<SqliteConnection>();

        public SqliteConnectionPool(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqliteConnection Rent()
        {
            lock (_pool)
            {
                if (_pool.Count == 0)
                {
                    return new SqliteConnection(_connectionString);
                }

                return _pool.Pop();
            }
        }

        public void Return(SqliteConnection conn)
        {
            lock (_pool)
            {
                _pool.Push(conn);
            }
        }
    }
}
