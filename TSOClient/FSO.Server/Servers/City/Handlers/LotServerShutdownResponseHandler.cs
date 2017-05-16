using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.City.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class LotServerShutdownResponseHandler
    {
        private LotServerPicker Picker;
        public LotServerShutdownResponseHandler(LotServerPicker picker)
        {
            this.Picker = picker;
        }

        public void Handle(IGluonSession session, ShardShutdownCompleteResponse request)
        {
            Picker.RegisterShutdown(session);
        }
    }
}
