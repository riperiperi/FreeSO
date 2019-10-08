using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Tasks;
using FSO.Server.Domain;
using FSO.Server.Protocol.Gluon.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Tasks.Domain
{
    public class NeighborhoodsTask : ITask
    {
        private IDAFactory DAFactory;
        private IGluonHostPool HostPool;

        public NeighborhoodsTask(IDAFactory DAFactory, IGluonHostPool hostPool)
        {
            this.DAFactory = DAFactory;
            this.HostPool = hostPool;
        }

        public void Run(TaskContext context)
        {
            var cityServers = HostPool.GetByRole(Database.DA.Hosts.DbHostRole.city);

            foreach (var city in cityServers)
            {
                city.Write(new CityNotify(CityNotifyType.NhoodUpdate));
            }
        }

        public void Abort()
        {
        }

        public DbTaskType GetTaskType()
        {
            return DbTaskType.neighborhood_tick;
        }
    }
}
