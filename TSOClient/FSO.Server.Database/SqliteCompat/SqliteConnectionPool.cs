using System.Collections.Generic;
using System.Data.SQLite;

namespace FSO.Server.Database.SqliteCompat
{
    internal class SqliteConnectionPool
    {
        private string _connectionString;
        private Stack<SQLiteConnection> _pool = new Stack<SQLiteConnection>();

        public SqliteConnectionPool(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SQLiteConnection Rent()
        {
            lock (_pool)
            {
                if (_pool.Count == 0)
                {
                    return new SQLiteConnection(_connectionString);
                }

                return _pool.Pop();
            }
        }

        public void Return(SQLiteConnection conn)
        {
            lock (_pool)
            {
                _pool.Push(conn);
            }
        }
    }
}
