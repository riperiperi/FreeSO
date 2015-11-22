using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Regulators
{
    public class JoinLotRegulator : AbstractRegulator
    {
        private AriesClient City;
        private uint LotId;

        public JoinLotRegulator([Named("City")] AriesClient cityClient)
        {
            this.City = cityClient;
            this.City.AddSubscriber(this);

            AddState("Floating")
                .Transition()
                .OnData(typeof(JoinLotRequest)).TransitionTo("JoinLot")
                .Default();

            AddState("JoinLot").OnlyTransitionFrom("Floating");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "JoinLot":
                    LotId = ((JoinLotRequest)data).LotId;
                    City.Write(new FSO.Server.Protocol.Electron.Packets.FindLotRequest {
                        LotId = LotId
                    });
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
    }

    class JoinLotRequest
    {
        public uint LotId;
    }
}
