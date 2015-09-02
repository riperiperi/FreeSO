using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Network.Regulators
{
    /// <summary>
    /// Handles authentication and city server network activity
    /// </summary>
    public class LoginRegulator : AbstractRegulator
    {
        public AuthResult AuthResult { get; internal set; }

        public LoginRegulator(){
            AddState("NotLoggedIn")
                .Default()
                    .Transition()
                        .OnData(typeof(AuthRequest)).TransitionTo("AuthLogin");

            AddState("AuthLogin").OnlyTransitionFrom("NotLoggedIn");
            AddState("InitialConnect").OnlyTransitionFrom("AuthLogin");
            AddState("AvatarData").OnlyTransitionFrom("InitialConnect");
            AddState("ShardStatus").OnlyTransitionFrom("AvatarData");
            AddState("LoggedIn").OnlyTransitionFrom("ShardStatus");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "AuthLogin":
                    var loginData = (AuthRequest)data;
                    break;
            }
        }
    }
}
