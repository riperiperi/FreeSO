using FSO.Common.Serialization;
using Mina.Core.Buffer;

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

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt16(MajorVersion);
            output.PutUInt16(MinorVersion);
            output.PutUInt16(PointVersion);
            output.PutUInt16(ArtVersion);
            output.PutUInt64(this.Timestamp);
            output.Put(NumberOfAttempts);
            output.Put(LastExitCode);
            output.Put(LastFailureType);
            output.Put(FailureCount);
            output.Put(IsRunning);
            output.Put(IsReLogging);
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
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
