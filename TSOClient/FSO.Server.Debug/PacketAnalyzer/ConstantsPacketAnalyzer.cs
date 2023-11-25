using System;
using System.Collections.Generic;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Server.Protocol.Voltron;
using FSO.Common.DatabaseService.Model;

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

            var voltronTypes = Enum.GetNames(typeof(VoltronPacketType));
            foreach (var name in voltronTypes)
            {
                var enumValue = (VoltronPacketType)Enum.Parse(typeof(VoltronPacketType), name);
                if (enumValue == VoltronPacketType.Unknown)
                {
                    continue;
                }

                var intVal = VoltronPacketTypeUtils.GetPacketCode(enumValue);

                Constants.Add(new Constant
                {
                    Type = ConstantType.USHORT,
                    Description = "VoltronPacketType." + name,
                    Value = intVal
                });
            }

            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x09736027, Description = "cTSOTopicUpdateMessage" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x0A2C6585, Description = "cTSODataTransportBuffer" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x496A78BC, Description = "cTSODataDefinitionContainer" });

            
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x097608AB, Description = "cTSOValueVector < unsigned char>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x097608B3, Description = "cTSOValueVector < signed char>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x097608B6, Description = "cTSOValueVector < unsigned short>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x29739B14, Description = "cTSOTopic" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x2A404946, Description = "cTSOTopicUpdateErrorMessage" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x496A78BC, Description = "cTSODataDefinitionContainer" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x696D1183, Description = "cTSOValue < bool >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x696D1189, Description = "cTSOValue < unsigned long>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x69D3E3DB, Description = "cTSOValue < unsigned __int64 >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x896D1196, Description = "cTSOValue < long >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x896D11A2, Description = "cTSOValueMap <class cRZAutoRefCount<class cITSOProperty> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x896D1688, Description = "cTSOValue<class cRZAutoRefCount<class cIGZString> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x89738492, Description = "cTSOValueBVector" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x89738496, Description = "cTSOValueVector<unsigned long>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x8973849A, Description = "cTSOValueVector<long>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x8973849E, Description = "cTSOValueVector <class cRZAutoRefCount<class cIGZString> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x89739A79, Description = "cTSOProperty" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x89D3E3EF, Description = "cTSOValue<__int64>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0x89D3E40E, Description = "cTSOValueMap<unsigned __int64>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA96E38A0, Description = "cTSOValueMap<unsigned long>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA96E38A8, Description = "cTSOValueMap<long>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA96E38AC, Description = "cTSOValueMap <class cRZAutoRefCount<class cIGZString> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA96E7E5B, Description = "cTSOValue<class cRZAutoRefCount<class cITSOProperty> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA97353EE, Description = "cTSODataService" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA97384A3, Description = "cTSOValueVector<class cRZAutoRefCount<class cITSOProperty> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA975FA6E, Description = "cTSODataServiceClient" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA99AF3A8, Description = "cTSOValueMap<class cRZAutoRefCount<class cIGZUnknown> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA99AF3AC, Description = "cTSOValueVector <class cRZAutoRefCount<class cIGZUnknown> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA99AF3B7, Description = "cTSOValue<class cRZAutoRefCount<class cIGZUnknown> >" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA9D3E412, Description = "cTSOValueMap<__int64>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA9D3E428, Description = "cTSOValueVector <unsigned __int64>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xA9D3E42D, Description = "cTSOValueVector<__int64>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xC976087C, Description = "cTSOValue<unsigned char>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xC97757F5, Description = "cTSOValueMap<bool>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xE976088A, Description = "cTSOValue<signed char>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xE9760891, Description = "cTSOValue<unsigned short>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xE9760897, Description = "cTSOValue<short>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xE976089F, Description = "cTSOValueMap<unsigned char>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xE97608A2, Description = "cTSOValueMap<signed char>" });

            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xE97608A5, Description = "cTSOValueMap <unsigned short>" });
            Constants.Add(new Constant { Type = ConstantType.UINT, Value = 0xE97608A8, Description = "cTSOValueMap<short>" });
    }


        #region IPacketAnalyzer Members
        public List<PacketAnalyzerResult> Analyze(byte[] data)
        {
            var result = new List<PacketAnalyzerResult>();

            for (var i = 0; i < data.Length; i++)
            {
                foreach (var constant in GetConstants())
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
                        case ConstantType.ULONG:
                            if (i + 8 >= data.Length) { continue; }
                            byte long1 = data[i];
                            byte long2 = data[i + 1];
                            byte long3 = data[i + 2];
                            byte long4 = data[i + 3];
                            byte long5 = data[i + 4];
                            byte long6 = data[i + 5];
                            byte long7 = data[i + 6];
                            byte long8 = data[i + 7];
                            
                            value = (ulong)(long1 << 54 | long2 << 48 | long3 << 40 | long4 << 32 | long5 << 24 | long6 << 16 | long7 << 8 | long8);
                            len = 4;
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

        public virtual List<Constant> GetConstants()
        {
            return Constants;
        }
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
        UINT,
        ULONG
    }
}
