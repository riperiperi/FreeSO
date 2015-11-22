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
    public class LotServerLifecycleHandler
    {
        private LotServerPicker PickingEngine;

        public LotServerLifecycleHandler(LotServerPicker pickingEngine)
        {
            this.PickingEngine = pickingEngine;
        }

        public void Handle(IGluonSession session, AdvertiseCapacity capacity)
        {
            PickingEngine.UpdateServerAdvertisement(session, capacity);
        }
    }
}
