using FSO.Common.Model;
using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class JoinLotWithTransitionRequest : AbstractElectronPacket
    {
        public LotTransitionInfo Transition;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Transition = GetTransition(input);
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.JoinLotWithTransitionRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            PutTransition(output, Transition);
        }

        private void PutTransition(IoBuffer output, LotTransitionInfo info)
        {
            output.PutUInt32(info.BeforeLocation);
            output.PutInt32(info.RelativeChangeX);
            output.PutInt32(info.RelativeChangeY);

            output.PutInt32(info.AvatarLotTilePosX);
            output.PutInt32(info.AvatarLotTilePosY);
            output.PutSingle(info.AvatarDirection);

            output.PutEnum(info.Type);
            output.PutUInt32(info.RoutingTargetLocation);
            output.PutInt32(info.RoutingLotTilePosX);
            output.PutInt32(info.RoutingLotTilePosY);
        }

        private LotTransitionInfo GetTransition(IoBuffer input)
        {
            return new LotTransitionInfo()
            {
                BeforeLocation = input.GetUInt32(),
                RelativeChangeX = input.GetInt32(),
                RelativeChangeY = input.GetInt32(),

                AvatarLotTilePosX = input.GetInt32(),
                AvatarLotTilePosY = input.GetInt32(),
                AvatarDirection = input.GetSingle(),

                Type = input.GetEnum<LotTransitionType>(),
                RoutingTargetLocation = input.GetUInt32(),
                RoutingLotTilePosX = input.GetInt32(),
                RoutingLotTilePosY = input.GetInt32(),
            };
        }
    }
}
