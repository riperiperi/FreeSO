using System.Collections.Generic;
using System.Text;

namespace FSO.Server.Debug.PacketAnalyzer
{
    public class VariableLengthStringAnalyzer : IPacketAnalyzer
    {
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
                    var offset = 1;
                    if(stringValue.Length > 128)
                    {
                        offset = 2;
                    }

                    result.Add(new PacketAnalyzerResult
                    {
                        Offset = i,
                        Length = stringValue.Length + offset,
                        Description = "var-string(" + stringValue + ")"
                    });
                }
            }

            return result;
        }

        private string GetString(byte[] data, int index)
        {
            byte lengthByte = 0;
            uint length = 0;
            int shift = 0;
            byte lengthBytes = 0;

            do
            {
                lengthByte = data[index + lengthBytes];
                length |= (uint)((lengthByte & (uint)0x7F) << shift);
                shift += 7;
                lengthBytes++;
            } while (
                (lengthByte >> 7) == 1 &&
                index + lengthBytes < data.Length
            );

            if (length > 600 || length == 0)
            {
                return null;
            }

            if((index + lengthBytes + length) <= data.Length)
            {
                /** Could be! **/
                StringBuilder str = new StringBuilder();
                for (int i = 0; i < length; i++)
                {
                    byte charValue = data[index + lengthBytes + i];

                    
                    if(charValue == 0x09)
                    {
                        //Tab
                    }
                    else if(charValue == 0x0A)
                    {
                        //Line feed
                    }else if(charValue == 0x0D)
                    {
                        //CR
                    }else if(charValue >= 0x20 && charValue <= 0x7E)
                    {
                        //a-z, special chars, numbers
                    }
                    else
                    {
                        return null;
                    }

                    str.Append((char)charValue);
                }
                return str.ToString();
            }

            return null;
        }
    }
}
