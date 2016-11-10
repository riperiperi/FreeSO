using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Framework.Gluon;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.City.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class JoinLotHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private LotServerPicker PickingEngine;
        private LotAllocations Lots;
        private IDAFactory DAFactory;
        private CityServerContext Context;

        public JoinLotHandler(LotAllocations lots, LotServerPicker pickingEngine, IDAFactory da, CityServerContext context)
        {
            this.Lots = lots;
            this.PickingEngine = pickingEngine;
            this.DAFactory = da;
            this.Context = context;
        }

        public async void Handle(IVoltronSession session, FindLotRequest packet)
        {
            if (session.IsAnonymous) //CAS users can't do this.
                return;

            try
            {
                //special modes at 0x200-0x1000
                //0x200: join my job lot
                //0x201-0x2FF: create job lot of type/lotgroup (client CANNOT join on demand.)
                //   0x01-0x10: Robot Factory
                //   0x11-0x20: Restaurant
                //   0x21-0x30: DJ/Dancer 
                //0x300+: instanced lots (client CAN join on demand, and appear in data service)

                //note: lotgroup is not the same as a grade. eg. restaurant grade 5+6 share a lot. (zero based grade)
                //nightclub 7-10 share a lot group. (two lots, one chosen randomly)

                if (packet.LotId >= 0x200 && packet.LotId < 0x300)
                {
                    //join my job lot. 
                    //look up our avatar's current job and attempt to match them to a job lot with <max players, or a new one.

                    using (var db = DAFactory.Get())
                    {
                        var job = db.Avatars.GetCurrentJobLevel(session.AvatarId);
                        if (job == null)
                        {
                            session.Write(new FindLotResponse
                            {
                                Status = Protocol.Electron.Model.FindLotResponseStatus.UNKNOWN_ERROR,
                                LotId = packet.LotId
                            });
                        }
                        //ok, choose the correct type/lotgroup combo
                        var type = job.job_type;
                        //if (type > 2) type--; //cook and waiter share job lot
                        //if (type > 3) type--; //dj and dancer share job lot
                        packet.LotId = (uint)(0x201 + (type - 1) * 0x10 + job.job_level);
                    }
                }

                var find = await Lots.TryFindOrOpen(packet.LotId, session.AvatarId, session); //null reference exception possible here

                if (find.Status == Protocol.Electron.Model.FindLotResponseStatus.FOUND)
                {

                    DbLotServerTicket ticket = null;

                    using (var db = DAFactory.Get())
                    {
                        //I need a shard ticket so I can connect to the lot server and assume the correct avatar
                        ticket = new DbLotServerTicket
                        {
                            ticket_id = Guid.NewGuid().ToString().Replace("-", ""),
                            user_id = session.UserId,
                            avatar_id = session.AvatarId,
                            lot_owner = find.Server.CallSign,
                            date = Epoch.Now,
                            ip = session.IpAddress,
                            lot_id = find.LotDbId,
                            avatar_claim_id = session.AvatarClaimId,
                            avatar_claim_owner = Context.Config.Call_Sign
                        };

                        db.Lots.CreateLotServerTicket(ticket);
                    }

                    session.Write(new FindLotResponse
                    {
                        Status = find.Status,
                        LotId = find.LotId, //can be modified by job matchmaker
                        LotServerTicket = ticket.ticket_id,
                        Address = find.Server.PublicHost,
                        User = session.UserId.ToString()
                    });
                }
                else
                {
                    session.Write(new FindLotResponse
                    {
                        Status = find.Status,
                        LotId = packet.LotId
                    });
                }
            } catch (Exception e)
            {
                LOG.Error(e);
            }
        }

        public void Handle(IGluonSession session, TransferClaimResponse claimResponse)
        {
            if(claimResponse.Type == Protocol.Gluon.Model.ClaimType.LOT)
            {
                Lots.OnTransferClaimResponse(claimResponse);
            }
        }
    }
}
