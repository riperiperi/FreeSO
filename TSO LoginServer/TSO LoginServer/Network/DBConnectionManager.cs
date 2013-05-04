using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Sql;
using System.Data.SqlClient;

namespace TSO_LoginServer.Network
{
    public class DBConnectionManager
    {
        public static SqlConnection DBConnection;

        public static void Connect(string ConnectionString)
        {
            try
            {
                DBConnection = new SqlConnection(ConnectionString);
            }
            catch (Exception)
            {
                throw new NoDBConnection("Couldn't connect to database server!");
            }
        }
    }
}
