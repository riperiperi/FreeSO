using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using System.Threading;

namespace FSO.Client.Network
{
    public class VMDataServiceNameCache : VMBasicAvatarNameCache
    {
        private IClientDataService DataService;
        public VMDataServiceNameCache(IClientDataService dataService)
        {
            DataService = dataService;
        }

        public override bool Precache(VM vm, uint persistID)
        {
            if (!base.Precache(vm, persistID))
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
                        AvatarNames[persistID] = ava.Avatar_Name;
                    });
                });
            }
            return true;
        }
    }
}
