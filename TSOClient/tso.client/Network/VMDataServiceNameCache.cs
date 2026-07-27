using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Client.Network
{
    public class VMDataServiceNameCache : VMBasicGlobalNameCache
    {
        private IClientDataService DataService;
        public VMDataServiceNameCache(IClientDataService dataService)
        {
            DataService = dataService;
        }

        public override bool Precache(VM vm, VMGlobalEntityType type, uint persistID)
        {
            if (!base.Precache(vm, type, persistID))
            {
                var cache = GetTypeCache(type);

                switch (type)
                {
                    case VMGlobalEntityType.Avatar:
                        {
                            //we need to ask the data service for this name
                            DataService.Request(Server.DataService.Model.MaskedStruct.Messaging_Icon_Avatar, persistID).ContinueWith(x =>
                            {
                                if (x.IsFaulted || x.IsCanceled || x.Result == null) return;
                                var ava = (Avatar)x.Result;
                                var failCount = 0;
                                while (ava.Avatar_Name == "Retrieving...")
                                {
                                    if (failCount++ > 100) return;
                                    Thread.Sleep(100);
                                }
                                GameThread.NextUpdate(y =>
                                {
                                    cache[persistID] = ava.Avatar_Name;
                                });
                            });
                            break;
                        }
                    case VMGlobalEntityType.Lot:
                        {
                            //we need to ask the data service for this name
                            DataService.Request(Server.DataService.Model.MaskedStruct.Bookmark_Lot, persistID).ContinueWith(x =>
                            {
                                if (x.IsFaulted || x.IsCanceled || x.Result == null) return;
                                var lot = (Lot)x.Result;
                                var failCount = 0;
                                while (lot.Lot_Name == "Retrieving...")
                                {
                                    if (failCount++ > 100) return;
                                    Thread.Sleep(100);
                                }
                                GameThread.NextUpdate(y =>
                                {
                                    cache[persistID] = lot.Lot_Name;
                                });
                            });
                            break;
                        }
                }
            }
            return true;
        }
    }
}
