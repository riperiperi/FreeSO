using System.Collections.Generic;
using System.Text;

namespace FSO.Server.Debug.PacketAnalyzer
{
    public class PascalStringPacketAnalyzer : IPacketAnalyzer
    {
        #region IPacketAnalyzer Members

        public List<PacketAnalyzerResult> Analyze(byte[] data)
        {
            var result = new List<PacketAnalyzerResult>();

            /**
             * We are looking for a uint32 with the msb flipped for length followed by that many chars within a reasonable ascii range
             */
            for (var i = 0; i < data.Length; i++)
            {
                var stringValue = GetString(data, i);
                if (stringValue != null)
                {
                    result.Add(new PacketAnalyzerResult {
                        Offset = i,
                        Length = stringValue.Length + 4,
                        Description = "pascal-string(" + stringValue + ")"
                    });
                }
            }

            return result;
        }

        private string GetString(byte[] data, int index)
        {
            if (index + 4 <= data.Length)
            {
                byte len1 = data[index];
                byte len2 = data[index + 1];
                byte len3 = data[index + 2];
                byte len4 = data[index + 3];

                //Is msb set?
                if ((len1 >> 7) != 1) { return null; }
                len1 &= 0x7F;

                long len = len1 << 24 | len2 << 16 | len3 << 8 | len4;
                if (len < 0) { return null; }

                if (index + 4 + len <= data.Length)
                {
                    /** Could be! **/
                    StringBuilder str = new StringBuilder();
                    for (int i = 0; i < len; i++)
                    {
                        byte charValue = data[index + 4 + i];
                        str.Append((char)charValue);
                    }
                    return str.ToString();
                }
            }
            return null;
        }

        #endregion
    }
}
