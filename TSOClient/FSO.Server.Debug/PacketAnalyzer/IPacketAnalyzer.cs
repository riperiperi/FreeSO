using System.Collections.Generic;

namespace FSO.Server.Debug.PacketAnalyzer
{
    public interface IPacketAnalyzer
    {
        List<PacketAnalyzerResult> Analyze(byte[] data);
    }

    public class PacketAnalyzerResult
    {
        public int Offset;
        public int Length;

        public string Description;

        public override string ToString()
        {
            return Description;
        }
    }
}
