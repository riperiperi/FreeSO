using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;

namespace FSO.Client.Regulators
{
    public class GenericActionRegulator<T, T2> : AbstractRegulator, IAriesMessageSubscriber where T: IActionRequest where T2 : IActionResponse
    {
        public T CurrentRequest;
        private Network.Network Network;
        public NhoodCandidateList CandidateList;

        public GenericActionRegulator(Network.Network network)
        {
            Network = network;
            Network.CityClient.AddSubscriber(this);

            AddState("Idle").Transition().Default()
                .OnData(typeof(T))
                .TransitionTo("StartAction");

            AddState("StartAction").OnlyTransitionFrom("Idle");
            //if action needs validation, transitions to ValidateAction.
            //otherwise, if action needs further input, transition to ActionInput
            //otherwise, transtion directly to PerformAction.
            AddState("GenericFailure");


            AddState("ValidateAction").OnlyTransitionFrom("StartAction")
                .OnData(typeof(T2))
                .TransitionTo("ValidateActionResponse");

            AddState("ValidateActionResponse").OnlyTransitionFrom("ValidateAction");
            //if failed, branch into GenericFailure
            AddState("ActionInput").OnlyTransitionFrom("ValidateActionResponse", "StartAction")
                .OnData(typeof(T))
                .TransitionTo("PerformAction");

            AddState("PerformAction").OnlyTransitionFrom("ActionInput", "StartAction")
                .OnData(typeof(T2))
                .TransitionTo("PerformActionResponse");

            AddState("PerformActionResponse").OnlyTransitionFrom("PerformAction");

            AddState("ActionSuccess").OnlyTransitionFrom("PerformActionResponse");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "StartAction":
                    if (!Network.CityClient.IsConnected)
                    {
                        ThrowErrorAndReset("City server disconnected");
                        return;
                    }
                    CurrentRequest = (T)data;
                    if (CurrentRequest.NeedsValidation)
                        AsyncTransition("ValidateAction");
                    else
                        AsyncTransition("PerformAction", data);
                    break;
                case "GenericFailure":
                    ThrowErrorAndReset(data);
                    AsyncTransition("Idle");
                    break;
                case "ValidateAction":
                    Network.CityClient.Write(CurrentRequest);
                    break;
                case "ValidateActionResponse":
                    var vresponse = (T2)data;
                    if (!vresponse.Success)
                        AsyncTransition("GenericFailure", data);
                    else
                        AsyncTransition("ActionInput");
                    break;
                case "ActionInput":
                    //a controller should pop up a dialog at this point
                    break;
                case "PerformAction":
                    if (!Network.CityClient.IsConnected)
                    {
                        ThrowErrorAndReset("City server disconnected");
                        return;
                    }
                    CurrentRequest = (T)data;
                    Network.CityClient.Write(CurrentRequest);
                    break;
                case "PerformActionResponse":
                    var response = (T2)data;
                    if (!response.Success)
                        AsyncTransition("GenericFailure", data);
                    else
                        AsyncTransition("ActionSuccess", data);
                    break;
                case "ActionSuccess":
                    AsyncTransition("Idle");
                    break;
            }
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {

        }

        public void MakeRequest(T request)
        {
            this.AsyncProcessMessage(request);
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is T || message is T2)
            {
                AsyncProcessMessage(message);
            }
            else if (message is NhoodCandidateList)
            {
                CandidateList = (NhoodCandidateList)message;
            }
        }
    }
}
