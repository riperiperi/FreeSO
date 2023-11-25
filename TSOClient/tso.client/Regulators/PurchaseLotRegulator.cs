using FSO.Common.Utils;
using FSO.Server.Clients;
using FSO.Server.Clients.Framework;

namespace FSO.Client.Regulators
{
    public class PurchaseLotRegulator : AbstractRegulator, IAriesMessageSubscriber
    {
        private PurchaseLotRequest CurrentRequest;
        private Network.Network Network;

        public delegate void PurchasedLotHandler(int newBudget);
        public event PurchasedLotHandler OnPurchased;

        public PurchaseLotRegulator(Network.Network network)
        {
            Network = network;
            Network.CityClient.AddSubscriber(this);

            AddState("Idle").Transition().Default()
                .OnData(typeof(PurchaseLotRequest))
                .TransitionTo("StartPurchase");

            AddState("StartPurchase").OnlyTransitionFrom("Idle");
            AddState("SendPurchaseRequest").OnlyTransitionFrom("StartPurchase")
                .OnData(typeof(Server.Protocol.Electron.Packets.PurchaseLotResponse))
                .TransitionTo("ReceivedPurchaseResponse");

            AddState("ReceivedPurchaseResponse").OnlyTransitionFrom("SendPurchaseRequest");
            AddState("PurchaseComplete").OnlyTransitionFrom("ReceivedPurchaseResponse");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "StartPurchase":
                    if (!Network.CityClient.IsConnected){
                        ThrowErrorAndReset("City server disconnected");
                        return;
                    }
                    CurrentRequest = data as PurchaseLotRequest;
                    AsyncTransition("SendPurchaseRequest");
                    break;
                case "SendPurchaseRequest":
                    Network.CityClient.Write(new FSO.Server.Protocol.Electron.Packets.PurchaseLotRequest
                    {
                        LotLocation_X = CurrentRequest.X,
                        LotLocation_Y = CurrentRequest.Y,
                        Name = CurrentRequest.Name,
                        StartFresh = CurrentRequest.StartFresh,
                        MayorMode = CurrentRequest.Mayor
                    });
                    break;
                case "ReceivedPurchaseResponse":
                    var response = data as FSO.Server.Protocol.Electron.Packets.PurchaseLotResponse;
                    if(response.Status == Server.Protocol.Electron.Packets.PurchaseLotStatus.FAILED){
                        //Error
                        ThrowErrorAndReset(response.Reason);
                    }else{

                        AsyncTransition("PurchaseComplete");
                        GameThread.NextUpdate(x => { OnPurchased(response.NewFunds); });
                    }
                    break;
                case "PurchaseComplete":
                    AsyncTransition("Idle");
                    break;
            }
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {

        }

        public void Purchase(PurchaseLotRequest request){
            this.AsyncProcessMessage(request);
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if(message is Server.Protocol.Electron.Packets.PurchaseLotResponse){
                AsyncProcessMessage(message);
            }
        }
    }

    public class PurchaseLotRequest
    {
        public ushort X;
        public ushort Y;
        public string Name;
        public bool StartFresh;
        public bool Mayor;
    }
}
