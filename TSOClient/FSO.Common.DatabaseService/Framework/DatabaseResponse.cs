using FSO.Common.DatabaseService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DatabaseService.Framework
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class DatabaseResponse : System.Attribute
    {
        public DBResponseType Type;

        public DatabaseResponse(DBResponseType type)
        {
            this.Type = type;
        }
    }
}
