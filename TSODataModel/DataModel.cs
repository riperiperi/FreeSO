using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace TSODataModel
{
    public class DataModel
    {
        public static string ConnectionString;

        public static PD Get()
        {
            var inst = new PD(
                new MySqlConnection(ConnectionString)
            );

            return inst;
        }
    }
}
