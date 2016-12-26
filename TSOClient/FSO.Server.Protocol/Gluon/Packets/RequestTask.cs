﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class RequestTask : AbstractGluonCallPacket
    {
        public string TaskType { get; set; }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            TaskType = input.GetPascalString();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
            output.PutPascalString(TaskType);
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.RequestTask;
        }
    }
}