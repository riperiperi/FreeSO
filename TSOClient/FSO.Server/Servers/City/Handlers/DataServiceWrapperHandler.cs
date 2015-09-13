using FSO.Common.DataService;
using FSO.Common.Serialization.Primitives;
using FSO.Server.DataService.Avatars;
using FSO.Server.DataService.Model;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron;
using FSO.Server.Protocol.Voltron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class DataServiceWrapperHandler
    {
        private IDataService DataService;

        public DataServiceWrapperHandler(IDataService dataService)
        {
            this.DataService = dataService;
        }

        /// <summary>
        /// The user is asking for some in RAM data
        /// </summary>
        /// <param name="session"></param>
        /// <param name="packet"></param>
        public async void Handle(IVoltronSession session, DataServiceWrapperPDU packet)
        {
            if(packet.Body is cTSONetMessageStandard){
                
                var msg = (cTSONetMessageStandard)packet.Body;
                var type = MaskedStructUtils.FromID(packet.RequestTypeID);

                if (!msg.Parameter.HasValue)
                {
                    return;
                }
                
                if(type != MaskedStruct.MyAvatar && type != MaskedStruct.SimPage_Main && type != MaskedStruct.MapView_RollOverInfo_Lot_Price
                    && type != MaskedStruct.MapView_RollOverInfo_Lot)
                {
                    //Currently broken for some reason
                    return;
                }

                //Lookup the entity, then process the request and send the response
                var task = DataService.Get(type, msg.Parameter.Value);
                if(task != null)
                {
                    var entity = await task;

                    var serialized = DataService.SerializeUpdate(type, entity, msg.Parameter.Value);
                    for (int i = 0; i < serialized.Count; i++)
                    {
                        session.Write(new DataServiceWrapperPDU()
                        {
                            SendingAvatarID = packet.SendingAvatarID,
                            RequestTypeID = packet.RequestTypeID,
                            Body = serialized[i]
                        });
                    }
                }
            }else if(packet.Body is cTSOTopicUpdateMessage)
            {
                //Client wants to update a value in the data service
                var affected = await DataService.ApplyUpdate(packet.Body as cTSOTopicUpdateMessage);
                /*session.Write(new DataServiceWrapperPDU()
                {
                    SendingAvatarID = packet.SendingAvatarID,
                    RequestTypeID = packet.RequestTypeID,
                    Body = affected
                });*/
            }
        }
    }
}
