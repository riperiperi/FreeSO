using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Tuning
{
    public interface ITuning
    {
        IEnumerable<DbTuning> All();
        IEnumerable<DbTuning> AllCategory(string type, int table);
    }
}
