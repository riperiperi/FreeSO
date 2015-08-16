using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class ClientOnlinePDU : AbstractVoltronPacket
    {
        public ushort MajorVersion { get; set; }
        public ushort MinorVersion { get; set; }
        public ushort PointVersion { get; set; }
        public ushort ArtVersion { get; set; }

        public ulong Timestamp { get; set; }
        public byte NumberOfAttempts { get; set; }

        public byte LastExitCode { get; set; }
        public byte LastFailureType { get; set; }
        public byte FailureCount { get; set; }
        public byte IsRunning { get; set; }
        public byte IsReLogging { get; set; }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.ClientOnlinePDU;
        }

        public override Mina.Core.Buffer.IoBuffer Serialize()
        {
            var buffer = Allocate(22);
            buffer.PutUInt16(MajorVersion);
            buffer.PutUInt16(MinorVersion);
            buffer.PutUInt16(PointVersion);
            buffer.PutUInt16(ArtVersion);
            buffer.PutUInt64(this.Timestamp);
            buffer.Put(NumberOfAttempts);
            buffer.Put(LastExitCode);
            buffer.Put(LastFailureType);
            buffer.Put(FailureCount);
            buffer.Put(IsRunning);
            buffer.Put(IsReLogging);

            return buffer;
        }

        public override void Deserialize(Mina.Core.Buffer.IoBuffer input)
        {
            this.MajorVersion = input.GetUInt16();
            this.MinorVersion = input.GetUInt16();
            this.PointVersion = input.GetUInt16();
            this.ArtVersion = input.GetUInt16();
            this.Timestamp = input.GetUInt64();
            this.NumberOfAttempts = input.Get();
            this.LastExitCode = input.Get();
            this.LastFailureType = input.Get();
            this.FailureCount = input.Get();
            this.IsRunning = input.Get();
            this.IsReLogging = input.Get();
        }
    }
}
