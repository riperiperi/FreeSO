using FSO.Client.Utils;
using FSO.Common.Domain.Shards;
using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Authorization;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Regulators
{
    /// <summary>
    /// Handles authentication and city server network activity
    /// </summary>
    public class LoginRegulator : AbstractRegulator
    {
        public AuthResult AuthResult { get; internal set; }
        public List<AvatarData> Avatars { get; internal set; } = new List<AvatarData>();
        //public List<ShardStatusItem> Shards { get; internal set; } = new List<ShardStatusItem>();
        public IShardsDomain Shards;

        private AuthClient AuthClient;
        private CityClient CityClient;

        public LoginRegulator(AuthClient authClient, CityClient cityClient, IShardsDomain domain)
        {
            this.Shards = domain;
            this.AuthClient = authClient;
            this.CityClient = cityClient;
            
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
            switch (newState.Name)
            {
                case "AuthLogin":
                    var loginData = (AuthRequest)data;
                    var result = AuthClient.Authenticate(loginData);

                    if (result == null || !result.Valid)
                    {
                        if (result.ReasonText != null){
                            base.ThrowErrorAndReset(ErrorMessage.FromLiteral(result.ReasonText));
                        }else{
                            base.ThrowErrorAndReset(new Exception("Unknown error"));
                        }
                    }
                    else
                    {
                        this.AuthResult = result;
                        AsyncTransition("InitialConnect");
                    }
                    break;
                case "InitialConnect":
                    try {
                        var connectResult = CityClient.InitialConnectServlet(
                            new InitialConnectServletRequest {
                                Ticket = AuthResult.Ticket,
                                Version = "Version 1.1097.1.0"
                            });

                        if (connectResult.Status == InitialConnectServletResultType.Authorized)
                        {
                            AsyncTransition("AvatarData");
                        }
                        else if (connectResult.Status == InitialConnectServletResultType.Error)
                        {
                            base.ThrowErrorAndReset(ErrorMessage.FromLiteral(connectResult.Error.Code, connectResult.Error.Message));
                        }
                    }catch(Exception ex)
                    {
                        base.ThrowErrorAndReset(ex);
                    }
                    break;
                case "AvatarData":
                    try {
                        Avatars = CityClient.AvatarDataServlet();
                        AsyncTransition("ShardStatus");
                    }
                    catch (Exception ex)
                    {
                        base.ThrowErrorAndReset(ex);
                    }
                    break;

                case "ShardStatus":
                    try {
                        ((ClientShards)Shards).All = CityClient.ShardStatus();
                        AsyncTransition("LoggedIn");
                    }
                    catch (Exception ex)
                    {
                        base.ThrowErrorAndReset(ex);
                    }
                    break;
                case "LoggedIn":
                    GameFacade.Controller.ShowPersonSelection();
                    break;
            }
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        public void Login(AuthRequest request){
            this.AsyncProcessMessage(request);
        }

        public void Logout()
        {
            this.AsyncTransition("NotLoggedIn");
        }
    }
}
