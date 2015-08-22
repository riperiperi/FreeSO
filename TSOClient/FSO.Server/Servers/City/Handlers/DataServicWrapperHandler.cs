using FSO.Server.Framework.Aries;
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

        public DataServicWrapperHandler(cTSOSerializer serializer){
            this.Serializer = serializer;
        }

        /// <summary>
        /// The user is asking for some in RAM data
        /// </summary>
        /// <param name="session"></param>
        /// <param name="packet"></param>
        public void Handle(IAriesSession session, DataServiceWrapperPDU packet)
        {
            if(packet.Body is cTSONetMessageStandard)
            {
                var msg = (cTSONetMessageStandard)packet.Body;
                var entity = Serializer.GetDerivedStruct(packet.RequestTypeID);
                if (entity == null) { return; }

                switch (entity.Parent){
                    //Avatar
                    case 0x05600332:
                        var avatarId = msg.RequestParameter;
                        break;
                    
                    //City
                    case 0xED56D057:
                        break;
                }

                //packet.RequestTypeID
            }



            //SimPage_Main
            if (packet.RequestTypeID == 0xD042E9D6)
            {
                var avatar = new Avatar {
                    Avatar_Name = "Bob",
                    Avatar_IsFounder = true,
                    Avatar_IsOnline = true,
                    Avatar_LotGridXY = 0,
                    Avatar_Appearance = new AvatarAppearance {
                        AvatarAppearance_SkinTone = 1,
                        AvatarAppearance_HeadOutfitID = 3990024617997,
                        AvatarAppearance_BodyOutfitID = 2516850835469
                    },
                    Avatar_Description = "Hello world\nThis is my description"
                };

                var request = (cTSONetMessageStandard)packet.Body;
                request.MessageID = 0x09736027;


                var fields = Serializer.SerializeDerived(request.RequestResponseType.Value, request.RequestParameter.Value, avatar);

                foreach(var field in fields){
                    session.Write(new DataServiceWrapperPDU(){
                        SendingAvatarID = packet.SendingAvatarID,
                        RequestTypeID = packet.RequestTypeID,
                        Body = field
                    });
                }

                
            }
        }
    }
}
