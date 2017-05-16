using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
