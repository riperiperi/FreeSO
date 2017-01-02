using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Utils
{
    public static class BufferedInsert
    {
        public static void ExecuteBufferedInsert(this DbConnection connection, string query, IEnumerable<object> param, int batches)
        {
            var buffer = new List<object>();
            var enumerator = param.GetEnumerator();

            while (enumerator.MoveNext())
            {
                buffer.Add(enumerator.Current);

                if(buffer.Count >= batches)
                {
                    connection.Execute(query, buffer);
                    buffer.Clear();
                }
            }

            if(buffer.Count > 0)
            {
                connection.Execute(query, buffer);
            }
        }
    }
}
