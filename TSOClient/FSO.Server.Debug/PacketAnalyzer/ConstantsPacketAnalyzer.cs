using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Server.Protocol.Voltron.Model;

namespace FSO.Server.Debug.PacketAnalyzer
{
    public class ConstantsPacketAnalyzer : IPacketAnalyzer
    {
        private static List<Constant> Constants = new List<Constant>();

        static ConstantsPacketAnalyzer()
        {
            /*var voltronTypes = Enum.GetNames(typeof(VoltronPacketType));
            foreach (var name in voltronTypes)
            {
                var enumValue = (VoltronPacketType)Enum.Parse(typeof(VoltronPacketType), name);
                if (enumValue == VoltronPacketType.Unknown) { continue; }

                Constants.Add(new Constant {
                    Type = ConstantType.USHORT,
                    Description = "VoltronPacket." + name,
                    Value = VoltronPacketTypeUtils.GetPacketCode(enumValue)
                });
            }*/

            var dbRequestTypes = Enum.GetNames(typeof(DBRequestType));
            foreach (var name in dbRequestTypes)
            {
                var enumValue = (DBRequestType)Enum.Parse(typeof(DBRequestType), name);
                if (enumValue == DBRequestType.Unknown) { continue; }

                Constants.Add(new Constant
                {
                    Type = ConstantType.UINT,
                    Description = "DBRequestType." + name,
                    Value = DBRequestTypeUtils.GetRequestID(enumValue)
                });

                if (enumValue == DBRequestType.UpdatePreferedLanguageByID)
                {
                    var val = DBRequestTypeUtils.GetRequestID(enumValue);
                }
            }

            var dbResponseTypes = Enum.GetNames(typeof(DBResponseType));
            foreach (var name in dbResponseTypes)
            {
                var enumValue = (DBResponseType)Enum.Parse(typeof(DBResponseType), name);
                if (enumValue == DBResponseType.Unknown)
                {
                    continue;
                }
                Constants.Add(new Constant
                {
                    Type = ConstantType.UINT,
                    Description = "DBResponseType." + name,
                    Value = DBResponseTypeUtils.GetResponseID(enumValue)
                });
            }

            var magicNumberTypes = Enum.GetNames(typeof(MagicNumberEnum));
            foreach (var name in magicNumberTypes)
            {
                var enumValue = (MagicNumberEnum)Enum.Parse(typeof(MagicNumberEnum), name);
                var intVal = MagicNumberEnumUtils.ToInt(enumValue);
                var floatVal = MagicNumberEnumUtils.ToFloat(enumValue);

                Constants.Add(new Constant
                {
                    Type = ConstantType.UINT,
                    Description = "MagicNumberEnum." + name + "( " + intVal + ", " + floatVal + ")",
                    Value = MagicNumberEnumUtils.ToID(enumValue)
                });
            }





            /*var dataDef = new TSODataDefinition();
            dataDef.Read(Path.Combine(NetworkInspector.GameDir, "TSOData_datadefinition.dat"));

            foreach (var str in dataDef.Strings)
            {
                Constants.Add(new Constant {
                    Type = ConstantType.UINT,
                    Description = "TSOData_datadefinition(" + str.Value + ")",
                    Value = str.ID
                });
            }*/
        }


        #region IPacketAnalyzer Members
        public List<PacketAnalyzerResult> Analyze(byte[] data)
        {
            var result = new List<PacketAnalyzerResult>();

            for (var i = 0; i < data.Length; i++)
            {
                foreach (var constant in Constants)
                {
                    int len = 0;
                    object value = null;
                    switch (constant.Type)
                    {
                        case ConstantType.UINT:
                            if (i + 4 >= data.Length) { continue; }
                            byte len1 = data[i];
                            byte len2 = data[i + 1];
                            byte len3 = data[i + 2];
                            byte len4 = data[i + 3];
                            value = (uint)(len1 << 24 | len2 << 16 | len3 << 8 | len4);
                            len = 4;
                            break;
                        case ConstantType.USHORT:
                            if (i + 2 >= data.Length) { continue; }
                            byte short1 = data[i];
                            byte short2 = data[i + 1];
                            value = (ushort)(short1 << 8 | short2);
                            len = 2;
                            break;
                    }

                    if (value.ToString() == constant.Value.ToString())
                    {
                        result.Add(new PacketAnalyzerResult {
                            Offset = i,
                            Length = len,
                            Description = constant.Description
                        });
                    }
                }
            }
            return result;
        }
        #endregion
    }


    public class Constant
    {
        public ConstantType Type;
        public object Value;
        public string Description;
    }

    public enum ConstantType
    {
        USHORT,
        UINT
    }
}
