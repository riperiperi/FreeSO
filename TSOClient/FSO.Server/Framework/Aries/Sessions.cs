using FSO.Server.Framework.Voltron;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public class Sessions
    {
        private HashSet<IAriesSession> Aries;
        private HashSet<IVoltronSession> Voltron;

        public void Add(object session){
            if (session is IAriesSession){
                Aries.Add((IAriesSession)session);
            }else if (session is IVoltronSession){
                Voltron.Add((IVoltronSession)session);
            }
        }

        public void Remove(object session){
            if(session is IAriesSession){
                Aries.Remove((IAriesSession)session);
            }else if(session is IVoltronSession){
                Voltron.Remove((IVoltronSession)session);
            }
        }
    }
}
