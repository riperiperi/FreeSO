using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Utils;
using FSO.Server.Servers.Lot.Lifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Handlers
{
    /// <summary>
    /// Establishes authentication with the city server
    /// </summary>
    public class CityServerAuthenticationHandler
    {
        public void Handle(IGluonSession session, RequestClientSession request)
        {
            //Respond asking for a gluon challenge
            session.Write(new RequestChallenge() { CallSign = session.CallSign, PublicHost = session.PublicHost, InternalHost = session.InternalHost });
        }

        public void Handle(IGluonSession session, RequestChallengeResponse challenge)
        {
            var rawSession = ((CityConnection)session);
            var answer = ChallengeResponse.AnswerChallenge(challenge.Challenge, rawSession.CityConfig.Secret);

            session.Write(new AnswerChallenge {
                Answer = answer
            });
        }

        public void Handle(IGluonSession session, AnswerAccepted accepted)
        {
            var rawSession = ((CityConnection)session);
            rawSession.AuthenticationEstablished();
        }
    }
}
