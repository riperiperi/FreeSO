using FSO.Common.DataService;
using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class AvatarRetireHandler
    {
        private IDAFactory DA;
        private IDataService DataService;
        private CityServerContext Context;
        private IKernel Kernel;

        public AvatarRetireHandler(CityServerContext context, IDAFactory da, IDataService dataService, IKernel kernel)
        {
            Context = context;
            DA = da;
            DataService = dataService;
            Kernel = kernel;
        }

        public async void Handle(IVoltronSession session, AvatarRetireRequest packet)
        {
            if (session.IsAnonymous) //CAS users can't do this.
                return;

            using (var da = DA.Get())
            {
                var avatar = da.Avatars.Get(session.AvatarId);
                if (avatar == null) return;

                var lots = da.Roommates.GetAvatarsLots(session.AvatarId);
                foreach (var roomie in lots)
                {
                    var lot = da.Lots.Get(roomie.lot_id);
                    if (lot == null) continue;
                    var kickResult = await Kernel.Get<ChangeRoommateHandler>().TryKick(lot.location, session.AvatarId, session.AvatarId);
                    //if something goes wrong here, just return.
                    if (kickResult != Protocol.Electron.Model.ChangeRoommateResponseStatus.SELFKICK_SUCCESS) return;
                }

                da.Avatars.Delete(session.AvatarId);
            }

            //now all our objects have null owner. objects with null owner will be cleaned out by the purge task (this needs to hit nfs & db).

            session.Close();
        }
    }
}
