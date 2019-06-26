using FSO.Common.Model;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Tuning;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Domain
{
    public class Tuning
    {
        private LotServerPicker LotServers;
        private DynamicTuning TuningCache;
        private IDAFactory DAFactory;

        public Tuning(LotServerPicker LotServers, IDAFactory da)
        {
            this.LotServers = LotServers;
            DAFactory = da;
        }

        public void UpdateTuningCache()
        {
            lock (this)
            {
                using (var da = DAFactory.Get())
                {
                    TuningCache = new DynamicTuning(da.Tuning.All());
                }
            }
        }

        public void UserJoined(IVoltronSession session)
        {
            if (TuningCache == null) UpdateTuningCache();
            session.Write(new GlobalTuningUpdate() { Tuning = TuningCache });
        }

        public void BroadcastTuningUpdate(bool updateImmediately)
        {
            LotServers.BroadcastMessage(new TuningChanged() { UpdateInstantly = updateImmediately });
            UpdateTuningCache();
        }
    }
}
