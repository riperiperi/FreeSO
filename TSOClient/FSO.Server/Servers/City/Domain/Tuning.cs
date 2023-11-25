using FSO.Common.Model;
using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using System.IO;

namespace FSO.Server.Servers.City.Domain
{
    public class Tuning
    {
        private LotServerPicker LotServers;
        private DynamicTuning TuningCache;
        private byte[] ObjectUpgradeData;
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

                var upgrades = Content.Content.Get().Upgrades.ActiveFile;
                if (upgrades == null) ObjectUpgradeData = new byte[0];
                else {
                    using (var mem = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(mem))
                        {
                            upgrades.Save(writer);
                        }
                        ObjectUpgradeData = mem.ToArray();
                    }
                }
            }
        }

        public void UserJoined(IVoltronSession session)
        {
            if (TuningCache == null) UpdateTuningCache();
            session.Write(new GlobalTuningUpdate() { Tuning = TuningCache, ObjectUpgrades = ObjectUpgradeData });
        }

        public void BroadcastTuningUpdate(bool updateImmediately)
        {
            LotServers.BroadcastMessage(new TuningChanged() { UpdateInstantly = updateImmediately });
            UpdateTuningCache();
        }
    }
}
