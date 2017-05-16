using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA
{
    public class AbstractSqlDA
    {
        protected ISqlContext Context;

        public AbstractSqlDA(ISqlContext context)
        {
            this.Context = context;
        }
    }
}
