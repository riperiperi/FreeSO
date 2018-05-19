using FSO.Common.DataService;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class AvatarRetireHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
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

            try
            {

                using (var da = DA.Get())
                {
                    var avatar = da.Avatars.Get(session.AvatarId);
                    if (avatar == null) return;

                    if (avatar.date > Epoch.Now - (60 * 60 * 24 * 7) && !da.Users.GetById(session.UserId).is_admin)
                    {
                        session.Write(new Protocol.Voltron.Packets.AnnouncementMsgPDU()
                        {
                            SenderID = "??" + "System",
                            Message = "\r\n" + "You cannot delete a sim younger than a week old!",
                            Subject = "Error"
                        });
                        LOG.Info("Avatar " + avatar.name + " under account " + avatar.user_id + " attempted to delete a less than week old account.");
                        
                        session.Close();
                        return;
                    }
                    LOG.Info("Deleting avatar " + avatar.name + " under account " + avatar.user_id + ".");

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
            } catch
            {

            }
        }
    }
}
