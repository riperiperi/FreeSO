using FSO.Content.Framework;
using FSO.Vitaboy;
using System.Collections.Generic;

namespace FSO.Server.Debug.PacketAnalyzer
{
    public class ContentPacketAnalyzer : ConstantsPacketAnalyzer
    {
        private List<Constant> Constants = new List<Constant>();

        public ContentPacketAnalyzer()
        {
            var content = Content.Content.Get();

            /** Avatar Collections **/
            foreach(var collection in content.AvatarCollections.List())
            {
                var items = collection.Get();
                var collectionCast = (Far3ProviderEntry<Collection>)collection;

                foreach(var item in items)
                {
                    Constants.Add(new Constant {
                        Type = ConstantType.ULONG,
                        Value = item.PurchasableOutfitId,
                        Description = collectionCast.FarEntry.Filename + "." + item.Index
                    });

                    /**Constants.Add(new Constant
                    {
                        Type = ConstantType.UINT,
                        Value = item.FileID,
                        Description = collectionCast.FarEntry.Filename + "." + item.Index
                    });**/
                }
            }


            //TSODataDefinition file
            var dataDef = content.DataDefinition;

            foreach (var str in dataDef.Strings)
            {
                Constants.Add(new Constant
                {
                    Type = ConstantType.UINT,
                    Description = "TSOData_datadefinition(" + str.Value + ")",
                    Value = str.ID
                });
            }
        }

        public override List<Constant> GetConstants()
        {
            return Constants;
        }
    }
}
