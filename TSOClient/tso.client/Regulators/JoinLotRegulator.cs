using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Regulators
{
    public class JoinLotRegulator : AbstractRegulator, IAriesMessageSubscriber
    {
        private AriesClient City;
        private uint LotId;

        public JoinLotRegulator([Named("City")] AriesClient cityClient)
        {
            this.City = cityClient;
            this.City.AddSubscriber(this);

            City.AddSubscriber(this);

            AddState("Floating")
                .Transition()
                .OnData(typeof(JoinLotRequest)).TransitionTo("Start")
                .Default();

            AddState("Start").OnlyTransitionFrom("Floating");
            AddState("FindLot").OnlyTransitionFrom("Start").OnData(typeof(FindLotResponse)).TransitionTo("FindLotResponse");
            AddState("FindLotResponse").OnlyTransitionFrom("FindLot");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "Start":
                    AsyncTransition("FindLot", data);
                    break;

                case "FindLot":
                    LotId = ((JoinLotRequest)data).LotId;
                    City.Write(new FSO.Server.Protocol.Electron.Packets.FindLotRequest {
                        LotId = LotId
                    });
                    break;
                case "FindLotResponse":
                    var result = (FindLotResponse)data;
                    if(result.Status == Server.Protocol.Electron.Model.FindLotResponseStatus.FOUND)
                    {
                        //Great, try and join
                    }
                    else
                    {
                        ThrowErrorAndReset(result.Status);
                    }
                    break;
            }
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        public void JoinLot(uint id)
        {
            AsyncProcessMessage(new JoinLotRequest { LotId = id });
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if(message is FindLotResponse)
            {
                AsyncProcessMessage(message);
            }
        }
    }

    class JoinLotRequest
    {
        public uint LotId;
    }
}
