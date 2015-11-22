using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class LotServerAuthenticationHandler
    {
        private CityServerConfiguration Config;
        private Sessions Sessions;

        public LotServerAuthenticationHandler(CityServerConfiguration config, Sessions sessions){
            this.Config = config;
            this.Sessions = sessions;
        }

        public void Handle(IAriesSession session, RequestChallenge request)
        {
            var challenge = ChallengeResponse.GetChallenge();
            session.SetAttribute("challenge", challenge);
            session.SetAttribute("callSign", request.CallSign);

            session.Write(new RequestChallengeResponse {
                Challenge = challenge
            });
        }

        public void Handle(IAriesSession session, AnswerChallenge answer)
        {
            var challenge = session.GetAttribute("challenge") as string;
            if(challenge == null)
            {
                session.Close();
                return;
            }

            var myAnswer = ChallengeResponse.AnswerChallenge(challenge, Config.Secret);
            if(myAnswer != answer.Answer)
            {
                session.Close();
                return;
            }

            //Trust established, good to go
            var newSession = Sessions.UpgradeSession<GluonSession>(session);
            newSession.IsAuthenticated = true;
            newSession.CallSign = (string)session.GetAttribute("callSign");
            newSession.Write(new AnswerAccepted());
        }
    }
}
