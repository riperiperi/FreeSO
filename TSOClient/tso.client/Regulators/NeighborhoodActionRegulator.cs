using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Regulators
{
    public class NeighborhoodActionRegulator : AbstractRegulator, IAriesMessageSubscriber
    {
        public NhoodRequest CurrentRequest;
        public NhoodCandidateList CandidateList;
        private Network.Network Network;

        public NeighborhoodActionRegulator(Network.Network network)
        {
            Network = network;
            Network.CityClient.AddSubscriber(this);

            AddState("Idle").Transition().Default()
                .OnData(typeof(NhoodRequest))
                .TransitionTo("StartNhoodAction");

            AddState("StartNhoodAction").OnlyTransitionFrom("Idle");
            //if action needs validation, transitions to ValidateAction.
            //otherwise, if action needs further input, transition to ActionInput
            //otherwise, transtion directly to PerformAction.
            AddState("GenericFailure");


            AddState("ValidateAction").OnlyTransitionFrom("StartNhoodAction")
                .OnData(typeof(NhoodResponse))
                .TransitionTo("ValidateActionResponse");

            AddState("ValidateActionResponse").OnlyTransitionFrom("ValidateAction");
            //if failed, branch into GenericFailure
            AddState("ActionInput").OnlyTransitionFrom("ValidateActionResponse", "StartNhoodAction")
                .OnData(typeof(NhoodRequest))
                .TransitionTo("PerformAction");

            AddState("PerformAction").OnlyTransitionFrom("ActionInput", "StartNhoodAction")
                .OnData(typeof(NhoodResponse))
                .TransitionTo("PerformActionResponse");

            AddState("PerformActionResponse").OnlyTransitionFrom("PerformAction");

            AddState("ActionSuccess").OnlyTransitionFrom("PerformActionResponse");
        }

        private bool ActionNeedsVerification(NhoodRequestType type)
        {
            return (type == NhoodRequestType.CAN_NOMINATE || type == NhoodRequestType.CAN_RATE || type == NhoodRequestType.CAN_VOTE || type == NhoodRequestType.CAN_RUN);
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "StartNhoodAction":
                    if (!Network.CityClient.IsConnected)
                    {
                        ThrowErrorAndReset("City server disconnected");
                        return;
                    }
                    CurrentRequest = data as NhoodRequest;
                    if (ActionNeedsVerification(CurrentRequest.Type))
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
                    var vresponse = data as NhoodResponse;
                    if (vresponse.Code != NhoodResponseCode.SUCCESS)
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
                    CurrentRequest = data as NhoodRequest;
                    Network.CityClient.Write(CurrentRequest);
                    break;
                case "PerformActionResponse":
                    var response = data as NhoodResponse;
                    if (response.Code != NhoodResponseCode.SUCCESS)
                        AsyncTransition("GenericFailure", data);
                    else
                        AsyncTransition("ActionSuccess");
                    break;
                case "ActionSuccess":
                    AsyncTransition("Idle");
                    break;
            }
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {

        }

        public void MakeRequest(NhoodRequest request)
        {
            this.AsyncProcessMessage(request);
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is NhoodResponse || message is NhoodRequest)
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
