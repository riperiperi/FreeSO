using System.Collections.Generic;

namespace FSO.Server.Debug.PacketAnalyzer
{
    public class ByteCountPacketAnalyzer : IPacketAnalyzer
    {
        #region IPacketAnalyzer Members

        public List<PacketAnalyzerResult> Analyze(byte[] data)
        {
            var result = new List<PacketAnalyzerResult>();

            for (var i = 0; i < data.Length; i++)
            {
                if (i + 4 < data.Length)
                {
                    byte len1 = data[i];
                    byte len2 = data[i + 1];
                    byte len3 = data[i + 2];
                    byte len4 = data[i + 3];

                    long len = len1 << 24 | len2 << 16 | len3 << 8 | len4;

                    if (len == data.Length - (i + 4))
                    {
                        result.Add(new PacketAnalyzerResult
                        {
                            Offset = i,
                            Length = 4,
                            Description = "byte-count(" + len + ")"
                        });
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
