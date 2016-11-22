using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class ChangeRoommateHandler
    {
        private IDAFactory DAFactory;
        private CityServerContext Context;
        public ChangeRoommateHandler(IDAFactory da, CityServerContext context)
        {
            this.DAFactory = da;
            this.Context = context;
        }

        public void Handle(IVoltronSession session, ChangeRoommateRequest packet)
        {
            if (session.IsAnonymous) return;
            using (var da = DAFactory.Get())
            {
                //get the lot info to see if we're an owner (if removing and removed avatar is not ourselves)
                //TODO
                //verify that target is definitely a roommate in the target lot
                da.Roommates.GetAvatarsLots(session.AvatarId);

                if (packet.Type == ChangeRoommateType.INVITE)
                {
                    //is invitee roommate somewhere else? count lot roommates and check for max


                    //create roommate request in database

                    //FAILED: attempt 1: remove other pending roommates for this lot. try again
                    //FAILED: attempt 2: unknown error, likely race condition to do with those first checks

                    //if online, notify roommate of pending request.
                    //if not, we'll catch them when they log in later.
                }
                else if (packet.Type == ChangeRoommateType.KICK)
                {
                    //if target avatar is our avatar, we are moving out

                    //if we are owner of the lot, set the new owner to the first (earliest) roommate entry in the database.
                    //make sure all references are set to new owner!

                    //remove roommate entry for target avatar.
                    //update lot data service and avatar data service for targets.

                    //if lot open, notify lot server of change (roommate add/remove AND new/same owner)
                    //the lot will remove objects as necessary

                    //future: if lot closed, special request to a lot server to quickly open an unjoinable instance of the lot to remove our objects.
                }
            }
        }
    }
}
