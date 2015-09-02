using FSO.Server.DataService.Avatars;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.DataService;
using FSO.Server.Protocol.Voltron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class DataServicWrapperHandler
    {
        private cTSOSerializer Serializer;
        private AvatarsDataService AvatarDataService;

        public DataServicWrapperHandler(cTSOSerializer serializer, AvatarsDataService avatarDataService)
        {
            this.Serializer = serializer;
            this.AvatarDataService = avatarDataService;
        }

        /// <summary>
        /// The user is asking for some in RAM data
        /// </summary>
        /// <param name="session"></param>
        /// <param name="packet"></param>
        public void Handle(IVoltronSession session, DataServiceWrapperPDU packet)
        {
            if(packet.Body is cTSONetMessageStandard)
            {
                var msg = (cTSONetMessageStandard)packet.Body;
                var entity = Serializer.GetDerivedStruct(packet.RequestTypeID);
                if (entity == null) { return; }

                object entityValue = null;

                switch (entity.Parent){
                    //Avatar
                    case 0x05600332:
                        var avatarId = msg.Parameter;
                        entityValue = AvatarDataService.Get(avatarId.Value);
                        break;

                    //Lot
                    case 0x7A6CECEE:
                        var lotLocation = msg.Parameter;
                        entityValue = new Lot {
                            Lot_Name = null,
                            Lot_IsOnline = false,
                            Lot_Location = new Location { Location_X = 0, Location_Y = 0 },
                            Lot_Price = 999,
                            Lot_OwnerVec = new List<uint>() { },
                            Lot_RoommateVec = new List<uint>() { },
                            Lot_NumOccupants = 0
                        };
                        break;

                    //City
                    case 0xED56D057:
                        break;
                }

                if(entityValue != null){
                    var fields = Serializer.SerializeDerived(msg.DataServiceType.Value, 
                                                             msg.Parameter.Value, 
                                                             entityValue);

                    foreach (var field in fields)
                    {
                        session.Write(new DataServiceWrapperPDU()
                        {
                            SendingAvatarID = packet.SendingAvatarID,
                            RequestTypeID = packet.RequestTypeID,
                            Body = field
                        });
                    }
                }

                //packet.RequestTypeID
            }


            
        }
    }
}
